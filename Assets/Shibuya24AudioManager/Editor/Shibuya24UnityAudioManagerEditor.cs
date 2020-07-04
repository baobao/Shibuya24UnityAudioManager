#if ENABLE_DEBUG_SHIBUYA24_AUDIO
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace info.shibuya24.Audio
{
    public class Shibuya24UnityAudioEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Shibuya24/Audio")]
        static void Init()
        {
            GetWindow<Shibuya24UnityAudioEditorWindow>().Show();
        }

        void OnGUI()
        {
            if (Shibuya24UnityAudioManager.HasInstance == false)
            {
                return;
            }

            // BGM

            EditorGUILayout.LabelField($"CurrentBGM : {Shibuya24UnityAudioManager.CurrentBgmPath}");

            var bgmInfoList = Shibuya24UnityAudioManager.PlayingChannelMap[AudioChannel.BGM];
            foreach (var bgmInfo in bgmInfoList)
            {
                EditorGUILayout.LabelField(bgmInfo.ToString());
            }

            Repaint();
        }
    }
}
#endif