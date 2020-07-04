using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class Sample : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Button("PlaySFX")]
    void PlaySfx()
    {
    }

    [Button("PlayBGM")]
    void PlayBGM()
    {
    }

    [SerializeField] private AudioClip _clip1;
    [SerializeField] private AudioClip _clip2;
}
