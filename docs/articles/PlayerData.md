# Player Data access

It is common for mods to want to monitor and control Player Data accesses. The Prepatcher Plugin provides
several convenient tools for doing this.

## Setting variables

Even if you are not using the prepatcher, it is generally considered polite to use the get/set variable methods
to set PlayerData variables, so other mods can monitor those accesses.

For example:

```
// Bad
PlayerData.instance.hasWalljump = true;

// Good
PlayerData.instance.SetBool(nameof(PlayerData.hasWalljump), true);
```

As an alternative, the PlayerDataAccess class provides properties that delegate the get/set accessors to
the appropriate get/set variable methods:

```
// Good
PlayerDataAccess.hasWalljump = true;  // This calls the SetBool method behind the scenes.
```

The same applies to fields of other types; if the type is not bool, int, string, float or Vector2,
then the GetVariable<T> extension method (defined in TeamCherry.SharedUtils) should be used instead.

## Monitoring access

To monitor changes to the player data, the prepatcher plugin provides events that make this easy:

```
// To listen to all changes to bool variables
// This line should go in your plugin's Awake method
PlayerDataVariableEvents.OnSetBool += MonitorBoolSets;

private bool MonitorBoolSets(PlayerData pd, string fieldName, bool current)
{
    Logger.LogInfo($"Setting bool {fieldName} to {current}");

    // Make sure to return current if you don't want to modify the value that would be set.
    // If you return something else here, that is what the field will be set to instead.
    return current;
}
```

As an alternative, you can hook/prefix the appropriate PlayerData get/set methods - in this case, you may
need to take care to distinguish between `PlayerData.instance.GetBool` and
`PlayerData.instance.GetVariable<bool>` - both of these are used in the game's code.
(Subscribing to the events provided by the Prepatcher Plugin automatically listens to both of these functions.)

## Controlling access

Commonly you may want to make the game think that a certain PlayerData variable is true or false without affecting
the save. For example, you need hasWalljump to be false for your mod to function. If you set PlayerData.hasWalljump
to false, then that may break players' casual saves. Instead, you can subscribe to the event:

```
// In your plugin's Awake method
PlayerDataVariableEvents.OnGetBool += ForceNoWallJump;

private bool ForceNoWallJump(PlayerData pd, string fieldName, bool current)
{
    // Return what you want the game to think the bool to be
    if (fieldName != nameof(PlayerData.hasWalljump))
    {
        // Return current if you don't want to modify the value of the field
        return current;
    }
    
    // All game code that checks for hasWalljump will now see false
    return false;
}

```

This technique is often used with custom player data fields. For example, some components are controlled
by a player data field. If you are adding that component to a custom game object, you may
wish to have more control over its behaviour; the easiest way to do this is to set the string defining
the player data variable to a custom string, and then listen to the appropriate 
`PlayerDataVariableEvents<T>.OnGetVariable` event to control what the game sees.
