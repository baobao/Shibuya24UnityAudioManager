using System.Collections;
using System.Collections.Generic;
using info.shibuya24.Audio;
using NaughtyAttributes;
using UnityEngine;

public class Sample : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Shibuya24UnityAudioManager.InitinalizeIfNeed();
    }

    // Update is called once per frame
    void Update()
    {
    }

    [Button("PlayFX1")]
    void PlayFx1()
    {
        Shibuya24UnityAudioManager.Play("se/se_fx1");
    }

    [Button("PlayFX2")]
    void PlayFx2()
    {
        Shibuya24UnityAudioManager.Play("se/se_fx2");
    }

    [Button("PlayFX3")]
    void PlayFx3()
    {
        Shibuya24UnityAudioManager.Play("se/se_fx3");
    }

    [Button("PlayBGM2")]
    void PlayBGM1()
    {
        Shibuya24UnityAudioManager.Play("bgm/bgm_test1");
    }

    [Button("PlayBGM2")]
    void PlayBGM2()
    {
        Shibuya24UnityAudioManager.Play("bgm/bgm_test2");
    }

    [SerializeField] private AudioClip _clip1;
    [SerializeField] private AudioClip _clip2;
}