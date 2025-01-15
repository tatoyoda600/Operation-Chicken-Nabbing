using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;
    public float actionTime = 2;
    public const string TIME_SCALE_PREF = "TimeScale";

    public float timeElapsed { get; private set; } = 0;
    bool frozen = false;
    Animator anim;

    public delegate void TurnHandler();
    public event TurnHandler OnTurnStart;
    public event TurnHandler AfterTurnStart;

    private void Awake()
    {
        if (instance)
        {
            Debug.LogError("Duplicate TimeManagers");
            DestroyImmediate(this);
        }
        else
        {
            instance = this;
            anim = gameObject.GetComponent<Animator>();
            float storedTimeScale = PlayerPrefs.GetFloat(TIME_SCALE_PREF, float.MinValue);
            if (storedTimeScale > float.MinValue)
            {
                ChangeTimeScale(ButtonHandler.ScaleTimeScaleSliderValue(storedTimeScale));
            }
            FreezeTimer(false);
            AlternateRuleTile.ClearMaps();
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void FreezeTimer(bool freeze)
    {
        anim.speed = freeze ? 0 : 1.0f / actionTime;
        frozen = freeze;
    }

    private void Update()
    {
        if (!frozen)
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed >= actionTime)
            {
                EndTurn();
            }
        }
    }

    public void EndTurn()
    {
        OnTurnStart?.Invoke();
        anim.Play("Animation", -1, 0);
        timeElapsed = timeElapsed >= actionTime ? timeElapsed - actionTime : 0.0f;
        AfterTurnStart?.Invoke();
    }

    public void ChangeTimeScale(float scale)
    {
        float percentage = timeElapsed / actionTime;
        actionTime = scale;
        timeElapsed = percentage * actionTime;
        anim.speed = frozen ? 0 : 1.0f / actionTime;
    }
}
