using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamCherry.SharedUtils;
using UnityEngine;
using NonGenericVariableHandler = System.Func<PlayerData, string, object, object>;

namespace PrepatcherPlugin;

/// <summary>
/// This class manages actually managing the Hooks.
/// For the public API see <see cref="PlayerDataVariableEvents"/> and <see cref="PlayerDataVariableEvents{T}"/>.
/// </summary>
internal static class Hooks
{
    private static readonly List<Hook> hooks = [];

    internal static void Init()
    {
        // Make Init idempotent
        if (hooks.Count > 0) return;

        // Bool
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.GetBool)),
            ModifyGetBool
        ));
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.SetBool)),
            ModifySetBool
        ));

        // Int
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.GetInt)),
            ModifyGetInt
        ));
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.SetInt)),
            ModifySetInt
        ));

        // String
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.GetString)),
            ModifyGetString
        ));
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.SetString)),
            ModifySetString
        ));

        // Float
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.GetFloat)),
            ModifyGetFloat
        ));
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.SetFloat)),
            ModifySetFloat
        ));

        // Vector3
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.GetVector3)),
            ModifyGetVector3
        ));
        hooks.Add(new(
            typeof(PlayerData).GetMethod(nameof(PlayerData.SetVector3)),
            ModifySetVector3
        ));

        // Generic
        hooks.Add(new(
            typeof(VariableExtensions).GetMethods().First(m => m.Name == nameof(VariableExtensions.GetVariable) && !m.IsGenericMethod),
            ModifyGetVariable
        ));
        hooks.Add(new(
            typeof(VariableExtensions).GetMethods().First(m => m.Name == nameof(VariableExtensions.SetVariable) && !m.IsGenericMethod),
            ModifySetVariable
        ));
    }


    // Bool
    private static bool ModifyGetBool(Func<PlayerData, string, bool> orig, PlayerData self, string boolName)
    {
        bool current = orig(self, boolName);
        bool modified = PlayerDataVariableEvents<bool>.ModifyGetVariable(self, boolName, current);
        return modified;
    }

    private static void ModifySetBool(Action<PlayerData, string, bool> orig, PlayerData self, string boolName, bool value)
    {
        bool modified = PlayerDataVariableEvents<bool>.ModifySetVariable(self, boolName, value);
        orig(self, boolName, modified);
    }

    // Int
    private static int ModifyGetInt(Func<PlayerData, string, int> orig, PlayerData self, string intName)
    {
        int current = orig(self, intName);
        int modified = PlayerDataVariableEvents<int>.ModifyGetVariable(self, intName, current);
        return modified;
    }

    private static void ModifySetInt(Action<PlayerData, string, int> orig, PlayerData self, string intName, int value)
    {
        int modified = PlayerDataVariableEvents<int>.ModifySetVariable(self, intName, value);
        orig(self, intName, modified);
    }

    // String
    private static string ModifyGetString(Func<PlayerData, string, string> orig, PlayerData self, string stringName)
    {
        string current = orig(self, stringName);
        string modified = PlayerDataVariableEvents<string>.ModifyGetVariable(self, stringName, current);
        return modified;
    }

    private static void ModifySetString(Action<PlayerData, string, string> orig, PlayerData self, string stringName, string value)
    {
        string modified = PlayerDataVariableEvents<string>.ModifySetVariable(self, stringName, value);
        orig(self, stringName, modified);
    }

    // Float
    private static float ModifyGetFloat(Func<PlayerData, string, float> orig, PlayerData self, string floatName)
    {
        float current = orig(self, floatName);
        float modified = PlayerDataVariableEvents<float>.ModifyGetVariable(self, floatName, current);
        return modified;
    }

    private static void ModifySetFloat(Action<PlayerData, string, float> orig, PlayerData self, string floatName, float value)
    {
        float modified = PlayerDataVariableEvents<float>.ModifySetVariable(self, floatName, value);
        orig(self, floatName, modified);
    }

    // Vector3
    private static Vector3 ModifyGetVector3(Func<PlayerData, string, Vector3> orig, PlayerData self, string vector3Name)
    {
        Vector3 current = orig(self, vector3Name);
        Vector3 modified = PlayerDataVariableEvents<Vector3>.ModifyGetVariable(self, vector3Name, current);
        return modified;
    }

    private static void ModifySetVector3(Action<PlayerData, string, Vector3> orig, PlayerData self, string vector3Name, Vector3 value)
    {
        Vector3 modified = PlayerDataVariableEvents<Vector3>.ModifySetVariable(self, vector3Name, value);
        orig(self, vector3Name, modified);
    }

    // Generic
    private static readonly Dictionary<Type, NonGenericVariableHandler> GenericGetMethodCache = [];
    private static readonly Dictionary<Type, NonGenericVariableHandler> GenericSetMethodCache = [];

    private static object ModifyGetVariable(Func<IIncludeVariableExtensions, string, Type, object> orig, IIncludeVariableExtensions obj, string fieldName, Type type)
    {
        object current = orig(obj, fieldName, type);
        if (obj is not PlayerData pd)
        {
            return current;
        }

        if (!GenericGetMethodCache.TryGetValue(type, out NonGenericVariableHandler func))
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            Type genericEventsType = typeof(PlayerDataVariableEvents<>).MakeGenericType(type);
            MethodInfo mi = genericEventsType
                .GetMethod(nameof(PlayerDataVariableEvents<object>.ModifyGetVariableNonGeneric), flags);
            func = (NonGenericVariableHandler)
                Delegate.CreateDelegate(typeof(NonGenericVariableHandler), mi);
            GenericGetMethodCache.Add(type, func);
        }

        object modified = func(pd, fieldName, current);
        return modified;
    }
    private static void ModifySetVariable(Action<IIncludeVariableExtensions, string, object, Type> orig, IIncludeVariableExtensions obj, string fieldName, object value, Type type)
    {
        if (obj is not PlayerData pd)
        {
            orig(obj, fieldName, value, type);
            return;
        }

        if (!GenericSetMethodCache.TryGetValue(type, out NonGenericVariableHandler func))
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            Type genericEventsType = typeof(PlayerDataVariableEvents<>).MakeGenericType(type);
            MethodInfo mi = genericEventsType
                .GetMethod(nameof(PlayerDataVariableEvents<object>.ModifySetVariableNonGeneric), flags);
            func = (NonGenericVariableHandler)Delegate.CreateDelegate(typeof(NonGenericVariableHandler), mi);
            GenericSetMethodCache.Add(type, func);
        }

        object modified = func(pd, fieldName, value);

        orig(obj, fieldName, modified, type);
    }
}
