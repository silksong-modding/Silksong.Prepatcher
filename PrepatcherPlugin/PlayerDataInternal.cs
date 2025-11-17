namespace PrepatcherPlugin;

/// <summary>
/// Class containing methods for accessing Player Data values that replace default methods.
/// </summary>
public static partial class PlayerDataInternal
{
    /// <summary>
    /// pd.IncrementInt, routed via IntAdd
    /// </summary>
    public static void IncrementInt(PlayerData pd, string intName)
    {
        IntAdd(pd, intName, 1);
    }

    /// <summary>
    /// pd.DecrementInt, routed via IntAdd
    /// </summary>
    public static void DecrementInt(PlayerData pd, string intName)
    {
        IntAdd(pd, intName, -1);
    }

    /// <summary>
    /// pd.IntAdd, routed via GetInt and SetInt
    /// </summary>
    public static void IntAdd(PlayerData pd, string intName, int amount)
    {
        int current = pd.GetInt(intName);
        int next = current + amount;
        pd.SetInt(intName, next);
    }
}