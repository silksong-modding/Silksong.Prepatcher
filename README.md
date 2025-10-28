# Silksong Prepatcher

Prepatcher with general purpose assembly modifications for Hollow Knight: Silksong.

### Modifications

* Replace certain calls to GetTypes with calls to a safe method.
In particular, the new method will not throw when types that are not loadable
are present in an assembly, and will ignore MMHOOK_ assemblies (which may contain
many types, none of which are of interest to functions defined in base game
assemblies).

* Route PlayerData accesses through Get/Set variable funcs.
This has been done so that mods can more easily monitor when the game gets/sets
field values in the PlayerData, and control the output/effect of those accesses
through the events defined in the PrepatcherPlugin.
In particular, this allows for compatibility between mods that interact with
PlayerData in different ways.

### Inspecting patched assemblies

To inspect patched assemblies, set the DumpAssemblies config variable
in BepInEx.cfg to true.

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
