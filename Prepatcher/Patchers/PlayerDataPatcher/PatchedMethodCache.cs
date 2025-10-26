using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace SilksongPrepatcher.Patchers.PlayerDataPatcher;

public class PatchedMethodCache
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(
        nameof(PatchedMethodCache)
    );

    // Dictionary [typeRef.FullName] -> List<methodRef.FullName>
    public Dictionary<string, List<string>> PatchedMethods { get; set; } = new();

    public void Add(string typeName, string methodName)
    {
        if (!PatchedMethods.TryGetValue(typeName, out List<string> methods))
            methods = PatchedMethods[typeName] = new();
        methods.Add(methodName);
    }

    public static string GetMetadataString()
    {
        string thisAssemblyVersion = typeof(PatchedMethodCache)
            .Assembly.GetName()
            .Version.ToString();

        string assemblyCsharpPath = Path.Combine(Paths.ManagedPath, AssemblyNames.Assembly_CSharp);
        DateTime assemblyCSharpModTime = File.GetLastWriteTimeUtc(assemblyCsharpPath);

        return $"{thisAssemblyVersion} // {assemblyCSharpModTime}";
    }

    // Annoyingly there isn't any JSON available by default
    public void Serialize(string filePath)
    {
        StringBuilder sb = new();

        sb.AppendLine($"X {GetMetadataString()}");

        foreach ((string typeName, List<string> methods) in PatchedMethods)
        {
            sb.AppendLine($"T {typeName}");
            foreach (string methodName in methods)
            {
                sb.AppendLine($"M {methodName}");
            }
            sb.AppendLine($"E ");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public static PatchedMethodCache? Deserialize(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.LogDebug($"Failed to deserialize: no method cache found in cache dir.");
            return null;
        }

        bool validated = false;
        PatchedMethodCache ret = new();

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            string? key = null;
            List<string> current = [];

            foreach (string line in lines)
            {
                if (line.StartsWith("X "))
                {
                    string metadata = line.Substring(2);
                    if (metadata == GetMetadataString())
                    {
                        validated = true;
                    }
                    else
                    {
                        Log.LogDebug("Failed to deserialize: metadata mismatch.");
                        return null;
                    }
                }
                else if (line.StartsWith("T "))
                {
                    key = line.Substring(2);
                }
                else if (line.StartsWith("M "))
                {
                    current.Add(line.Substring(2));
                }
                else if (line.StartsWith("E "))
                {
                    if (key == null)
                        throw new Exception("Key null on write");
                    ret.PatchedMethods[key] = current;
                    key = null;
                    current = new();
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError("Failed to deserialize:\n" + ex);
            return null;
        }

        if (!validated)
        {
            Log.LogInfo("Failed to deserialize: no metadata found.");
            return null;
        }

        return ret;
    }
}
