using BepInEx.Logging;
using System;

namespace PrepatcherPlugin
{
    public static class PlayerDataHooks
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(PlayerDataHooks));

        /// <summary>
        /// Delegate for events that access PlayerData bool values.
        /// 
        /// To avoid changing anything, return the value of <paramref name="current"/>.
        /// </summary>
        /// <param name="pd">The PlayerData instance.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="current">The value that is going to be set or got.</param>
        /// <returns>The updated value.</returns>
        public delegate bool PlayerDataBoolProxy(PlayerData pd, string fieldName, bool current);

        /// <summary>
        /// Event used to control the output of PlayerData.GetBool.
        /// 
        /// See <see cref="PlayerDataBoolProxy">.
        /// </summary>
        public static event PlayerDataBoolProxy? OnGetBool;

        /// <summary>
        /// Event used to control the value set by PlayerData.SetBool.
        /// 
        /// See <see cref="PlayerDataBoolProxy">.
        /// </summary>
        public static event PlayerDataBoolProxy? OnSetBool;


        internal static bool PlayerDataGetBool(PlayerData playerData, string fieldName, bool current)
        {
            if (OnGetBool == null) return current;

            foreach (PlayerDataBoolProxy toInvoke in OnGetBool.GetInvocationList())
            {
                try
                {
                    current = toInvoke?.Invoke(playerData, fieldName, current) ?? current;
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to execute {nameof(OnGetBool)}:\n" + ex);
                }
            }
            return current;
        }

    }
}
