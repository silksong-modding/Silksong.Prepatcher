namespace PrepatcherPlugin;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static partial class PlayerDataInternal
{

    public static void IncrementInt(PlayerData pd, string intName)
    {
        IntAdd(pd, intName, 1);
    }

    public static void DecrementInt(PlayerData pd, string intName)
    {
        IntAdd(pd, intName, -1);
    }

    public static void IntAdd(PlayerData pd, string intName, int amount)
    {
        int current = pd.GetInt(intName); // GetIntInternal;
        int next = current + amount;
        pd.SetInt(intName, next);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
