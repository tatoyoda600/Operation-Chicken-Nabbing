using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    public AudioClip keyDownSound;
    public AudioClip keyUpSound;
    public AudioSource buttonSource;
    public AudioMixer mixer;
    public GameObject pauseMenu;

    public void PlaySoundDown()
    {
        buttonSource.time = 0;
        buttonSource.clip = keyDownSound;
        buttonSource.Play();
    }

    public void PlaySoundUp()
    {
        buttonSource.clip = keyUpSound;
        if (buttonSource.isPlaying)
        {
            buttonSource.PlayDelayed(buttonSource.clip.length - buttonSource.time);
        }
        else
        {
            buttonSource.time = 0;
            buttonSource.Play();
        }
    }

    public void ChangeScene(string sceneName)
    {
        StartCoroutine(DelayChangeScene(sceneName));
    }

    IEnumerator DelayChangeScene(string sceneName)
    {
        // Delay scene change to allow button click sounds to play
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(sceneName);
    }

    public void UnpauseGame()
    {
        if (GameManager.instance)
        {
            GameManager.instance.PauseGame(false);
        }
        else
        {
            pauseMenu.SetActive(false);
        }
    }

    public void EndTurn()
    {
        TimeManager.instance.EndTurn();
    }

    public void UpdateMasterVolume(System.Single value) { UpdateChannelVolume(MusicTracks.MASTER_CHANNEL, value); }
    public void UpdateMusicVolume(System.Single value) { UpdateChannelVolume(MusicTracks.MUSIC_CHANNEL, value); }
    public void UpdateEffectsVolume(System.Single value) { UpdateChannelVolume(MusicTracks.SFX_CHANNEL, value); }

    void UpdateChannelVolume(string channel, float value)
    {
        PlayerPrefs.SetFloat(channel, value);
        mixer.SetFloat(channel, ScaleVolumeSliderValue(value));
    }

    public static float ScaleVolumeSliderValue(float value)
    {
        return Mathf.Log10(Mathf.Max(value / 100.0f, 0.0001f)) * 20;
    }

    public void UpdateTimeScale(Slider slider)
    {
        PlayerPrefs.SetFloat(TimeManager.TIME_SCALE_PREF, slider.value);
        float scaledValue = ScaleTimeScaleSliderValue(slider.value);

        TextMeshProUGUI textComponent = slider.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textComponent.text = textComponent.text.Split(':')[0] + $": {scaledValue.ToString("0.0")}s";

        if (TimeManager.instance != null)
        {
            TimeManager.instance.ChangeTimeScale(scaledValue);
        }
    }

    public static float ScaleTimeScaleSliderValue(float value)
    {
        return value / 10.0f;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
