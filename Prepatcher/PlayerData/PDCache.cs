using System.Collections.Generic;
using HarmonyLib;

namespace SilksongPrepatcher.PlayerData;

public static class PDCache
{
    private static readonly Dictionary<string, AccessTools.FieldRef<object, bool>> cachedBools = [];
    private static readonly Dictionary<string, AccessTools.FieldRef<object, int>> cachedInts = [];
    private static readonly Dictionary<
        string,
        AccessTools.FieldRef<object, string>
    > cachedStrings = [];
    private static readonly Dictionary<string, AccessTools.FieldRef<object, float>> cachedFloats =
    [];

    public static bool GetBool(object pd, string fieldName)
    {
        if (!cachedBools.TryGetValue(fieldName, out AccessTools.FieldRef<object, bool> access))
        {
            access = AccessTools.FieldRefAccess<bool>(pd.GetType(), fieldName);
            cachedBools[fieldName] = access;
        }

        return access(pd);
    }

    public static void SetBool(object pd, string fieldName, bool value)
    {
        if (!cachedBools.TryGetValue(fieldName, out AccessTools.FieldRef<object, bool> access))
        {
            access = AccessTools.FieldRefAccess<bool>(pd.GetType(), fieldName);
            cachedBools[fieldName] = access;
        }

        access(pd) = value;
    }

    public static int GetInt(object pd, string fieldName)
    {
        if (!cachedInts.TryGetValue(fieldName, out AccessTools.FieldRef<object, int> access))
        {
            access = AccessTools.FieldRefAccess<int>(pd.GetType(), fieldName);
            cachedInts[fieldName] = access;
        }

        return access(pd);
    }

    public static void SetInt(object pd, string fieldName, int value)
    {
        if (!cachedInts.TryGetValue(fieldName, out AccessTools.FieldRef<object, int> access))
        {
            access = AccessTools.FieldRefAccess<int>(pd.GetType(), fieldName);
            cachedInts[fieldName] = access;
        }

        access(pd) = value;
    }

    public static string GetString(object pd, string fieldName)
    {
        if (!cachedStrings.TryGetValue(fieldName, out AccessTools.FieldRef<object, string> access))
        {
            access = AccessTools.FieldRefAccess<string>(pd.GetType(), fieldName);
            cachedStrings[fieldName] = access;
        }

        return access(pd);
    }

    public static void SetString(object pd, string fieldName, string value)
    {
        if (!cachedStrings.TryGetValue(fieldName, out AccessTools.FieldRef<object, string> access))
        {
            access = AccessTools.FieldRefAccess<string>(pd.GetType(), fieldName);
            cachedStrings[fieldName] = access;
        }
        access(pd) = value;
    }

    public static float GetFloat(object pd, string fieldName)
    {
        if (!cachedFloats.TryGetValue(fieldName, out AccessTools.FieldRef<object, float> access))
        {
            access = AccessTools.FieldRefAccess<float>(pd.GetType(), fieldName);
            cachedFloats[fieldName] = access;
        }

        return access(pd);
    }

    public static void SetFloat(object pd, string fieldName, float value)
    {
        if (!cachedFloats.TryGetValue(fieldName, out AccessTools.FieldRef<object, float> access))
        {
            access = AccessTools.FieldRefAccess<float>(pd.GetType(), fieldName);
            cachedFloats[fieldName] = access;
        }

        access(pd) = value;
    }
}
