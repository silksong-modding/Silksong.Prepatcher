using BepInEx;

namespace PrepatcherPlugin;

/// <summary>
/// Plugin for utils associated with the Prepatcher.
/// </summary>
[BepInAutoPlugin(id: "io.github.silksong-modding.prepatcherplugin")]
public partial class PrepatcherPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // We only init hooks when the variable events are subscribed to
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}
