using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace info.shibuya24.Audio
{
    /// <summary>
    /// オーディオの状態クラス
    /// </summary>
    public sealed class AudioInfo
    {
        /// <summary>
        /// 使用中ソースフラグ
        /// </summary>
        public bool IsUsing => _source != null
                               && _source.clip != null
                               && _source.isPlaying;

        /// <summary>
        /// オーディオ再生キー
        ///
        /// このキーを元にShibuya24UnityAudioManager
        /// </summary>
        public int PlayingId { get; private set; } = -1;

        /// <summary>
        /// オーディオリソースパス
        /// </summary>
        public string ResourcePath { get; private set; }

        /// <summary>
        /// AudioSource
        /// </summary>
        private readonly AudioSource _source;

        /// <summary>
        /// チャンネルごとのグローバルボリューム
        /// </summary>
        private float _globalVolume;

        /// <summary>
        /// このAudioSourceのボリューム
        /// </summary>
        private float Volume
        {
            get => _source.volume;
            set => _source.volume = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AudioInfo(AudioSource source)
        {
            _source = source;
            Initialize();
        }

        /// <summary>
        /// 再利用する際に実行して初期化する
        /// </summary>
        public void Initialize()
        {
            Stop();
            PlayingId = -1;
            ResourcePath = null;
        }

        public AudioInfo SetAudioClip(AudioClip clip)
        {
            _source.clip = clip;
            return this;
        }

        public AudioInfo SetResourcePath(string path)
        {
            ResourcePath = path;
            return this;
        }

        public AudioInfo SetPlayingId(int id)
        {
            PlayingId = id;
            return this;
        }

        /// <summary>
        /// ストップ
        /// フェードアウトして止める場合はFadeOut
        /// </summary>
        public void Stop()
        {
            IsStopping = false;
            _source.Stop();
        }

        public bool IsStopping { get; private set; }

        /// <summary>
        /// フェードアウトして終了させる
        /// </summary>
        public async UniTask FadeOut(float duration = 1f)
        {
            if (IsUsing == false) return;
            IsStopping = true;
            DOTween.Kill(this);
            await DOTween.To(() => Volume, x => Volume = x, 0, duration).SetEase(Ease.Linear)
                .SetTarget(this);

            // フェードアウト中に再生状態になったら止めない
            if (IsStopping)
            {
                Stop();
            }
        }

        /// <summary>
        /// フェードインする
        /// </summary>
        public async UniTask FadeIn(float duration = 1f)
        {
            DOTween.Kill(this);
            await DOTween.To(() => _source.volume, x => _source.volume = x, _globalVolume, duration)
                .SetEase(Ease.Linear)
                .SetTarget(this);

            // フェードイン後に最新のボリュームをセットする
            _source.volume = _globalVolume;
        }

        /// <summary>
        /// 再生
        /// </summary>
        public AudioInfo Play(bool isStartVolumeZero = false)
        {
            DOTween.Kill(this);
            Volume = isStartVolumeZero ? 0 : _globalVolume;

            // 停止中フラグをfalseにする
            IsStopping = false;

            _source.Play();
            return this;
        }

        /// <summary>
        /// グローバルボリュームのセット
        /// </summary>
        public AudioInfo SetGlobalVolume(float volume)
        {
            Volume = _globalVolume = volume;
            return this;
        }

        /// <summary>
        /// ミュート設定
        /// </summary>
        public void SetMute(bool isMute)
        {
            _source.mute = isMute;
        }

        public override string ToString()
        {
            return
                $"IsUsing:{IsUsing} | ID : {PlayingId} | {ResourcePath} | IsStopping : {IsStopping} | Vol : {Volume}";
        }
    }
}