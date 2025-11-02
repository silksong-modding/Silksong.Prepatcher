namespace PrepatcherPlugin;

/// <summary>
/// Class containing members that correctly delegate to fields on <see cref="PlayerData.instance" /> via the
/// appropriate Get/Set variable methods.
/// 
/// For example, PlayerDataAccess.visitedMossCave is equivalent to PlayerData.instance.visitedMossCave, except
/// passing via the Get/Set bool events.
/// </summary>
public static partial class PlayerDataAccess;
