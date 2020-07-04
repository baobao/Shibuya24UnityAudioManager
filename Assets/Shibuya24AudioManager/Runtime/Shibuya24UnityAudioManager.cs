using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Shibuya24.Common;
using UnityEngine;

namespace info.shibuya24.Audio
{
    public enum AudioChannel
    {
        None = 0,
        SE = 1,
        BGM = 2,
    }

    public class AudioInfo
    {
        /// <summary>
        /// 使用中ソースフラグ
        /// </summary>
        public bool IsUsing => source != null && source.clip != null && source.isPlaying;

        public AudioSource source;
        public long playingId;
        public string path;

        public void Initialize()
        {
            source.Stop();
            playingId = -1;
            path = null;
        }

        public void Stop()
        {
            source.Stop();
        }

        /// <summary>
        ///
        /// </summary>
        public async UniTask FadeOut(float duration = 0.5f)
        {

        }

        public void Update()
        {

        }
    }

    public class AudioSetting
    {
        public int seChannelCount = 16;
    }

    /// <summary>
    /// UnityのAudioシステムを使用したマネージャ
    /// </summary>
    public class Shibuya24UnityAudioManager : MonoBehaviourSingleton<Shibuya24UnityAudioManager>
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

        /// <summary>
        /// 再生中BGMチャンネルマップ
        /// </summary>
        private static readonly Dictionary<AudioChannel, List<AudioInfo>> PlayingChannelMap =
            new Dictionary<AudioChannel, List<AudioInfo>>
            {
                {AudioChannel.SE, new List<AudioInfo>()},
                {AudioChannel.BGM, new List<AudioInfo>()}
            };

        public static IAudioResourceLoader Loader { get; set; }

        private static bool _isInitialized;

        /// <summary>
        /// 現在再生中BGMパス
        /// </summary>
        public static string CurrentBgmKey { get; private set; }

        private void OnDestroy()
        {
            // staticの初期化
            PlayingChannelMap.Clear();
            _playingUniqueId = 0;
            CurrentBgmKey = null;
            Loader = null;
        }

        void Update()
        {
            foreach (var channelInfoList in PlayingChannelMap.Values)
            {
                for (int i = 0; i < channelInfoList.Count; i++)
                {
                    var info = channelInfoList[i];
                    info.Update();
                }
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public static void InitinalizeIfNeed(AudioSetting setting = null, IAudioResourceLoader loader = null)
        {
            if (_isInitialized) return;

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

            _isInitialized = true;
        }

        /// <summary>
        /// チャンネルの初期化
        /// </summary>
        private static void InitializeChannel(AudioSetting setting)
        {
            if (_isInitialized) return;
            PlayingChannelMap[AudioChannel.SE].Clear();
            PlayingChannelMap[AudioChannel.BGM].Clear();
            var go = Instance.gameObject;
            for (int i = 0; i < setting.seChannelCount; i++)
            {
                PlayingChannelMap[AudioChannel.SE].Add(new AudioInfo()
                {
                    source = go.AddComponent<AudioSource>()
                });
            }

            for (int i = 0; i < BgmPlayCount; i++)
            {
                PlayingChannelMap[AudioChannel.BGM].Add(new AudioInfo()
                {
                    source = go.AddComponent<AudioSource>()
                });
            }
        }

        /// <summary>
        /// サウンド再生
        /// </summary>
        public static async UniTask<int> Play(string audioPath)
        {
            if (CurrentBgmKey == audioPath)
            {
                // 同じBGMが再生していたら何もしない
                return -1;
            }

            var channel = ResolveChannel(audioPath);
            if (channel == AudioChannel.None)
            {
                Debug.Log($"再生できませんでした : {audioPath}");
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
            var info = infoList.Find(x => x.playingId == playId);
            info?.Stop();
        }

        /// <summary>
        /// BGM再生 / クロスフェード
        /// </summary>
        private static async UniTask<int> PlayBgm(string bgmPath)
        {
            // 現在再生中のBGMをフェードアウトさせる
            if (string.IsNullOrEmpty(CurrentBgmKey) == false)
            {
                FadeOut(AudioChannel.BGM, CurrentBgmKey).Forget();
                return await FadeIn(bgmPath);
            }

            // 無音状態からの再生
            return await LoadAndPlayAudioClip(AudioChannel.BGM, bgmPath);
        }

        private static async UniTask FadeOut(AudioChannel ch, string path)
        {
            var info = GetAudioInfoWithPath(ch, path);
            if (info == null)
            {
                return;
            }

            info.FadeOut();
        }

        private static async UniTask<int> FadeIn(string bgmPath)
        {
            return -1;
        }

        /// <summary>
        /// 指定したチャンネル、パスを指定してAudioInfoを返却する
        /// </summary>
        private static AudioInfo GetAudioInfoWithPath(AudioChannel ch, string path)
        {
            var infoList = PlayingChannelMap[ch];
            return infoList.Find(x => x.path == path);
        }

        /// <summary>
        /// ロードしてAudioClipを再生させる
        /// </summary>
        private static async UniTask<int> LoadAndPlayAudioClip(AudioChannel ch, string audioPath)
        {
            var clip = await Loader.Load(audioPath);
            var audioInfoList = GetPlayingAudioInfoList(ch);
            var audioInfo = GetUnUseOrOldestAudioInfo(audioInfoList);

            var nextId = NextPlayingUniqueId;

            audioInfo.source.clip = clip;
            audioInfo.path = audioPath;
            audioInfo.playingId = nextId;
            audioInfo.source.Play();

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

            // TODO 全部使用中だった場合
            info = audioInfoList[0];
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

            Debug.LogError($"Invalid audioPath : {audioPath}");
            return AudioChannel.None;
        }
    }
}