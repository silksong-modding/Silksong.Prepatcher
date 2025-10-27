using BepInEx.Logging;
using System;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace PrepatcherPlugin;

/// <summary>
/// Class holding the events for Get/Set of fields of the specified type.
/// </summary>
/// <typeparam name="T">The field type of the field to look at.</typeparam>
public static class PlayerDataVariableEvents<T>
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(PlayerDataVariableEvents));

    /// <summary>
    /// Delegate representing a function that can monitor and modify the value observed for the given field.
    /// </summary>
    /// <param name="pd">The PlayerData object. Typically this will be the same object as PlayerData.Instance.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="current">The value that would be got/set if this subscriber weren't in place.</param>
    /// <returns>The modified value. Subscribers should return <paramref name="current"/> to avoid changing anything.</returns>
    public delegate T PlayerDataVariableHandler(PlayerData pd, string fieldName, T current);

    private static PlayerDataVariableHandler? _onGetVariable;
    private static PlayerDataVariableHandler? _onSetVariable;

    /// <summary>
    /// Event to control the return value of PlayerData.GetVariable with generic type parameter <typeparamref name="T"/>.
    /// 
    /// If <typeparamref name="T"/> is bool, string, int, float or Vector3, this is equivalent
    /// to the relevant event on <see cref="PlayerDataVariableEvents"/>.
    /// </summary>
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

    /// <summary>
    /// Event to control the value set by PlayerData.SetVariable with generic type parameter <typeparamref name="T"/>.
    /// 
    /// If <typeparamref name="T"/> is bool, string, int, float or Vector3, this is equivalent
    /// to the relevant method on <see cref="PlayerDataVariableEvents"/>.
    /// </summary>
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

/// <summary>
/// Class holding non-generic events for variables of certain common types.
/// </summary>
public static class PlayerDataVariableEvents
{
    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Boolean}.OnGetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<bool>.PlayerDataVariableHandler? OnGetBool
    {
        add => PlayerDataVariableEvents<bool>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<bool>.OnGetVariable -= value;
    }
    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Boolean}.OnSetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<bool>.PlayerDataVariableHandler? OnSetBool
    {
        add => PlayerDataVariableEvents<bool>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<bool>.OnSetVariable -= value;
    }

    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Int32}.OnGetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<int>.PlayerDataVariableHandler? OnGetInt
    {
        add => PlayerDataVariableEvents<int>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<int>.OnGetVariable -= value;
    }
    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Int32}.OnSetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<int>.PlayerDataVariableHandler? OnSetInt
    {
        add => PlayerDataVariableEvents<int>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<int>.OnSetVariable -= value;
    }

    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{String}.OnGetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<string>.PlayerDataVariableHandler? OnGetString
    {
        add => PlayerDataVariableEvents<string>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<string>.OnGetVariable -= value;
    }
    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{String}.OnSetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<string>.PlayerDataVariableHandler? OnSetString
    {
        add => PlayerDataVariableEvents<string>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<string>.OnSetVariable -= value;
    }

    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Single}.OnGetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<float>.PlayerDataVariableHandler? OnGetFloat
    {
        add => PlayerDataVariableEvents<float>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<float>.OnGetVariable -= value;
    }
    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Single}.OnSetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<float>.PlayerDataVariableHandler? OnSetFloat
    {
        add => PlayerDataVariableEvents<float>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<float>.OnSetVariable -= value;
    }

    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Vector3}.OnGetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<Vector3>.PlayerDataVariableHandler? OnGetVector3
    {
        add => PlayerDataVariableEvents<Vector3>.OnGetVariable += value;
        remove => PlayerDataVariableEvents<Vector3>.OnGetVariable -= value;
    }
    /// <summary>
    /// Equivalent to <see cref="PlayerDataVariableEvents{Vector3}.OnSetVariable"/>.
    /// </summary>
    public static event PlayerDataVariableEvents<Vector3>.PlayerDataVariableHandler? OnSetVector3
    {
        add => PlayerDataVariableEvents<Vector3>.OnSetVariable += value;
        remove => PlayerDataVariableEvents<Vector3>.OnSetVariable -= value;
    }
}
