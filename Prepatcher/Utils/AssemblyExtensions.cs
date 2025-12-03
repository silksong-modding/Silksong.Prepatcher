using System;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;

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

    public static Type[] GetTypesSafelyIgnoreModded(Assembly asm)
    {
        if (ShouldSkip(asm, skipModdedAssemblies: true))
        {
            return [];
        }

        return asm.GetTypesSafely();
    }

    public static Type[] GetTypesSafelyIgnoreMMHOOK(Assembly asm)
    {
        if (ShouldSkip(asm, skipModdedAssemblies: false))
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
    /// <param name="skipModdedAssemblies">If this is true, then assemblies in the BepInEx dir will be skipped.</param>
    /// <returns>True for assemblies that shouldn't have their types iterated over by the caller.</returns>
    private static bool ShouldSkip(Assembly asm, bool skipModdedAssemblies)
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

        if (name.StartsWith("MMHOOK"))
        {
            // MMHOOK assemblies never need to be read
            return true;
        }

        if (asm.GetType("MonoDetour.HookGen.MonoDetourTargetsAttribute") != null)
        {
            // MonoDetour creates a lot of types so we should skip these in general
            return true;
        }

        if (skipModdedAssemblies)
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
            if (location.Contains("BepInEx"))
            {
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
