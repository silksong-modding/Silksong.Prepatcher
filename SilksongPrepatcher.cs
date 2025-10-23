using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using SilksongPrepatcher.Patchers;
using SilksongPrepatcher.Patchers.PlayerDataPatcher;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SilksongPrepatcher;

public static class SilksongPrepatcher
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(SilksongPrepatcher));

    private static readonly List<(string assemblyName, BasePrepatcher patcher)> patcherData = new()
    {
        (AssemblyNames.TeamCherry_NestedFadeGroup, new GetTypesPatcher()),
        (AssemblyNames.PlayMaker, new GetTypesPatcher()),
        (AssemblyNames.PlayMaker, new ReflectionUtilsPatcher()),
        (AssemblyNames.Assembly_CSharp, new PlayerDataPatcher()),
        (AssemblyNames.TeamCherry_SharedUtils, new VariableExtensionsPatcher()),
    };

    internal static string PatchCacheDir
    {
        get
        {
            string path = Path.Combine(Paths.CachePath, "SilksongPrepatcher");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static IEnumerable<string> TargetDLLs => patcherData.Select(pair => pair.assemblyName).Distinct();

    public static void Patch(AssemblyDefinition assembly)
    {
        string assemblyName = assembly.Name.Name;

        List<BasePrepatcher> patchers = patcherData
            .Where(pair => pair.assemblyName == assemblyName)
            .Select(pair => pair.patcher)
            .ToList();

        Log.LogInfo($"Patching {assemblyName}: {string.Join(", ", patchers.Select(x => x.Name))}");

        foreach (BasePrepatcher patcher in patchers)
        {
            patcher.PatchAssembly(assembly);
        }

        if (Config.Instance.WritePatchedAssemblies.Value)
        {
            assembly.MainModule.Write(Path.Combine(PatchCacheDir, $"{nameof(SilksongPrepatcher)}_{assemblyName}"));
        }
    }
}
