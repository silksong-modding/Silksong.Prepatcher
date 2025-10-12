using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silksong.Prepatcher
{
    public static class Util
    {
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
    }

    public static class AssemblyPatcher
    {
        private static ManualLogSource Log = Logger.CreateLogSource("Silksong.AssemblyPatcher");

        public static IEnumerable<string> TargetDLLs { get; } = new[] { "TeamCherry.NestedFadeGroup.dll" };

        private static IEnumerable<TypeReference> GetTypeReferences(ModuleDefinition mod)
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

        public static void Patch(AssemblyDefinition assembly)
        {
            MethodReference safeGetTypesRef;

            try
            {
                MethodInfo safeMethod = typeof(Util).GetMethod(nameof(Util.GetTypesSafely), [typeof(Assembly)]);

                if (safeMethod == null)
                {
                    Log.LogError($"Could not find method: {nameof(Util.GetTypesSafely)}.");
                    return;
                }

                safeGetTypesRef = assembly.MainModule.ImportReference(safeMethod);
            }
            catch (Exception ex)
            {
                Log.LogError($"Fatal error during reference loading: {ex.Message}");
                return;
            }


            int patchCount = 0;

            foreach (TypeDefinition type in GetTypeReferences(assembly.MainModule))
            {
                foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
                {
                    Log.LogInfo($"  Method {method.Name}");
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
                                instruction.Operand = safeGetTypesRef;

                                Log.LogInfo($"Patching {type.Name} {method.Name} {mr.Name}");
                                patchCount++;
                            }
                        }
                    }

                    method.Body.OptimizeMacros();
                }
            }

            Log.LogInfo($"Finished patching {assembly.Name.Name}. {patchCount} call(s) replaced.");
        }
    }
}