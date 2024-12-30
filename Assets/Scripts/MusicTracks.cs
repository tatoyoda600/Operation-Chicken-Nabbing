using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTracks : MonoBehaviour
{
    [System.Serializable]
    public struct MusicClip
    {
        public AudioClip clip;
        public float volume;
    }

    public AudioSource audioSource;
    public List<MusicClip> tracks;
    int nextIndex = 0;
    Camera mainCamera = null;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        NextTrack();
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
}
