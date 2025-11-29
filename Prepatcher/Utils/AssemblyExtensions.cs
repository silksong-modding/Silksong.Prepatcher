using System;
using System.Linq;
using System.Reflection;

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
        if (asm.GetName().Name.StartsWith("MMHOOK"))
        {
            return [];
        }

        return asm.GetTypesSafely();
    }

    public static Type[] GetTypesSafelyIgnoreModded(Assembly asm)
    {
        // Base game assemblies shouldn't need to be aware of modded assemblies,
        // so this is safe to do

        string asmName = asm.GetName().Name;
        if (
            asmName.StartsWith("TeamCherry")
            || asmName == "Assembly-CSharp"
            || asmName == "PlayMaker"
        )
        {
            // This *should* avoid false negatives in the case
            // that someone put their install inside a folder called BepInEx
            return asm.GetTypesSafely();
        }

        if (asm.Location.Contains("BepInEx"))
        {
            return [];
        }

        return asm.GetTypesSafely();
    }

    public static bool TypeAssignableFromSafe(Type self, Type other)
    {
        try
        {
            return self.IsAssignableFrom(other);
        }
        catch
        {
            // This only happens if other is not loadable, so we should just eat the error and return false
            return false;
        }
    }
}
