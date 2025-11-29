using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using SilksongPrepatcher.Patchers;
using SilksongPrepatcher.Patchers.PlayerDataPatcher;
using AssemblyExtensions = SilksongPrepatcher.Utils.AssemblyExtensions;

namespace SilksongPrepatcher;

public static class SilksongPrepatcher
{
    private static ManualLogSource Log { get; } =
        Logger.CreateLogSource(nameof(SilksongPrepatcher));

    private static List<(string assemblyName, BasePrepatcher patcher)> GetPatcherData()
    {
        // If some of the type names action data in an FSM prefab is corrupted or incorrect,
        // Playmaker will search all assemblies for all types to try to find the correct type.
        // Certainly the MMHOOK assemblies don't contain the correct type, and the
        // MMHOOK_Assembly-CSharp assembly is quite large and it is undesirable to load the whole assembly.
        //
        // We shouldn't skip all assemblies just in case someone decides to ship fsms with custom
        // fsm state actions in an asset bundle.
        BasePrepatcher GetTypesMMHookIgnorer = new MethodReplacer(
            mr => mr.DeclaringType.Name == nameof(Assembly) && mr.Name == nameof(Assembly.GetTypes),
            typeof(AssemblyExtensions).GetMethod(
                nameof(AssemblyExtensions.GetTypesSafelyIgnoreMMHook),
                [typeof(Assembly)]
            )
        );

        // NestedFadeGroup searches all assemblies for a NestedFadeGroupBridgeAttribute - I don't
        // think they do very much with this though that isn't implicit in the RequireComponent attribute
        // and it's very unlikely modded assemblies will need it so I think the performance boost from
        // skipping modded assemblies (about 3 seconds at startup) is worth it.
        BasePrepatcher GetTypesModdedIgnorer = new MethodReplacer(
            mr => mr.DeclaringType.Name == nameof(Assembly) && mr.Name == nameof(Assembly.GetTypes),
            typeof(AssemblyExtensions).GetMethod(
                nameof(AssemblyExtensions.GetTypesSafelyIgnoreMMHook),
                [typeof(Assembly)]
            )
        );

        // Patch calls to Type.IsAssignableFrom so that they return false if the second type is not loadable.
        //
        // In some cases, such as when T is otherwise loadable but has a nested class with a static field of
        // type that is not loadable, T will be allowed through GetTypes but will then throw in IsAssignableFrom.
        BasePrepatcher NewtonsoftUnityPatcher = new MethodReplacer(
            mr => mr.DeclaringType.Name == nameof(Type) && mr.Name == nameof(Type.IsAssignableFrom),
            typeof(AssemblyExtensions).GetMethod(
                nameof(AssemblyExtensions.TypeAssignableFromSafe),
                [typeof(Type), typeof(Type)]
            )
        );

        return new()
        {
            (AssemblyNames.TeamCherry_NestedFadeGroup, GetTypesModdedIgnorer),
            (AssemblyNames.PlayMaker, GetTypesMMHookIgnorer),
            (AssemblyNames.PlayMaker, new ReflectionUtilsPatcher()),
            (AssemblyNames.Assembly_CSharp, new PlayerDataPatcher()),
            (AssemblyNames.TeamCherry_SharedUtils, new VariableExtensionsPatcher()),
            (AssemblyNames.Newtonsoft_Json_UnityConverters, NewtonsoftUnityPatcher),
        };
    }

    private static readonly List<(string assemblyName, BasePrepatcher patcher)> patcherData =
        GetPatcherData();

    internal static string PatchCacheDir
    {
        get
        {
            string path = Path.Combine(Paths.CachePath, nameof(SilksongPrepatcher));
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static IEnumerable<string> TargetDLLs =>
        patcherData.Select(pair => pair.assemblyName).Distinct();

    public static void Patch(AssemblyDefinition assembly)
    {
        string assemblyName = $"{assembly.Name.Name}.dll";

        List<BasePrepatcher> patchers = patcherData
            .Where(pair => pair.assemblyName == assemblyName)
            .Select(pair => pair.patcher)
            .ToList();

        Log.LogInfo($"Patching {assemblyName}: {string.Join(", ", patchers.Select(x => x.Name))}");

        foreach (BasePrepatcher patcher in patchers)
        {
            patcher.PatchAssembly(assembly);
        }
    }
}
