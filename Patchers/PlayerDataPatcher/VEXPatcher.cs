using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilksongPrepatcher.Patchers.PlayerDataPatcher;

public static class VEXPatcher
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource($"SilksongPrepatcher.PlayerDataPatcher");

    public static IEnumerable<string> TargetDLLs { get; } = new[] { AssemblyNames.TeamCherry_SharedUtils, };

    public static void Patch(AssemblyDefinition asm)
    {
        Log.LogInfo($"Patching {asm.Name.Name}");

        ModuleDefinition module = asm.MainModule;

        string fullClassName = "TeamCherry.SharedUtils.VariableExtensions";
        TypeDefinition type = module.GetType(fullClassName);

        MethodDefinition? getVariablesMethod = null;
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name != "GetVariables") continue;
            GenericInstanceType? secondParamType = method.Parameters[1].ParameterType as GenericInstanceType;

            if (secondParamType == null ||
                secondParamType.Name != "Func`2" ||
                secondParamType.Resolve().FullName != "System.Func`2"
                )
            {
                continue;
            }
            getVariablesMethod = method;
            break;
        }

        MethodDefinition? getVariableMethod = null;
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name != "GetVariable") continue;
            if (method.Parameters.Count != 2) continue;
            if (!method.ContainsGenericParameter) continue;
            getVariableMethod = method;
            break;
        }

        if (getVariablesMethod != null && getVariableMethod != null)
        {
            PatchGetVariablesMethod(getVariablesMethod, getVariableMethod, module);
        }

        // for debugging - can inspect in ILSpy
        module.Write(System.IO.Path.Combine(BepInEx.Paths.CachePath, $"{nameof(PDPatcher)}_{AssemblyNames.TeamCherry_SharedUtils}"));
    }

    private static void PatchGetVariablesMethod(MethodDefinition method, MethodDefinition getVariableMethod, ModuleDefinition mod)
    {
        Log.LogInfo($"Found method {method.FullName}");

        // Replace (T)(object)fieldInfo.GetValue(obj)
        // with VariableExtensions.GetVariable<T>(obj, fieldInfo.Name)

        method.Body.SimplifyMacros();

        ILProcessor il = method.Body.GetILProcessor();

        Instruction? callGetValue = null;
        foreach (Instruction instr in method.Body.Instructions)
        {
            if (instr.OpCode == OpCodes.Callvirt &&
                instr.Operand is MethodReference methodRef &&
                methodRef.Name == "GetValue" &&
                methodRef.DeclaringType.FullName == "System.Reflection.FieldInfo")
            {
                callGetValue = instr;
                break;
            }
        }

        if (callGetValue == null)
        {
            throw new Exception("Could not find call to FieldInfo::GetValue.");
        }

        Instruction[] patchZone = [
            callGetValue.Previous.Previous,
            callGetValue.Previous,
            callGetValue,
            callGetValue.Next,
            ];

        // validate
        if (!patchZone[0].OpCode.Name.ToLower().StartsWith("ldloc"))
        {
            throw new Exception("First instruction not Ldloc");
        }
        if (!patchZone[1].OpCode.Name.ToLower().StartsWith("ldarg"))
        {
            throw new Exception("Second instruction not Ldarg");
        }
        if (!patchZone[3].OpCode.Name.ToLower().StartsWith("unbox"))
        {
            throw new Exception("Fourth instruction not unbox");
        }

        // Currently
        // [...]
        // ->(loadFieldInfo) [..., fieldInfo]
        // ->(loadObject) [..., fieldInfo, obj]
        // ->(callGetValue) [..., fieldValue_boxed]
        // ->(unbox) [..., fieldValue_unboxed]

        // Should become
        // [...]
        // ->(loadObject) [..., obj]
        // ->(loadFieldInfo) [..., obj, fieldInfo]
        // ->(callvirt) [..., obj, fieldName]
        // ->(call GetVariable) [..., fieldValue]

        // Swap the first two
        OpCode tmpOpcode = patchZone[1].OpCode;
        object tmpOperand = patchZone[1].Operand;
        patchZone[1].OpCode = patchZone[0].OpCode;
        patchZone[1].Operand = patchZone[0].Operand;
        patchZone[0].OpCode = tmpOpcode;
        patchZone[0].Operand = tmpOperand;

        // Replace the third with a call to FieldInfo.get_Name
        MethodInfo fieldNameMethodInfo = typeof(FieldInfo).GetProperty(nameof(FieldInfo.Name)).GetGetMethod();
        MethodReference fieldNameMethodRef = mod.ImportReference(fieldNameMethodInfo);

        patchZone[2].OpCode = OpCodes.Callvirt; // Technically not needed
        patchZone[2].Operand = fieldNameMethodRef;

        // Replace the fourth with call VariableExtensions.GetVariable<T>
        patchZone[3].OpCode = OpCodes.Call;
        patchZone[3].Operand = getVariableMethod;

        method.Body.OptimizeMacros();
    }
}
