using Mono.Cecil;
using System.Collections.Generic;

namespace SilksongPrepatcher.Utils
{
    public static class CecilUtils
    {
        /// <summary>
        /// Get all types in a module, including nested types.
        /// </summary>
        public static IEnumerable<TypeDefinition> GetTypeDefinitions(ModuleDefinition mod)
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

        public static IEnumerable<MethodDefinition> GetMethodDefinitions(ModuleDefinition mod)
        {
            foreach (TypeDefinition td in GetTypeDefinitions(mod))
            {
                foreach (MethodDefinition md in td.Methods)
                {
                    yield return md;
                }
            }
        }
    }
}
