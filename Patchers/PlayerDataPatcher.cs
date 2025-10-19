using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using SilksongPrepatcher.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SilksongPrepatcher.Patchers
{
    public static class PlayerDataPatcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource($"SilksongPrepatcher.{nameof(PlayerDataPatcher)}");

        public static IEnumerable<string> TargetDLLs { get; } = new[] { AssemblyNames.Assembly_CSharp };

        public static void Patch(AssemblyDefinition asm)
        {
            Log.LogInfo($"Patching {asm.Name.Name}");

            int replaceCounter = 0;

            int getMissCounter = 0;
            int setMissCounter = 0;

            ModuleDefinition mod = asm.MainModule;
            TypeDefinition pdType = mod.Types.First(t => t.Name == "PlayerData");

            // List of allowed types
            HashSet<string> allowedTypes =
            [
                "Bool",
                "String",
                "Float",
                "Int",
                "Vector3",
            ];

            // Cache Get/Set methods
            Dictionary<TypeReference, MethodDefinition> getMethods = pdType.Methods
                .Where(m => m.Name.StartsWith("Get") && allowedTypes.Contains(m.Name.Substring(3)))
                .ToDictionary(m => m.ReturnType);

            Dictionary<TypeReference, MethodDefinition> setMethods = pdType.Methods
                .Where(m => m.Name.StartsWith("Set") && allowedTypes.Contains(m.Name.Substring(3)))
                .ToDictionary(m => m.Parameters[1].ParameterType);

            // Cache for added variables, reset after each method
            Dictionary<TypeReference, VariableReference> addedVariables = new();

            Stopwatch sw = new();
            sw.Start();

            foreach (MethodDefinition method in CecilUtils.GetMethodDefinitions(asm.MainModule).Where(md => md.HasBody))
            {
                if (method.DeclaringType == pdType && (
                    method.Name == "SetupNewPlayerData"
                    || method.Name.Contains(".ctor")))
                {
                    continue;
                }

                method.Body.SimplifyMacros();

                ILProcessor il = method.Body.GetILProcessor();

                addedVariables.Clear();

                VariableReference GetOrAddLocal(TypeReference td)
                {
                    TypeReference importedType = mod.ImportReference(td);

                    if (!addedVariables.ContainsKey(importedType))
                    {
                        VariableDefinition newLocal = new(importedType);
                        method.Body.Variables.Add(newLocal);
                        addedVariables.Add(importedType, newLocal);
                    }
                    return addedVariables[importedType];
                }

                int instructionIndex;
                for (instructionIndex = il.Body.Instructions.Count - 1; instructionIndex >= 0; instructionIndex--)
                {
                    Instruction instr = il.Body.Instructions[instructionIndex];

                    if (instr.OpCode == OpCodes.Ldfld)
                    {
                        if (instr.Operand is not FieldReference field || field.DeclaringType.FullName != pdType.FullName)
                        {
                            continue;
                        }

                        // Currently: [..., PlayerData] ->(Ldfld) [..., Value]
                        // Should become: [..., PlayerData] ->(Ldstr) [..., PlayerData, FieldName] ->(Callvirt) [..., Value]

                        if (!getMethods.TryGetValue(field.FieldType, out MethodDefinition getMethod))
                        {
                            getMissCounter++;
                            continue;
                        }

                        Instruction[] newInstrs =
                        [
                            il.Create(OpCodes.Ldstr, field.Name),
                            il.Create(OpCodes.Callvirt, mod.ImportReference(getMethod))
                        ];

                        // Copy over the new instruction so br* style instructions still work
                        instr.OpCode = newInstrs[0].OpCode;
                        instr.Operand = newInstrs[0].Operand;
                        il.InsertAfter(instr, newInstrs[1]);

                        replaceCounter++;
                    }
                    else if (instr.OpCode == OpCodes.Stfld)
                    {
                        if (instr.Operand is not FieldReference field || field.DeclaringType.FullName != pdType.FullName)
                        {
                            continue;
                        }

                        // Currently: [..., PlayerData, NewValue] ->(Stfld) [...]
                        // Should become:
                        // - [..., PlayerData, NewValue]
                        // ->(Stloc) [..., PlayerData] (NewValue in locals)
                        // ->(Ldstr) [..., PlayerData, FieldName] (NewValue in locals)
                        // ->(Ldloc) [..., PlayerData, FieldName, NewValue]
                        // ->(Callvirt) [...]

                        if (!setMethods.TryGetValue(field.FieldType, out MethodDefinition setMethod))
                        {
                            setMissCounter++;
                            continue;
                        }

                        VariableReference local = GetOrAddLocal(field.FieldType);

                        Instruction[] newInstrs =
                        [
                            il.Create(OpCodes.Stloc, local),
                            il.Create(OpCodes.Ldstr, field.Name),
                            il.Create(OpCodes.Ldloc, local),
                            il.Create(OpCodes.Callvirt, mod.ImportReference(setMethod))
                        ];

                        // Copy over the new instruction so br* style instructions still work
                        instr.OpCode = newInstrs[0].OpCode;
                        instr.Operand = newInstrs[0].Operand;

                        il.InsertAfter(instr, newInstrs[1]);
                        il.InsertAfter(newInstrs[1], newInstrs[2]);
                        il.InsertAfter(newInstrs[2], newInstrs[3]);

                        replaceCounter++;
                    }
                }

                method.Body.OptimizeMacros();
            }

            sw.Stop();
            Log.LogInfo($"Patched {replaceCounter} accesses in {sw.ElapsedMilliseconds} ms");
            Log.LogInfo($"Missed {getMissCounter} gets and {setMissCounter} sets");

            // if debugging
            // mod.Write(...);  // (and then inspect in DNSpy)
        }
    }
}