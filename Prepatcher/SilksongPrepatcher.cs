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
        BasePrepatcher GetTypesPatcher = new MethodReplacer(
            mr => mr.DeclaringType.Name == nameof(Assembly) && mr.Name == nameof(Assembly.GetTypes),
            typeof(AssemblyExtensions).GetMethod(
                nameof(AssemblyExtensions.GetTypesSafelyIgnoreMMHook),
                [typeof(Assembly)]
            )
        );
        BasePrepatcher NewtonsoftUnityPatcher = new MethodReplacer(
            mr => mr.DeclaringType.Name == nameof(Type) && mr.Name == nameof(Type.IsAssignableFrom),
            typeof(AssemblyExtensions).GetMethod(
                nameof(AssemblyExtensions.TypeAssignableFromSafe),
                [typeof(Type), typeof(Type)]
            )
        );

        return new()
        {
            (AssemblyNames.TeamCherry_NestedFadeGroup, GetTypesPatcher),
            (AssemblyNames.PlayMaker, GetTypesPatcher),
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
