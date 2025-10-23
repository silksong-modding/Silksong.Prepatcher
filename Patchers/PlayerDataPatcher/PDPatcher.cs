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

namespace SilksongPrepatcher.Patchers.PlayerDataPatcher
{
    public static class PDPatcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource($"SilksongPrepatcher.PlayerDataPatcher");

        private static readonly string CacheFilePath = Path.Combine(Paths.CachePath, "PDPatcher_cache.txt");

        public static IEnumerable<string> TargetDLLs { get; } = new[] { AssemblyNames.Assembly_CSharp, };

        public static void Patch(AssemblyDefinition asm)
        {
            Log.LogInfo($"Patching {asm.Name.Name}");

            PatchingContext ctx = new(asm);

            PatchedMethodCache? cache = PatchedMethodCache.Deserialize(CacheFilePath);
            if (cache == null)
            {
                ReplaceFieldAccesses(ctx);
            }
            else
            {
                ReplaceFieldAccessesFromCache(ctx, cache);
            }

            // for debugging - can inspect in ILSpy
            ctx.MainModule.Write(Path.Combine(Paths.CachePath, $"{nameof(PDPatcher)}_{AssemblyNames.Assembly_CSharp}"));
        }

        private static void ReplaceFieldAccessesFromCache(PatchingContext ctx, PatchedMethodCache cache)
        {
            int replaceCounter = 0;
            int missCounter = 0;
            Stopwatch sw = new();
            sw.Start();

            foreach ((string typeName, List<string> methodNames) in cache.PatchedMethods)
            {
                TypeDefinition type = ctx.MainModule.GetType(typeName);
                HashSet<string> methodNameSet = new(methodNames);
                
                foreach (MethodDefinition method in type.Methods)
                {
                    if (methodNames.Contains(method.FullName))
                    {
                        bool patched = PatchMethod(method, ctx, out int replaced, out int missed);
                        if (!patched)
                        {
                            throw new System.Exception($"Failed to patch {typeName} {method.FullName}");
                        }

                        replaceCounter += replaced;
                        missCounter += missed;
                    }
                }
            }

            sw.Stop();
            Log.LogInfo($"Patched {replaceCounter} accesses from cache in {sw.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Replace field accesses with calls to Get/Set variable funcs
        /// </summary>
        private static void ReplaceFieldAccesses(PatchingContext ctx)
        {
            int replaceCounter = 0;
            int missCounter = 0;
            PatchedMethodCache cache = new();

            Stopwatch sw = new();
            sw.Start();

            foreach (TypeDefinition typeDef in CecilUtils.GetTypeDefinitions(ctx.MainModule))
            {
                foreach (MethodDefinition method in typeDef.Methods)
                {
                    if (!method.HasBody) continue;

                    if (method.DeclaringType == ctx.PDType && (
                        method.Name == "SetupNewPlayerData"
                        || method.Name.Contains(".ctor")))
                    {
                        continue;
                    }

                    if (PatchMethod(method, ctx, out int replaced, out int missed))
                    {
                        cache.Add(typeDef.FullName, method.FullName);
                    }
                    replaceCounter += replaced;
                    missCounter += missed;
                }

            }

            sw.Stop();
            Log.LogInfo($"Patched {replaceCounter} accesses in {sw.ElapsedMilliseconds} ms");
            Log.LogInfo($"Missed {missCounter} accesses");

            cache.Serialize(CacheFilePath);
        }

        private static bool PatchMethod(MethodDefinition method, PatchingContext ctx, out int replaced, out int missed)
        {
            Dictionary<TypeReference, VariableReference> addedVariables = new();
            replaced = 0;
            missed = 0;

            method.Body.SimplifyMacros();

            ILProcessor il = method.Body.GetILProcessor();

            VariableReference GetOrAddLocal(TypeReference td)
            {
                TypeReference importedType = ctx.MainModule.ImportReference(td);

                if (!addedVariables.ContainsKey(importedType))
                {
                    VariableDefinition newLocal = new(importedType);
                    method.Body.Variables.Add(newLocal);
                    addedVariables.Add(importedType, newLocal);
                }
                return addedVariables[importedType];
            }

            for (int instructionIndex = il.Body.Instructions.Count - 1; instructionIndex >= 0; instructionIndex--)
            {
                Instruction instr = il.Body.Instructions[instructionIndex];

                if (instr.Operand is not FieldReference field
                    || field.DeclaringType.FullName != ctx.PDType.FullName
                    // Private/nonserialized attribute access shouldn't be routed through the event
                    || field.Resolve().IsPrivate
                    || field.Resolve().CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.NonSerializedAttribute")
                    )
                {
                    continue;
                }

                if (instr.OpCode == OpCodes.Ldfld)
                {
                    // Currently: [..., PlayerData] ->(Ldfld) [..., Value]
                    // Should become: [..., PlayerData] ->(Ldstr) [..., PlayerData, FieldName] ->(Callvirt) [..., Value]

                    ctx.GetGetMethod(field.FieldType, out MethodReference accessMethod, out PatchingContext.AccessMethodType accessType);
                    OpCode callOpCode = accessType == PatchingContext.AccessMethodType.Default ? OpCodes.Callvirt : OpCodes.Call;

                    Instruction[] newInstrs =
                    [
                        il.Create(OpCodes.Ldstr, field.Name),
                            il.Create(callOpCode, accessMethod)
                    ];

                    // Copy over the new instruction so br* style instructions still work
                    instr.OpCode = newInstrs[0].OpCode;
                    instr.Operand = newInstrs[0].Operand;
                    il.InsertAfter(instr, newInstrs[1]);

                    replaced++;
                }
                else if (instr.OpCode == OpCodes.Stfld)
                {
                    // Currently: [..., PlayerData, NewValue] ->(Stfld) [...]
                    // Should become:
                    // - [..., PlayerData, NewValue]
                    // ->(Stloc) [..., PlayerData] (NewValue in locals)
                    // ->(Ldstr) [..., PlayerData, FieldName] (NewValue in locals)
                    // ->(Ldloc) [..., PlayerData, FieldName, NewValue]
                    // ->(Callvirt) [...]

                    ctx.GetSetMethod(field.FieldType, out MethodReference accessMethod, out PatchingContext.AccessMethodType accessType);
                    OpCode callOpCode = accessType == PatchingContext.AccessMethodType.Default ? OpCodes.Callvirt : OpCodes.Call;

                    VariableReference local = GetOrAddLocal(field.FieldType);

                    Instruction[] newInstrs =
                    [
                        il.Create(OpCodes.Stloc, local),
                            il.Create(OpCodes.Ldstr, field.Name),
                            il.Create(OpCodes.Ldloc, local),
                            il.Create(callOpCode, accessMethod),
                        ];

                    // Copy over the new instruction so br* style instructions still work
                    instr.OpCode = newInstrs[0].OpCode;
                    instr.Operand = newInstrs[0].Operand;

                    il.InsertAfter(instr, newInstrs[1]);
                    il.InsertAfter(newInstrs[1], newInstrs[2]);
                    il.InsertAfter(newInstrs[2], newInstrs[3]);

                    replaced++;
                }
                else if (instr.OpCode == OpCodes.Ldflda)
                {
                    bool patched = TryPatchRefAccess(il, instr, method, ctx);
                    if (patched) replaced++;
                    else missed++;
                }
            }

            method.Body.OptimizeMacros();

            return replaced > 0;
        }

        private static bool TryPatchRefAccess(ILProcessor il, Instruction instr, MethodDefinition method, PatchingContext ctx)
        {
            // TODO - implement this where desired
            return false;
        }
    }
}