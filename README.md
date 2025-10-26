# Silksong Prepatcher

Prepatcher with general purpose assembly modifications for Hollow Knight: Silksong.

### Modifications

* Replace certain calls to GetTypes with calls to a safe method.
In particular, the new method will not throw when types that are not loadable
are present in an assembly, and will ignore MMHOOK_ assemblies.

* Route PlayerData accesses through Get/Set variable funcs.
This has been done so that the values the game thinks are the actual values
can be controlled by mods without affecting the underlying save data. This
is particularly useful for mods that users might play on a save file, and
then return to the save file without that mod installed.


### Config

Set WritePatchedAssemblies to true in the config file (in BepInEx/config) to write the
patched assemblies to the prepatcher folder in BepInEx/cache.
These assemblies will never be loaded by the game, but are written so that the state of
the assemblies that the game will see can be inspected (in ILSpy/equivalent).

# PrepatcherPlugin

This plugin exposes events to monitor and modify player data accesses, without affecting
the underlying save data.

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
