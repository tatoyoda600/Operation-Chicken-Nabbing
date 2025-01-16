using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PixelPerfectCenteredText : MonoBehaviour
{
    const float PIXEL_WIDTH = 6;

    TextMeshProUGUI textComponent;

    [InspectorButton("Re-Center")]
    public void UpdateText()
    {
        if (!textComponent)
        {
            textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        }

        textComponent.margin = Vector4.zero;
        textComponent.ForceMeshUpdate();
        float leftOffset = 0.5f * PIXEL_WIDTH * (Mathf.Floor(textComponent.rectTransform.rect.size.x / PIXEL_WIDTH) - Mathf.Round(textComponent.textBounds.size.x / PIXEL_WIDTH));
        textComponent.margin = new Vector4(leftOffset, 0, 0, 0);
        textComponent.ForceMeshUpdate();
    }

    public void UpdateText(string text)
    {
        if (!textComponent)
        {
            textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        }

        textComponent.text = text;
        UpdateText();
    }
}
