using Mono.Cecil;
using System.Collections.Generic;

namespace SilksongPrepatcher.Utils;

public static class CecilUtils
{
    /// <summary>
    /// Yield the type, and all nested types
    /// </summary>
    public static IEnumerable<TypeDefinition> GetTypesRecursive(TypeDefinition type, bool includeSelf = true)
    {
        if (includeSelf) yield return type;

        foreach (TypeDefinition nested in type.NestedTypes)
        {
            foreach (TypeDefinition inner in GetTypesRecursive(nested, includeSelf: true))
            {
                yield return inner;
            }
        }
    }

    /// <summary>
    /// Get all types in a module, including nested types.
    /// </summary>
    public static IEnumerable<TypeDefinition> GetTypeDefinitions(ModuleDefinition mod)
    {
        foreach (TypeDefinition type in mod.Types)
        {
            foreach (TypeDefinition inner in GetTypesRecursive(type, includeSelf: true))
            {
                yield return inner;
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
