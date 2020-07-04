using Cysharp.Threading.Tasks;
using UnityEngine;

namespace info.shibuya24.Audio
{
    public interface IAudioResourceLoader
    {
        /// <summary>
        /// ロード
        /// </summary>
        UniTask<AudioClip> Load(string key);
    }
}