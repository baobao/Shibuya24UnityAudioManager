using Cysharp.Threading.Tasks;
using UnityEngine;

namespace info.shibuya24.Audio
{
    /// <summary>
    /// Resource.Load Loader
    /// </summary>
    public class ResourcesLoader : IAudioResourceLoader
    {
        public async UniTask<AudioClip> Load(string path)
        {
            var req = Resources.LoadAsync<AudioClip>(path);
            await req;
            return req.asset as AudioClip;
        }
    }
}