using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;
    public const float actionTime = 2;
    public AudioClip scanSound;

    public float timeElapsed = 0;
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

            if (timeElapsed > actionTime)
            {
                OnTurnStart?.Invoke();
                anim.Play("Animation", -1, 0);
                timeElapsed -= actionTime;
                AfterTurnStart?.Invoke();
            }
        }
    }
}
