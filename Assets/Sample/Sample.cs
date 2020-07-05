using System.Collections;
using System.Collections.Generic;
using info.shibuya24.Audio;
using NaughtyAttributes;
using UnityEngine;

public class Sample : MonoBehaviour
{
    [OnValueChanged("OnValueChangeBgmVolume")] [Range(0, 1f)]public float bgmVolume;
    [OnValueChanged("OnValueChangeSeVolume")] [Range(0, 1f)]public float seVolume;
    private bool _isSeMute;
    private bool _isBgmMute;

    void Start()
    {
        Shibuya24UnityAudioManager.InitializeIfNeed();

        // Apply LocalSave
#if ENABLE_LOCALSAVE_SHIBUYA24_AUDIO
        bgmVolume = AudioLocalSave.GetVolume(AudioChannel.BGM);
        seVolume = AudioLocalSave.GetVolume(AudioChannel.SE);
        _isBgmMute = AudioLocalSave.GetMute(AudioChannel.BGM);
        _isSeMute = AudioLocalSave.GetMute(AudioChannel.SE);
#endif
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

    [Button("PlayBGM1")]
    void PlayBGM1()
    {
        Shibuya24UnityAudioManager.Play("bgm/bgm_test1");
    }

    [Button("PlayBGM2")]
    void PlayBGM2()
    {
        Shibuya24UnityAudioManager.Play("bgm/bgm_test2");
    }

    [Button("StopBGM")]
    void StopBgm()
    {
        Shibuya24UnityAudioManager.StopBgm();
    }

    [Button("Mute SE")]
    void ToggleSEMute()
    {
        Shibuya24UnityAudioManager.SetMute(AudioChannel.SE, _isSeMute);
        _isSeMute = _isSeMute == false;
    }


    [Button("Mute BGM")]
    void ToggleBGMMute()
    {
        Shibuya24UnityAudioManager.SetMute(AudioChannel.BGM, _isBgmMute);
        _isBgmMute = _isBgmMute == false;
    }

    private void OnValueChangeBgmVolume()
    {
        Debug.Log($"OnValueChangeBgmVolume : {bgmVolume}");
        Shibuya24UnityAudioManager.SetVolume(AudioChannel.BGM, bgmVolume);
    }

    private void OnValueChangeSeVolume()
    {
        Shibuya24UnityAudioManager.SetVolume(AudioChannel.SE, seVolume);
    }
}