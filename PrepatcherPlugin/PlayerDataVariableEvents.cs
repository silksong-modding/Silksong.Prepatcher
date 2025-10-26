using BepInEx.Logging;
using System;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace PrepatcherPlugin;

public static class PlayerDataVariableEvents<T>
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(PlayerDataVariableEvents));

    public delegate T PlayerDataVariableHandler(PlayerData pd, string fieldName, T current);

    private static PlayerDataVariableHandler? _onGetVariable;
    private static PlayerDataVariableHandler? _onSetVariable;

    public static event PlayerDataVariableHandler? OnGetVariable
    {
        add
        {
            Hooks.Init();
            _onGetVariable += value;
        }
        remove
        {
            _onGetVariable -= value;
        }
    }
    public static event PlayerDataVariableHandler? OnSetVariable
    {
        add
        {
            Hooks.Init();
            _onSetVariable += value;
        }
        remove
        {
            _onSetVariable -= value;
        }
    }

    internal static T ModifyGetVariable(PlayerData pd, string fieldName, T current)
    {
        if (_onGetVariable == null)
        {
            return current;
        }

        foreach (PlayerDataVariableHandler handler in _onGetVariable.GetInvocationList().Cast<PlayerDataVariableHandler>())
        {
            try
            {
                current = handler(pd, fieldName, current);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error invoking {nameof(ModifyGetVariable)}, {typeof(T).Name}\n" + ex);
            }
        }

        return current;
    }

    internal static object? ModifyGetVariableNonGeneric(PlayerData pd, string fieldName, object current) 
    { 
        return ModifyGetVariable(pd, fieldName, (T)current);
    }

    internal static T ModifySetVariable(PlayerData pd, string fieldName, T current)
    {
        if (_onSetVariable == null)
        {
            return current;
        }

        foreach (PlayerDataVariableHandler handler in _onSetVariable.GetInvocationList())
        {
            try
            {
                current = handler(pd, fieldName, current);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error invoking {nameof(ModifyGetVariable)}, {typeof(T).Name}\n" + ex);
            }
        }

        return current;
    }

    internal static object? ModifySetVariableNonGeneric(PlayerData pd, string fieldName, object current)
    {
        return ModifySetVariable(pd, fieldName, (T)current);
    }
}

public static class PlayerDataVariableEvents
{
    // Bool
    public static event PlayerDataVariableEvents<bool>.PlayerDataVariableHandler? OnGetBool
    {
        add => PlayerDataVariableEvents<bool>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<bool>.OnGetVariable -= value;
    }
    public static event PlayerDataVariableEvents<bool>.PlayerDataVariableHandler? OnSetBool
    {
        add => PlayerDataVariableEvents<bool>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<bool>.OnSetVariable -= value;
    }

    // Int
    public static event PlayerDataVariableEvents<int>.PlayerDataVariableHandler? OnGetInt
    {
        add => PlayerDataVariableEvents<int>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<int>.OnGetVariable -= value;
    }
    public static event PlayerDataVariableEvents<int>.PlayerDataVariableHandler? OnSetInt
    {
        add => PlayerDataVariableEvents<int>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<int>.OnSetVariable -= value;
    }

    // String
    public static event PlayerDataVariableEvents<string>.PlayerDataVariableHandler? OnGetString
    {
        add => PlayerDataVariableEvents<string>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<string>.OnGetVariable -= value;
    }
    public static event PlayerDataVariableEvents<string>.PlayerDataVariableHandler? OnSetString
    {
        add => PlayerDataVariableEvents<string>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<string>.OnSetVariable -= value;
    }

    // Float
    public static event PlayerDataVariableEvents<float>.PlayerDataVariableHandler? OnGetFloat
    {
        add => PlayerDataVariableEvents<float>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<float>.OnGetVariable -= value;
    }
    public static event PlayerDataVariableEvents<float>.PlayerDataVariableHandler? OnSetFloat
    {
        add => PlayerDataVariableEvents<float>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<float>.OnSetVariable -= value;
    }

    // Vector3
    public static event PlayerDataVariableEvents<Vector3>.PlayerDataVariableHandler? OnGetVector3
    {
        add => PlayerDataVariableEvents<Vector3>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<Vector3>.OnGetVariable -= value;
    }
    public static event PlayerDataVariableEvents<Vector3>.PlayerDataVariableHandler? OnSetVector3
    {
        add => PlayerDataVariableEvents<Vector3>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<Vector3>.OnSetVariable -= value;
    }
}
