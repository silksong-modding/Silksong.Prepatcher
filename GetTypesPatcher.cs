using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Silksong.Prepatcher
{
    internal static class GetTypesPatcher
    {
        private static ManualLogSource Log = Logger.CreateLogSource($"Silksong.Prepatcher.{nameof(GetTypesPatcher)}");

        public static Type[] GetTypesSafely(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x is not null).ToArray();
            }
        }

        /// <summary>
        /// Get all types in a module, including nested types.
        /// </summary>
        public static IEnumerable<TypeReference> GetTypeReferences(ModuleDefinition mod)
        {
            Queue<TypeDefinition> targets = new(mod.Types);

            while (targets.Count > 0)
            {
                TypeDefinition type = targets.Dequeue();

                if (type.IsInterface) { continue; }
                yield return type;

                foreach (TypeDefinition nested in type.NestedTypes)
                {
                    targets.Enqueue(nested);
                }
            }
        }

        public static void PatchAssembly(AssemblyDefinition asm)
        {
            MethodInfo newMethodInfo = typeof(GetTypesPatcher).GetMethod(nameof(GetTypesSafely), [typeof(Assembly)]);
            MethodReference newMethodRef = asm.MainModule.ImportReference(newMethodInfo);

            foreach (TypeDefinition type in GetTypeReferences(asm.MainModule))
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
}
