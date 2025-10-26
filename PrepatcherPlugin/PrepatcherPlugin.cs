using BepInEx;

namespace PrepatcherPlugin;

// TODO - adjust the plugin guid as needed
[BepInAutoPlugin(id: "io.github.silksong-modding.prepatcherplugin")]
public partial class PrepatcherPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Put your initialization logic here
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}
