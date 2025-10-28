using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using SilksongPrepatcher.Utils;
using AssemblyExtensions = SilksongPrepatcher.Utils.AssemblyExtensions;

namespace SilksongPrepatcher.Patchers;

public class NewtonsoftUnityPatcher : BasePrepatcher
{
    public override void PatchAssembly(AssemblyDefinition asm)
    {
        MethodInfo newMethodInfo = typeof(AssemblyExtensions).GetMethod(
            nameof(AssemblyExtensions.TypeAssignableFrom),
            [typeof(Type), typeof(Type)]
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
                            mr.Name == nameof(Type.IsAssignableFrom)
                            && mr.DeclaringType.Name == nameof(Type)
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
