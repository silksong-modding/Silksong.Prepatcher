using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using SilksongPrepatcher.Utils;
using AssemblyExtensions = SilksongPrepatcher.Utils.AssemblyExtensions;

namespace SilksongPrepatcher.Patchers;

/// <summary>
/// Patch all calls to Assembly.GetTypes so they skip MMHOOK assemblies, and in general don't throw if some types fail to load.
///
/// If some of the action data in an FSM prefab is corrupted or incorrect, Playmaker will search all assemblies for all types
/// to try to find the correct type. Certainly the MMHOOK assemblies don't contain the correct type, and the
/// MMHOOK_Assembly-CSharp assembly is quite large and it is undesirable to load the whole assembly.
/// </summary>
public class GetTypesPatcher : BasePrepatcher
{
    public override void PatchAssembly(AssemblyDefinition asm)
    {
        MethodInfo newMethodInfo = typeof(AssemblyExtensions).GetMethod(
            nameof(AssemblyExtensions.GetTypesSafelyIgnoreMMHook),
            [typeof(Assembly)]
        );
        MethodReference newMethodRef = asm.MainModule.ImportReference(newMethodInfo);

        foreach (TypeDefinition type in CecilUtils.GetTypeDefinitions(asm.MainModule))
        {
            foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
            {
                Collection<Instruction> instructions = method.Body.Instructions;

                for (int i = 0; i < instructions.Count; i++)
                {
                    Instruction instruction = instructions[i];

                    if (
                        (
                            instruction.OpCode == OpCodes.Call
                            || instruction.OpCode == OpCodes.Callvirt
                        ) && instruction.Operand is MethodReference mr
                    )
                    {
                        if (
                            mr.Name == nameof(Assembly.GetTypes)
                            && mr.DeclaringType.Name == "Assembly"
                        )
                        {
                            instruction.OpCode = OpCodes.Call;
                            instruction.Operand = newMethodRef;

                            Log.LogInfo($"Patching {type.FullName} : {method.FullName}");
                        }
                    }
                }

                method.Body.OptimizeMacros();
            }
        }
    }
}
