using System;
using System.Linq;
using System.Reflection;
using BepInEx;

namespace SilksongPrepatcher.Utils;

public static class AssemblyExtensions
{
    public const string IncludeInTypeSearchAttributeKey = "SilksongPrepatcher.IncludeInUnmoddedTypeSearch";

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

    public static Type[] GetTypesSafelyIgnoreModded(Assembly asm)
    {
        if (ShouldSkip(asm))
        {
            return [];
        }

        return asm.GetTypesSafely();
    }

    /// <summary>
    /// Check if the assembly should be skipped by functions that scan all assemblies for types.
    ///
    /// This patch will only be applied to Assembly.GetTypes at the call site in
    /// selected assemblies (e.g. TC assemblies and PlayMaker - see SilksongPrepatcher.cs for specifics).
    /// </summary>
    /// <param name="asm">The assembly.</param>
    /// <returns>True for assemblies that shouldn't have their types iterated over by the caller.</returns>
    private static bool ShouldSkip(Assembly asm)
    {
        string name = asm.GetName().Name;
        if (
            name.StartsWith("TeamCherry")
            || name == "PlayMaker"
            || name.StartsWith("Assembly-CSharp")
        )
        {
            // Always look at TC assemblies; these are the only assemblies that are actually needed by the relevant functions
            return false;
        }

        if (name.StartsWith("System.") || name == "mscorlib")
        {
            // Checking system assemblies takes significant time and are never needed by these functions
            return true;
        }

        {
            string location;
            try
            {
                location = asm.Location;
            }
            catch (NotSupportedException)
            {
                // Dynamic assembly
                location = string.Empty;
            }
            if (location.StartsWith(Paths.BepInExRootPath))
            {
                // Skip all assemblies in the BepInEx folder unless they have the custom attribute
                if (
                    asm.GetCustomAttributes<AssemblyMetadataAttribute>()
                        .Any(m => m.Key == IncludeInTypeSearchAttributeKey && m.Value == "True")
                )
                {
                    return false;
                }

                return true;
            }
        }

        // Default to false to avoid false positives
        return false;
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
