using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Shibuya24.Common;
using UnityEngine;

namespace info.shibuya24.Audio
{
    /// <summary>
    /// オーディオチャンネル
    /// TODO Voice
    /// </summary>
    public enum AudioChannel
    {
        None = 0,
        SE = 1,
        BGM = 2,
    }

    /// <summary>
    /// UnityのAudioシステムを使用したマネージャ
    /// </summary>
    public sealed class Shibuya24UnityAudioManager : MonoBehaviourSingleton<Shibuya24UnityAudioManager>
    {
        /// <summary>
        /// BGMの同時再生数は2固定
        /// </summary>
        public const int BgmPlayCount = 2;

        public static readonly string BgmPrefix = "bgm_";
        public static readonly string SePrefix = "se_";

        /// <summary>
        /// 各チャンネルのボリュームMap
        /// </summary>
        public static readonly Dictionary<AudioChannel, float> ChannelVolumeMap = new Dictionary<AudioChannel, float>
        {
            {AudioChannel.SE, 1f},
            {AudioChannel.BGM, 1f}
        };

        private static int _playingUniqueId = 0;
        private static int NextPlayingUniqueId => _playingUniqueId++;

        public static string CurrentBgmPath { get; private set; }

#if ENABLE_DEBUG_SHIBUYA24_AUDIO
        /// <summary>
        /// 再生中BGMチャンネルマップ
        /// </summary>
        public static readonly Dictionary<AudioChannel, List<AudioInfo>> PlayingChannelMap =
            new Dictionary<AudioChannel, List<AudioInfo>>
            {
                {AudioChannel.SE, new List<AudioInfo>()},
                {AudioChannel.BGM, new List<AudioInfo>()}
            };
#else
        /// <summary>
        /// 再生中BGMチャンネルマップ
        /// </summary>
        private static readonly Dictionary<AudioChannel, List<AudioInfo>> PlayingChannelMap =
            new Dictionary<AudioChannel, List<AudioInfo>>
            {
                {AudioChannel.SE, new List<AudioInfo>()},
                {AudioChannel.BGM, new List<AudioInfo>()}
            };

#endif

        public static IAudioResourceLoader Loader { get; set; }

        private static bool _isInitialized;

        protected override void OnAwake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            // staticの初期化
            _isInitialized = false;
            PlayingChannelMap.Clear();
            _playingUniqueId = 0;
            Loader = null;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public static void InitializeIfNeed(AudioSetting setting = null, IAudioResourceLoader loader = null)
        {
            if (_isInitialized)
            {
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
                Debug.Log("Already Initialized");
#endif
                return;
            }

            if (setting == null)
            {
                setting = new AudioSetting();
            }

            InitializeChannel(setting);
            if (loader == null)
            {
                loader = new ResourcesLoader();
            }

            Loader = loader;

#if ENABLE_LOCALSAVE_SHIBUYA24_AUDIO
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
                Debug.Log("Enable AudioLocalSave");
#endif
            // Apply Initialize LocalSetting
            SetVolume(AudioChannel.BGM, AudioLocalSave.GetVolume(AudioChannel.BGM));
            SetVolume(AudioChannel.SE, AudioLocalSave.GetVolume(AudioChannel.SE));
            SetMute(AudioChannel.BGM, AudioLocalSave.GetMute(AudioChannel.BGM));
            SetMute(AudioChannel.SE, AudioLocalSave.GetMute(AudioChannel.SE));
#else
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
            Debug.Log("Disable AudioLocalSave");
#endif
#endif
            _isInitialized = true;
        }


        /// <summary>
        /// サウンド再生
        /// </summary>
        public static async UniTask<int> Play(string audioPath)
        {
            if (CurrentBgmPath == audioPath)
            {
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
                Debug.Log($"同じBGMが再生中 : {audioPath}");
#endif
                return -1;
            }

            var channel = ResolveChannel(audioPath);
            if (channel == AudioChannel.None)
            {
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
                Debug.Log($"再生できませんでした : {audioPath}");
#endif
                return -1;
            }

            if (channel == AudioChannel.BGM)
            {
                // BGMはクロスフェードする
                return await PlayBgm(audioPath);
            }

            // load AudioClip from Loader
            return await LoadAndPlayAudioClip(channel, audioPath);
        }


        public static void Stop(AudioChannel ch, int playId)
        {
            var infoList = GetPlayingAudioInfoList(ch);
            var info = infoList.Find(x => x.PlayingId == playId);
            info?.Stop();
        }

        /// <summary>
        /// 現在再生中のBGMをストップする
        /// </summary>
        public static async UniTask StopBgm(float duration = 1f)
        {
            CurrentBgmPath = null;
            var infoList = PlayingChannelMap[AudioChannel.BGM];
            await UniTask.WhenAll(infoList.Select(x => x.FadeOut(duration)));
        }

        /// <summary>
        /// ボリュームのセット
        /// </summary>
        /// <param name="ch">チャンネル</param>
        /// <param name="volume">0~1f</param>
        public static void SetVolume(AudioChannel ch, float volume)
        {
            if (ch == AudioChannel.None) return;
#if ENABLE_LOCALSAVE_SHIBUYA24_AUDIO
            AudioLocalSave.SetVolume(ch, volume);
#endif
            // Clamp
            volume = Mathf.Clamp01(volume);
            var list = PlayingChannelMap[ch];
            for (int i = 0; i < list.Count; i++)
            {
                list[i].SetGlobalVolume(volume);
            }
        }

        /// <summary>
        /// ミュート設定のセット
        /// </summary>
        public static void SetMute(AudioChannel ch, bool isMute)
        {
            if (ch == AudioChannel.None) return;
#if ENABLE_LOCALSAVE_SHIBUYA24_AUDIO
            AudioLocalSave.SetMute(ch, isMute);
#endif
            var list = PlayingChannelMap[ch];
            for (int i = 0; i < list.Count; i++)
            {
                list[i].SetMute(isMute);
            }
        }

        /// <summary>
        /// チャンネルの初期化
        /// </summary>
        private static void InitializeChannel(AudioSetting setting)
        {
            if (_isInitialized) return;
            PlayingChannelMap[AudioChannel.SE].Clear();
            PlayingChannelMap[AudioChannel.BGM].Clear();

            var initBgmVolume = ChannelVolumeMap[AudioChannel.SE];
            var initSeVolume = ChannelVolumeMap[AudioChannel.BGM];

            var go = Instance.gameObject;
            for (var i = 0; i < setting.seChannelCount; i++)
            {
                PlayingChannelMap[AudioChannel.SE].Add(
                    new AudioInfo(go.AddComponent<AudioSource>())
                        .SetGlobalVolume(initSeVolume)
                );
            }

            for (var i = 0; i < BgmPlayCount; i++)
            {
                PlayingChannelMap[AudioChannel.BGM].Add(
                    new AudioInfo(go.AddComponent<AudioSource>())
                        .SetGlobalVolume(initBgmVolume)
                );
            }
        }


        /// <summary>
        /// 指定パスがBGMとして再生されているか返却
        /// </summary>
        private static int GetPlayingBgm(string audioPath)
        {
            if (string.IsNullOrEmpty(audioPath)) return -1;
            var infoList = PlayingChannelMap[AudioChannel.BGM];
            var info = infoList.Find(x => x.IsUsing && x.ResourcePath == audioPath && x.IsStopping == false);
            if (info != null)
            {
                return info.PlayingId;
            }

            return -1;
        }

        /// <summary>
        /// 再生中のBGMを返却する
        /// </summary>
        private static int GetPlayingBgm()
        {
            var infoList = PlayingChannelMap[AudioChannel.BGM];
            var info = infoList.Find(x => x.IsUsing);
            if (info != null)
            {
                return info.PlayingId;
            }

            return -1;
        }

        /// <summary>
        /// BGM再生 / クロスフェード
        /// </summary>
        private static async UniTask<int> PlayBgm(string bgmPath)
        {
            // 現在再生中のBGMをフェードアウトさせる
            var playingId = GetPlayingBgm(CurrentBgmPath);

            CurrentBgmPath = bgmPath;
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
            var msg = playingId < 0 ? "Not Playing" : $"playingId : {playingId.ToString()}";
            Debug.Log($"current Bgm => {msg}");
#endif
            if (playingId >= 0)
            {
                FadeOut(AudioChannel.BGM, playingId).Forget();
                playingId = await FadeIn(AudioChannel.BGM, CurrentBgmPath);
                return playingId;
            }

            // 無音状態からの再生
            playingId = await LoadAndPlayAudioClip(AudioChannel.BGM, bgmPath);
            return playingId;
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        private static async UniTask FadeOut(AudioChannel ch, int playingId)
        {
            var info = GetPlayingAudioInfo(ch, playingId);
            if (info == null)
            {
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
                Debug.Log($"Can't FadeOut. {ch} / {playingId}");
#endif
                return;
            }

            await info.FadeOut();
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        private static async UniTask<int> FadeIn(AudioChannel ch, string path)
        {
            var playingId = await LoadAndPlayAudioClip(ch, path, true);
            var info = GetPlayingAudioInfo(ch, playingId);
            if (info == null)
            {
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
                Debug.Log($"Can't FadeIn. {ch} / {path}");
#endif
                return -1;
            }

            await info.FadeIn();
            return playingId;
        }

        private static AudioInfo GetPlayingAudioInfo(AudioChannel ch, int playingId)
        {
            var infoList = PlayingChannelMap[ch];
            return infoList.Find(x => x.PlayingId == playingId);
        }

        /// <summary>
        /// 指定したチャンネル、パスを指定して再生中のAudioInfoを返却する
        /// </summary>
        private static AudioInfo GetPlayingAudioInfoWithPath(AudioChannel ch, string path)
        {
            var infoList = PlayingChannelMap[ch];
            return infoList.Find(x => x.ResourcePath == path);
        }

        /// <summary>
        /// ロードしてAudioClipを再生させる
        /// </summary>
        private static async UniTask<int> LoadAndPlayAudioClip(AudioChannel ch, string audioPath,
            bool isStartVolumeZero = false)
        {
            var clip = await Loader.Load(audioPath);
            if (clip == null)
            {
                // Load Fail
                return -1;
            }

            var audioInfoList = GetPlayingAudioInfoList(ch);
            var audioInfo = GetUnUseOrOldestAudioInfo(audioInfoList);

            var nextId = NextPlayingUniqueId;

            audioInfo.SetAudioClip(clip)
                .SetPlayingId(nextId)
                .SetResourcePath(audioPath)
                .Play(isStartVolumeZero);

            return nextId;
        }

        /// <summary>
        /// 未使用または古いAudioInfoを返却
        /// </summary>
        private static AudioInfo GetUnUseOrOldestAudioInfo(List<AudioInfo> audioInfoList)
        {
            AudioInfo info = null;
            for (int i = 0; i < audioInfoList.Count; i++)
            {
                info = audioInfoList[i];
                if (info.IsUsing == false)
                {
                    info.Initialize();
                    return info;
                }
            }

            info = audioInfoList.Find(x => x.IsStopping);
            if (info == null)
            {
                info = audioInfoList[0];
            }

            // 使用中のものを予めストップして初期化しておく
            info.Initialize();
            return info;
        }

        private static List<AudioInfo> GetPlayingAudioInfoList(AudioChannel ch)
        {
            return PlayingChannelMap[ch];
        }

        /// <summary>
        /// ファイルパスからAudioChannelを返却
        /// </summary>
        private static AudioChannel ResolveChannel(string audioPath)
        {
            var fileName = Path.GetFileName(audioPath);
            if (fileName != null && fileName.IndexOf(SePrefix, StringComparison.Ordinal) == 0)
            {
                return AudioChannel.SE;
            }

            if (fileName != null && fileName.IndexOf(BgmPrefix, StringComparison.Ordinal) == 0)
            {
                return AudioChannel.BGM;
            }
#if ENABLE_DEBUG_SHIBUYA24_AUDIO
            Debug.LogError($"Invalid audioPath : {audioPath}");
#endif
            return AudioChannel.None;
        }
    }
}