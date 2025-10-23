using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace SilksongPrepatcher;

public class Config
{
    public ConfigEntry<bool> WritePatchedAssemblies;

    private static readonly string ConfigDesc = """
        If enabled, will dump the output of the patchers to the BepInEx cache directory.
        This feature is mainly of use to modders inspecting the output of the patching procedure.
        """;

    public Config(ConfigFile cfg)
    {
        WritePatchedAssemblies = cfg.Bind("General", "WritePatchedAssemblies", false, ConfigDesc);
    }

    private static Config? _instance;
    public static Config Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new Config(new ConfigFile(Path.Combine(Paths.ConfigPath, "org.silksong-modding.prepatcher.cfg"), false));
            }

            return _instance;
        }
    }
}
