using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicTracks : MonoBehaviour
{
    [System.Serializable]
    public struct MusicClip
    {
        public AudioClip clip;
        public float volume;
    }

    public AudioSource audioSource;
    public AudioMixer mixer;
    public List<MusicClip> tracks;
    static MusicTracks instance;
    int nextIndex = 0;
    Camera mainCamera = null;

    public const string MASTER_CHANNEL = "MasterVolume";
    public const string MUSIC_CHANNEL = "MusicVolume";
    public const string SFX_CHANNEL = "SFXVolume";

    private void Awake()
    {
        if (!instance || instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
            NextTrack();
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        RestoreMixerValue(MASTER_CHANNEL);
        RestoreMixerValue(MUSIC_CHANNEL);
        RestoreMixerValue(SFX_CHANNEL);
    }

    void NextTrack()
    {
        audioSource.clip = tracks[nextIndex].clip;
        audioSource.volume = tracks[nextIndex].volume;
        audioSource.Play();
        Invoke(nameof(NextTrack), audioSource.clip.length);
        nextIndex++;
        nextIndex %= tracks.Count;
    }

    private void Update()
    {
        if (!mainCamera)
        {
            mainCamera = Camera.main;
        }
        transform.position = mainCamera.transform.position;
    }

    void RestoreMixerValue(string channel)
    {
        float value = PlayerPrefs.GetFloat(channel, 100);
        mixer.SetFloat(channel, ButtonHandler.ScaleVolumeSliderValue(value));
    }
}
