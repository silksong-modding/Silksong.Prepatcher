using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using SilksongPrepatcher.PlayerData;
using SilksongPrepatcher.Utils;

namespace SilksongPrepatcher.Patchers.PlayerDataPatcher;

/// <summary>
/// Replace all field accesses (Get/Set) on an instance of PlayerData with calls to Get/Set variable funcs.
///
/// This allows for easy hooking, allowing mods to effectively change the value of a PD field without affecting the save data,
/// and allowing mods to monitor when a field value is changed.
///
/// Accesses to fields which are not serialized are not patched, as they do not affect the save data.
/// Mods wanting to affect the value of such fields should do so directly - and this will not affect the save data by definition.
///
/// Private fields are also not patched - in all cases these are not serialized, and the Get/Set variable funcs as written
/// fail when given a private field anyway.
/// </summary>
public class PlayerDataPatcher : BasePrepatcher
{
    private static string CacheFilePath =>
        Path.Combine(SilksongPrepatcher.PatchCacheDir, $"{nameof(PlayerDataPatcher)}_cache.txt");

    public override void PatchAssembly(AssemblyDefinition asm)
    {
        PatchingContext ctx = new(asm);

        ReplaceAccessMethodBodies(ctx);

        PatchedMethodCache? cache = PatchedMethodCache.Deserialize(CacheFilePath);
        if (cache == null)
        {
            ReplaceFieldAccesses(ctx);
        }
        else
        {
            ReplaceFieldAccessesFromCache(ctx, cache);
        }
    }

    private void ReplaceAccessMethodBodies(PatchingContext ctx)
    {
        ReplaceGetMethodBody(ctx, "GetBool", typeof(PDCache).GetMethod(nameof(PDCache.GetBool)));
        ReplaceGetMethodBody(ctx, "GetInt", typeof(PDCache).GetMethod(nameof(PDCache.GetInt)));
        ReplaceGetMethodBody(
            ctx,
            "GetString",
            typeof(PDCache).GetMethod(nameof(PDCache.GetString))
        );
        ReplaceGetMethodBody(ctx, "GetFloat", typeof(PDCache).GetMethod(nameof(PDCache.GetFloat)));

        ReplaceSetMethodBody(ctx, "SetBool", typeof(PDCache).GetMethod(nameof(PDCache.SetBool)));
        ReplaceSetMethodBody(ctx, "SetInt", typeof(PDCache).GetMethod(nameof(PDCache.SetInt)));
        ReplaceSetMethodBody(
            ctx,
            "SetString",
            typeof(PDCache).GetMethod(nameof(PDCache.SetString))
        );
        ReplaceSetMethodBody(ctx, "SetFloat", typeof(PDCache).GetMethod(nameof(PDCache.SetFloat)));
    }

    private void ReplaceGetMethodBody(
        PatchingContext ctx,
        string methodName,
        MethodInfo replacingMethod
    )
    {
        MethodDefinition toReplace = ctx.PDType.Methods.FirstOrDefault(m => m.Name == methodName);

        if (toReplace is null)
        {
            Log.LogError(
                $"Failed to replace method {methodName} with method {replacingMethod.Name}"
            );
            return;
        }

        MethodReference mr = ctx.MainModule.ImportReference(replacingMethod);

        toReplace.Body.ExceptionHandlers.Clear();
        toReplace.Body.Variables.Clear();

        ILProcessor processor = toReplace.Body.GetILProcessor();

        foreach (Instruction instr in toReplace.Body.Instructions.ToList())
        {
            processor.Remove(instr);
        }

        processor.Append(processor.Create(OpCodes.Ldarg_0));
        processor.Append(processor.Create(OpCodes.Ldarg_1));
        processor.Append(processor.Create(OpCodes.Call, mr));
        processor.Append(processor.Create(OpCodes.Ret));

        toReplace.Body.InitLocals = false;
    }

    private void ReplaceSetMethodBody(
        PatchingContext ctx,
        string methodName,
        MethodInfo replacingMethod
    )
    {
        MethodDefinition toReplace = ctx.PDType.Methods.FirstOrDefault(m => m.Name == methodName);

        if (toReplace is null)
        {
            Log.LogError(
                $"Failed to replace method {methodName} with method {replacingMethod.Name}"
            );
            return;
        }

        MethodReference mr = ctx.MainModule.ImportReference(replacingMethod);

        toReplace.Body.ExceptionHandlers.Clear();
        toReplace.Body.Variables.Clear();

        ILProcessor processor = toReplace.Body.GetILProcessor();

        foreach (Instruction instr in toReplace.Body.Instructions.ToList())
        {
            processor.Remove(instr);
        }

        processor.Append(processor.Create(OpCodes.Ldarg_0));
        processor.Append(processor.Create(OpCodes.Ldarg_1));
        processor.Append(processor.Create(OpCodes.Ldarg_2));
        processor.Append(processor.Create(OpCodes.Call, mr));

        toReplace.Body.InitLocals = false;
    }

    private void ReplaceFieldAccessesFromCache(PatchingContext ctx, PatchedMethodCache cache)
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
                        throw new Exception($"Failed to patch {typeName} {method.FullName}");
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
    private void ReplaceFieldAccesses(PatchingContext ctx)
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
                if (!method.HasBody)
                    continue;

                if (
                    method.DeclaringType == ctx.PDType
                    && (
                        method.Name == "SetupNewPlayerData"
                        || method.Name == "SetupExistingPlayerData"
                        || method.Name == "OnUpdatedVariable"
                        || method.Name.Contains(".ctor")
                    )
                )
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

    private bool PatchMethod(
        MethodDefinition method,
        PatchingContext ctx,
        out int replaced,
        out int missed
    )
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

        for (
            int instructionIndex = il.Body.Instructions.Count - 1;
            instructionIndex >= 0;
            instructionIndex--
        )
        {
            Instruction instr = il.Body.Instructions[instructionIndex];

            if (
                instr.Operand is not FieldReference field
                || field.DeclaringType.FullName != ctx.PDType.FullName
                // Private/nonserialized attribute access shouldn't be routed through the event
                || field.Resolve().IsPrivate
                || field
                    .Resolve()
                    .CustomAttributes.Any(ca =>
                        ca.AttributeType.FullName == "System.NonSerializedAttribute"
                    )
            )
            {
                continue;
            }

            if (instr.OpCode == OpCodes.Ldfld)
            {
                // Currently: [..., PlayerData] ->(Ldfld) [..., Value]
                // Should become: [..., PlayerData] ->(Ldstr) [..., PlayerData, FieldName] ->(Callvirt) [..., Value]

                ctx.GetGetMethod(
                    field.FieldType,
                    out MethodReference accessMethod,
                    out PatchingContext.AccessMethodType accessType
                );
                OpCode callOpCode =
                    accessType == PatchingContext.AccessMethodType.Default
                        ? OpCodes.Callvirt
                        : OpCodes.Call;

                Instruction[] newInstrs =
                [
                    il.Create(OpCodes.Ldstr, field.Name),
                    il.Create(callOpCode, accessMethod),
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

                ctx.GetSetMethod(
                    field.FieldType,
                    out MethodReference accessMethod,
                    out PatchingContext.AccessMethodType accessType
                );
                OpCode callOpCode =
                    accessType == PatchingContext.AccessMethodType.Default
                        ? OpCodes.Callvirt
                        : OpCodes.Call;

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
                bool patched = TryPatchRefAccess(il, instr, method, ctx, GetOrAddLocal);
                if (patched)
                    replaced++;
                else
                    missed++;
            }
        }

        method.Body.OptimizeMacros();

        return replaced > 0;
    }

    private bool TryPatchRefAccess(
        ILProcessor il,
        Instruction instr,
        MethodDefinition method,
        PatchingContext ctx,
        Func<TypeReference, VariableReference> getOrAddLocal
    )
    {
        // Currently this only operates on the five calls in GameManager.TimePassesElsewhere to CheckReadyToLeave(ref ...)

        Instruction nextInstr = instr.Next;

        if (
            nextInstr.OpCode == OpCodes.Call
            && nextInstr.Operand is MethodReference methodReference
            && methodReference.ReturnType.FullName == "System.Void"
            && methodReference.Parameters.Count == 1
            && instr.Operand is FieldReference field
        )
        {
            // Current:
            // [..., PlayerData]
            // ->(Ldflda) [..., ref field]
            // ->(Call or Callvirt) [...]

            // Note there are no other args and no return in the case we care about...

            // Desired:
            // [..., PlayerData]
            // ->(dup) [..., PlayerData, PlayerData]
            // ->(Ldstr) [..., PlayerData, PlayerData, fieldName]
            // ->(Call or callvirt) [..., PlayerData, fieldValue]
            // ->(Stloc) [..., PlayerData] (fieldValue in locals)
            // ->(Ldloca) [..., PlayerData, ref fieldValue] (fieldValue in locals)
            // ->(Call or Callvirt) [..., PlayerData] (fieldValue in locals, updated)
            // ->(Ldstr) [..., PlayerData, fieldName] (fieldValue in locals, updated)
            // ->(Ldloc) [..., PlayerData, fieldName, updatedFieldValue]
            // ->(Call or Callvirt set variable) [...]

            VariableReference local = getOrAddLocal(field.FieldType);

            ctx.GetGetMethod(
                field.FieldType,
                out MethodReference getAccess,
                out PatchingContext.AccessMethodType getAccessType
            );
            OpCode getAccessOpcode =
                getAccessType == PatchingContext.AccessMethodType.Default
                    ? OpCodes.Callvirt
                    : OpCodes.Call;
            ctx.GetSetMethod(
                field.FieldType,
                out MethodReference setAccess,
                out PatchingContext.AccessMethodType setAccessType
            );
            OpCode setAccessOpcode =
                setAccessType == PatchingContext.AccessMethodType.Default
                    ? OpCodes.Callvirt
                    : OpCodes.Call;

            Instruction[] newInstructions =
            [
                il.Create(OpCodes.Dup),
                il.Create(OpCodes.Ldstr, field.Name), // Modify existing instr here
                il.Create(getAccessOpcode, getAccess),
                il.Create(OpCodes.Stloc, local),
                il.Create(OpCodes.Ldloca, local),
                nextInstr,
                il.Create(OpCodes.Ldstr, field.Name),
                il.Create(OpCodes.Ldloc, local),
                il.Create(setAccessOpcode, setAccess),
            ];

            il.InsertBefore(instr, newInstructions[0]);
            instr.OpCode = newInstructions[1].OpCode;
            instr.Operand = newInstructions[1].Operand;
            il.InsertAfter(instr, newInstructions[2]);
            il.InsertAfter(newInstructions[2], newInstructions[3]);
            il.InsertAfter(newInstructions[3], newInstructions[4]);

            il.InsertAfter(nextInstr, newInstructions[6]);
            il.InsertAfter(newInstructions[6], newInstructions[7]);
            il.InsertAfter(newInstructions[7], newInstructions[8]);

            return true;
        }

        return false;
    }
}
