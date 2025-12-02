# Silksong Prepatcher

Prepatcher with general purpose assembly modifications for Hollow Knight: Silksong.

# PrepatcherPlugin

This plugin exposes events to monitor player data accesses, and control the value
received by the game without affecting the underlying save data.

To use, subscribe to either the `PlayerDataVariableEvents<T>.OnGetVariable` or
`PlayerDataVariableEvents<T>.OnSetVariable` events, where `T` is the field type.

The subscriber takes as arguments the player data object, the field name, and the
unmodified value (or the previous value if there are multiple subscribers), and 
should return the modified value; in the GetVariable case the unmodified value
is the value of the field and the returned value is what the game code sees, and
in the SetVariable case the unmodified value is what the game code is trying to
set and the returned value is the value that is actually written.

For convenience, some common events have been added to the non-generic
PlayerDataVariableEvents class. These behave the same as the generic equivalent,
so `PlayerDataVariableEvents.OnGetBool` is the same as 
`PlayerDataVariableEvents<bool>.OnGetVariable`, for example.

Example usage:

```cs
using GlobalEnums;
using PrepatcherPlugin;

// In your mod's Awake class

PlayerDataVariableEvents.OnSetBool += MonitorBoolSets;
PlayerDataVariableEvents<CaravanTroupeLocations>.OnGetVariable += OnGetCaravanLocation;

// Defining the functions

private bool MonitorBoolSets(PlayerData pd, string fieldName, bool current)
{
	Logger.LogInfo($"Setting bool {fieldName} to {current}");
	
	// Make sure to return current if you don't want to modify the value that would be set.
	return current;
}

private CaravanTroupeLocations OnGetCaravanLocation(PlayerData pd, string fieldName, CaravanTroupeLocations current)
{
    // If the caravan is in the Marrow, make the game think it's in Greymoor. Otherwise no change.
	return current == CaravanTroupeLocations.Bone ? CaravanTroupeLocations.Greymoor : current;
}
```

For convenience, the PlayerDataAccess class exposes properties to get and set values from the
PlayerData.instance object while going via the above events (using the Get/Set functions defined in the unmodded code)
to maintain compatibility with mods that monitor save data accesses. For example, the following are equivalent:

```cs
bool b = PlayerData.instance.GetBool(nameof(PlayerData.hasBrolly));
PlayerData.instance.SetBool(nameof(PlayerData.hasWalljump), true);
```
and
```cs
bool b = PlayerDataAccess.hasBrolly;
PlayerDataAccess.hasWalljump = true;
```


# Prepatcher

The following patches are made by the Prepatcher:

* Route PlayerData field accesses through Get/Set variable funcs.
This has been done so that mods can more easily monitor when the game gets/sets
field values in the PlayerData, and control the output/effect of those accesses
through the events defined in the PrepatcherPlugin.
In particular, this allows for compatibility between mods that interact with
PlayerData in different ways.

* Replace certain calls to Assembly.GetTypes with calls to a safe method.
In particular, the new method will not throw when types that are not loadable
are present in an assembly, and will ignore MMHOOK_ assemblies (which may contain
many types, none of which are of interest to functions defined in base game
assemblies).

* Replace certain calls to Type.IsAssignableFrom with calls to a safe method.
For some reason, in certain circumstances types will be loadable by
GetTypes and non-null, but will throw an error when the argument of
Type.IsAssignableFrom. This is only an issue in some cases when mods are missing
soft dependencies.


### Inspecting patched assemblies

To inspect patched assemblies, set the DumpAssemblies config variable
in BepInEx.cfg to true.
