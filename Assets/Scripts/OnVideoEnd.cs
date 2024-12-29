using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class OnVideoEnd : MonoBehaviour
{
    public VideoPlayer video;

    // Start is called before the first frame update
    void Start()
    {
        video.loopPointReached += (_) => {
            gameObject.GetComponent<Button>().onClick.Invoke();
        };
    }
}
