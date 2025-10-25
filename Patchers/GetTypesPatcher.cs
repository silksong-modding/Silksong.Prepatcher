using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssemblyExtensions = SilksongPrepatcher.Utils.AssemblyExtensions;
using SilksongPrepatcher.Utils;

namespace SilksongPrepatcher.Patchers;

public class GetTypesPatcher : BasePrepatcher
{
    public override void PatchAssembly (AssemblyDefinition asm)
    {
        MethodInfo newMethodInfo = typeof(AssemblyExtensions).GetMethod(nameof(AssemblyExtensions.GetTypesSafelyIgnoreMMHook), [typeof(Assembly)]);
        MethodReference newMethodRef = asm.MainModule.ImportReference(newMethodInfo);

        foreach (TypeDefinition type in CecilUtils.GetTypeDefinitions(asm.MainModule))
        {
            foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
            {
                Collection<Instruction> instructions = method.Body.Instructions;

                for (int i = 0; i < instructions.Count; i++)
                {
                    Instruction instruction = instructions[i];

                    if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                        instruction.Operand is MethodReference mr)
                    {
                        if (mr.Name == nameof(Assembly.GetTypes) && mr.DeclaringType.Name == "Assembly")
                        {
                            instruction.OpCode = OpCodes.Call;
                            instruction.Operand = newMethodRef;

                            Log.LogInfo($"Patching {type.Name}:{method.Name}");
                        }
                    }
                }

                method.Body.OptimizeMacros();
            }
        }
    }
}
