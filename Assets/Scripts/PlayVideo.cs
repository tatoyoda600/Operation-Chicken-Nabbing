using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        videoPlayer.time = 0;
        videoPlayer.Play();

        videoPlayer.loopPointReached += (_) => {
            gameObject.GetComponent<Button>().onClick.Invoke();
        };
    }
}
