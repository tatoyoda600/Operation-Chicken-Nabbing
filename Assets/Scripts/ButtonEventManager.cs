using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonEventManager : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    public UnityEvent pointerDown;
    public UnityEvent pointerUp;

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        pointerUp.Invoke();
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        pointerDown.Invoke();
    }
}
