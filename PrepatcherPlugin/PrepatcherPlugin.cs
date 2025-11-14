using BepInEx;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace PrepatcherPlugin;

/// <summary>
/// Plugin for utils associated with the Prepatcher.
/// </summary>
[BepInAutoPlugin(id: "org.silksong-modding.prepatcher")]
public partial class PrepatcherPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // We only init hooks when the variable events are subscribed to
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            for (int j  = 0; j < 5; j++)
            {
                PlayerData pd = PlayerData.instance;
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 100_00; i++)
                {
                    pd.GetBool(nameof(PlayerData.shakraAidForumBattle));
                }
                sw.Stop();
                Logger.LogInfo($"GetBool {j} time {sw.ElapsedMilliseconds}");
            }

            for (int j = 0; j < 5; j++)
            {
                PlayerData pd = PlayerData.instance;
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 100_00; i++)
                {
                    PlayerDataInternal.GetBool(pd, nameof(PlayerData.shakraAidForumBattle));
                }
                sw.Stop();
                Logger.LogInfo($"GetBoolInternal {j} time {sw.ElapsedMilliseconds}");
            }

            List<string> boolFields = typeof(PlayerData).GetFields().Where(f => f.FieldType == typeof(bool)).Select(fi => fi.Name).ToList();

            for (int j = 0; j < 5; j++)
            {
                PlayerData pd = PlayerData.instance;
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 10; i++)
                {
                    foreach (string fName in boolFields)
                    {
                        pd.GetBool(fName);
                    }
                }
                sw.Stop();
                Logger.LogInfo($"GetBoolAll {j} time {sw.ElapsedMilliseconds}");
            }

            for (int j = 0; j < 5; j++)
            {
                PlayerData pd = PlayerData.instance;
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 10; i++)
                {
                    foreach (string fName in boolFields)
                    {
                        PlayerDataInternal.GetBool(pd, fName);
                    }
                }
                sw.Stop();
                Logger.LogInfo($"GetBoolAllInternal {j} time {sw.ElapsedMilliseconds}");
            }
        }
    }
}
