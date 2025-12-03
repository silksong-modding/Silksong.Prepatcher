using System.ComponentModel;

namespace PrepatcherPlugin;

/// <summary>
/// Class containing methods for accessing Player Data values that replace default methods.
/// </summary>
public static partial class PlayerDataInternal
{
    /// <summary>
    /// <see cref="PlayerData.IncrementInt(string)" />, routed via IntAdd.
    /// 
    /// Mods should use the original PlayerData method, not this one.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void IncrementInt(PlayerData pd, string intName)
    {
        IntAdd(pd, intName, 1);
    }

    /// <summary>
    /// <see cref="PlayerData.DecrementInt(string)" />, routed via IntAdd.
    /// 
    /// Mods should use the original PlayerData method, not this one.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void DecrementInt(PlayerData pd, string intName)
    {
        IntAdd(pd, intName, -1);
    }

    /// <summary>
    /// <see cref="PlayerData.IntAdd(string, int)"/>, routed via GetInt and SetInt.
    /// 
    /// Mods should use the original PlayerData method, not this one.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void IntAdd(PlayerData pd, string intName, int amount)
    {
        int current = pd.GetInt(intName);
        int next = current + amount;
        pd.SetInt(intName, next);
    }
}