using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Codec : MonoBehaviour
{
    public PathWeb pathWeb;
    public GameObject disabledCodecPrefab;
    public AudioClip keySound;

    string curLetter = null;
    string curNumber = null;
    GameObject disabledCodecGo;

    public void InputLetter(string letter)
    {
        if (!disabledCodecGo)
        {
            AudioSource source = gameObject.GetComponent<AudioSource>();
            source.clip = keySound;
            source.Play();

            curLetter = letter;
            CallNeuro();
        }
    }

    public void InputNumber(string number)
    {
        if (!disabledCodecGo)
        {
            AudioSource source = gameObject.GetComponent<AudioSource>();
            source.clip = keySound;
            source.Play();

            curNumber = number;
            CallNeuro();
        }
    }

    void CallNeuro()
    {
        if (curLetter != null && curNumber != null)
        {
            string roomName = curLetter + curNumber;
            pathWeb.ScanNode(roomName);
            curLetter = null;
            curNumber = null;

            // Disable codec screen
            disabledCodecGo = Instantiate(disabledCodecPrefab, gameObject.transform.position, Quaternion.identity, gameObject.transform);
            disabledCodecGo.transform.GetComponentInChildren<Animator>().SetInteger("Face", Random.Range(0, 6));
            TimeManager.instance.RegisterDestroyObject(disabledCodecGo, TimeManager.RoomScan.delayTime);
        }
    }
}
