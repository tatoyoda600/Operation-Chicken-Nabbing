using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuardScript : MonoBehaviour
{
    public enum MarkerFading
    {
        Normal = 0,
        Fading = 1,
        Faded = 2
    }
    MarkerFading curFadeStage = MarkerFading.Faded;
    Color markerColor { get { return new Color(210f / 255f, 45f / 255f, 45f / 255f, 1f); } }
    Color fadingColor { get { return new Color(210f / 255f, 45f / 255f, 45f / 255f, 0.6f); } }
    Color fadedColor { get { return new Color(210f / 255f, 90f / 255f, 45f / 255f, 0.3f); } }
    const int fadeLifetime = 2;

    public PathWeb pathWeb;
    public List<string> path;
    Animator anim;
    SpriteRenderer sprite;
    static Coroutine coroutine = null;

    private void Awake()
    {
        TimeManager.instance.RegisterGuard(this);
        anim = gameObject.GetComponent<Animator>();
        sprite = gameObject.GetComponent<SpriteRenderer>();
        ShowMarker(false);
    }

    public void ShowMarker(bool show, MarkerFading fadeStage = MarkerFading.Normal)
    {
        if (coroutine == null)
        {
            sprite.enabled = show;
            switch (fadeStage)
            {
                case MarkerFading.Normal:
                    sprite.color = markerColor;
                    break;
                case MarkerFading.Fading:
                    sprite.color = fadingColor;
                    break;
                case MarkerFading.Faded:
                    if (curFadeStage == MarkerFading.Fading)
                    {
                        GameObject fadedGo = new GameObject(gameObject.name + "_Faded", typeof(SpriteRenderer));
                        fadedGo.transform.SetParent(gameObject.transform.parent);
                        fadedGo.transform.position = gameObject.transform.position;
                        SpriteRenderer fadedSprite = fadedGo.GetComponent<SpriteRenderer>();
                        fadedSprite.sprite = sprite.sprite;
                        fadedSprite.color = fadedColor;
                        fadedSprite.sortingOrder = sprite.sortingOrder;
                        sprite.color = Color.white;
                        TimeManager.instance.RegisterDestroyObject(fadedGo, fadeLifetime);
                    }
                    break;
            }
            curFadeStage = fadeStage;
        }
    }

    public void CatchPlayer()
    {
        sprite.color = Color.white;
        sprite.enabled = true;
        anim.SetInteger("Face", Random.Range(0, 6));
        if (coroutine == null)
        {
            gameObject.GetComponent<AudioSource>().Play();
            coroutine = StartCoroutine(ResetScene());
        }
    }

    IEnumerator ResetScene()
    {
        yield return new WaitForSecondsRealtime(2);
        coroutine = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
