using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderRestoreValue : MonoBehaviour
{
    public enum SliderTypes
    {
        MasterVolume = 0,
        MusicVolume = 1,
        SfxVolume = 2,
        TimeScale = 3,
    }

    public SliderTypes sliderType;
    static readonly string[] playerPrefsKeys = { MusicTracks.MASTER_CHANNEL, MusicTracks.MUSIC_CHANNEL, MusicTracks.SFX_CHANNEL, TimeManager.TIME_SCALE_PREF };

    private void Awake()
    {
        float value = PlayerPrefs.GetFloat(playerPrefsKeys[((int)sliderType)], float.MinValue);
        if (value > float.MinValue)
        {
            gameObject.GetComponent<Slider>().value = value;
        }
    }
}
