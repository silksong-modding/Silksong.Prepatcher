using System;
using System.Reflection;
using System.Linq;

namespace SilksongPrepatcher.Utils;

public static class AssemblyExtensions
{
    public static Type[] GetTypesSafely(this Assembly asm)
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

    public static Type[] GetTypesSafelyIgnoreMMHook(Assembly asm)
    {
        // Base game assemblies shouldn't need to be aware of types in MMHOOK assemblies,
        // so this is safe to do
        if (asm.GetName().Name.StartsWith("MMHOOK"))
        {
            return [];
        }

        return asm.GetTypesSafely();
    }

}
