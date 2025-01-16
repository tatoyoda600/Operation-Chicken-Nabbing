using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyMapper : MonoBehaviour
{
    public InputActionReference inputAction;
    public PixelPerfectCenteredText textComponent;

    public const string KEY_MAPPINGS_PREF = "KeyMappings";

    private void Awake()
    {
        string keyMapping = PlayerPrefs.GetString(KEY_MAPPINGS_PREF, null);
        if (!string.IsNullOrEmpty(keyMapping))
        {
            inputAction.asset.LoadBindingOverridesFromJson(keyMapping);
        }
        textComponent.UpdateText(inputAction.action.GetBindingDisplayString());
    }

    public void Rebind()
    {
        textComponent.UpdateText("...");
        InputAction action = inputAction.action;
        if (GameManager.instance)
        {
            action = GameManager.instance.GetInputSystem().FindAction(inputAction.action.name);
        }
        action.Disable();
        action.PerformInteractiveRebinding().OnComplete((operation) => OnRebind(operation, action)).Start();
    }

    void OnRebind(InputActionRebindingExtensions.RebindingOperation operation, InputAction action)
    {
        textComponent.UpdateText(action.GetBindingDisplayString());
        inputAction.action.ApplyBindingOverride(operation.selectedControl.path);
        PlayerPrefs.SetString(KEY_MAPPINGS_PREF, inputAction.asset.SaveBindingOverridesAsJson());
        action.Enable();
        operation.Dispose();
    }
}
