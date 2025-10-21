using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using SilksongPrepatcher.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SilksongPrepatcher.Patchers
{
    public static class PlayerDataPatcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource($"SilksongPrepatcher.{nameof(PlayerDataPatcher)}");

        public static IEnumerable<string> TargetDLLs { get; } = new[] { AssemblyNames.Assembly_CSharp, };

        public static void Patch(AssemblyDefinition asm)
        {
            Log.LogInfo($"Patching {asm.Name.Name}");

            // Get the method definition for the GetVariable and SetVariable methods
            AssemblyDefinition sharedUtilsAsm = AssemblyDefinition.ReadAssembly(Path.Combine(Paths.ManagedPath, "TeamCherry.SharedUtils.dll"));
            TypeDefinition variableExt = sharedUtilsAsm.MainModule.GetType("TeamCherry.SharedUtils.VariableExtensions");
            MethodDefinition genericGetMethod = variableExt.GetMethods().First(
                x => x.Name == "GetVariable" && x.ContainsGenericParameter && x.Parameters.Count == 2);
            MethodDefinition genericSetMethod = variableExt.GetMethods().First(
                x => x.Name == "SetVariable" && x.ContainsGenericParameter && x.Parameters.Count == 3);


            int replaceCounter = 0;

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

                        MethodReference getMethodRef;
                        OpCode callOpCode;
                        if (!getMethods.TryGetValue(field.FieldType, out MethodDefinition getMethod))
                        {
                            MethodReference genericMethodRefImported = mod.ImportReference(genericGetMethod);
                            GenericInstanceMethod genericInstanceMethod = new(genericMethodRefImported);
                            genericInstanceMethod.GenericArguments.Add(field.FieldType);

                            getMethodRef = genericInstanceMethod;
                            callOpCode = OpCodes.Call;
                        }
                        else
                        {
                            getMethodRef = mod.ImportReference(getMethod);
                            callOpCode = OpCodes.Callvirt;
                        }

                        Instruction[] newInstrs =
                        [
                            il.Create(OpCodes.Ldstr, field.Name),
                            il.Create(callOpCode, getMethodRef)
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

                        MethodReference setMethodRef;
                        OpCode callOpCode;
                        if (!setMethods.TryGetValue(field.FieldType, out MethodDefinition setMethod))
                        {
                            MethodReference genericMethodRefImported = mod.ImportReference(genericSetMethod);
                            GenericInstanceMethod genericInstanceMethod = new(genericMethodRefImported);
                            genericInstanceMethod.GenericArguments.Add(field.FieldType);

                            setMethodRef = genericInstanceMethod;
                            callOpCode = OpCodes.Call;
                        }
                        else
                        {
                            setMethodRef = mod.ImportReference(setMethod);
                            callOpCode = OpCodes.Callvirt;
                        }

                        VariableReference local = GetOrAddLocal(field.FieldType);

                        Instruction[] newInstrs =
                        [
                            il.Create(OpCodes.Stloc, local),
                            il.Create(OpCodes.Ldstr, field.Name),
                            il.Create(OpCodes.Ldloc, local),
                            il.Create(callOpCode, setMethodRef),
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

            // if debugging
            // mod.Write(Path.Combine(Paths.BepInExRootPath, "patched_Assembly-CSharp.dll"));  // (and then inspect in DNSpy)
        }
    }
}