using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonHandler : MonoBehaviour
{
    public AudioClip keyDownSound;
    public AudioClip keyUpSound;
    public AudioSource buttonSource;

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
        GameManager.instance.PauseGame(false);
    }
}
