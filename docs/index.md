# About the Silksong Prepatcher

The Silkong Prepatcher makes several modifications to the game's code prior to startup to better support modding.

The project consists of two main parts: the Prepatcher, which actually does the modification,
and the PrepatcherPlugin, which provides an API surface for functionality associated with
the Prepatcher.

## When you should use

* If you have a compile-time dependency on a mod but that mod is not a hard dependency at runtime,
then you *might* need the Prepatcher installed to prevent runtime errors. It is hard to predict
whether this will happen, so it is best to test without the dependency installed.
* If you are referencing MMHOOK files, you should also declare a dependency on the Prepatcher.
This is because the MMHOOK_Assembly-CSharp file is quite large, and there are certain places where
the game looks over all loaded assemblies for types - an example (on the most recent patch) is Bone_04.
The Prepatcher causes this function to skip modded assemblies, which speeds up scene loading in these cases.
* If you are interacting with PlayerData, you may need the PrepatcherPlugin installed.
  - The Prepatcher replaces all get/set accesses to PlayerData fields with calls to the appropriate Get/Set
  functions, so mods that want to monitor player data accesses globally should depend on the Prepatcher
  to make this possible. For convenience, events are provided in the PrepatcherPlugin that monitor all
  Player Data accesses.
  - If you are modifying Player Data, you do not need to depend on the Prepatcher. To allow mods that monitor
  Player Data accesses to see the changes you make, you should use the existing Get/Set variable functions
  to do so.


## Usage

If you do not need to use the PrepatcherPlugin API, simply add a dependency to your thunderstore.toml:

```
silksong_modding-SilksongPrepatcher = "1.2.0"
```

The version number does not matter hugely, but the most up to date number can be retrieved from
[Thunderstore](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/SilksongPrepatcher/).

If manually uploading, instead copy the dependency string from the Thunderstore link.


If you do need the PrepatcherPlugin API, also add the following line to your .csproj:

```
<PackageReference Include="Silksong.PrepatcherPlugin" Version="1.2.0" />
```

The most up to date version number can be retrieved from [Nuget](https://www.nuget.org/packages/Silksong.PrepatcherPlugin/).

In either case, you should also add the Prepatcher as a BepInEx dependency by putting the following attribute
onto your plugin class, below the BepInAutoPlugin attribute.
```
[BepInDependency("org.silksong-modding.prepatcher")]
```
