using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PlayVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string fileName;

    void OnEnable()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        videoPlayer.Play();
    }
}
