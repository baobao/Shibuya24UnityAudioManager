#if ENABLE_LOCALSAVE_SHIBUYA24_AUDIO
using System;
using UnityEngine;

namespace info.shibuya24.Audio
{
    /// <summary>
    /// Shibuya24AudioManager Local Save
    /// Use PlayerPrefs
    /// </summary>
    public static class AudioLocalSave
    {
        private const string BgmVolumeKey = "shibuya24.bgm.volume";
        private const string SeVolumeKey = "shibuya24.se.volume";

        private const string BgmMuteKey = "shibuya24.bgm.mute";
        private const string SeMuteKey = "shibuya24.se.mute";

        /// <summary>
        /// Save Volume
        /// </summary>
        public static void SetVolume(AudioChannel ch, float value)
        {
            switch (ch)
            {
                case AudioChannel.BGM:
                    SetFloat(BgmVolumeKey, value);
                    break;
                case AudioChannel.SE:
                    SetFloat(SeVolumeKey, value);
                    break;
            }
        }

        /// <summary>
        /// Get Volume.
        /// </summary>
        public static float GetVolume(AudioChannel ch, float defaultValue = 1f)
        {
            switch (ch)
            {
                case AudioChannel.BGM:
                    return GetFloat(BgmVolumeKey, defaultValue);
                case AudioChannel.SE:
                    return GetFloat(SeVolumeKey, defaultValue);
            }

            Debug.LogError($"Invalid key : {ch}");
            return 0;
        }

        /// <summary>
        /// Set Mute State.
        /// </summary>
        public static void SetMute(AudioChannel ch, bool value)
        {
            switch (ch)
            {
                case AudioChannel.BGM:
                    SetBool(BgmMuteKey, value);
                    break;
                case AudioChannel.SE:
                    SetBool(SeMuteKey, value);
                    break;
            }
        }


        /// <summary>
        /// Get Mute State.
        /// </summary>
        public static bool GetMute(AudioChannel ch, bool defaultValue = false)
        {
            switch (ch)
            {
                case AudioChannel.BGM:
                    return GetBool(BgmMuteKey, defaultValue);
                case AudioChannel.SE:
                    return GetBool(SeMuteKey, defaultValue);
            }

            Debug.LogError($"Invalid key : {ch}");
            return false;
        }

        #region PlayerPrefs Wrapper

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        private static float GetFloat(string key, float defaultValue = 0)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        private static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        private static int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        private static void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        private static bool GetBool(string key, bool defaultValue = false)
        {
            return GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        #endregion
    }
}
#endif