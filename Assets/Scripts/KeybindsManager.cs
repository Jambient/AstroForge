using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

public class KeybindsManager : MonoBehaviour
{
    public PlayerInput playerInput;
    private RebindingOperation rebindingOperation;
    private const string RebindsKey = "rebinds";

    private void Awake()
    {
        string rebinds = PlayerPrefs.GetString(RebindsKey, string.Empty);

        if (string.IsNullOrEmpty(rebinds)) { return; }

        playerInput.actions.LoadBindingOverridesFromJson(rebinds);
    }

    public void Save()
    {
        string rebinds = playerInput.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(RebindsKey, rebinds);
    }

    public IEnumerator StartRebindingCoroutine(string actionName, Action onFinish)
    {
        InputAction actionReference = playerInput.actions.FindActionMap("Game").FindAction(actionName);

        rebindingOperation = actionReference.PerformInteractiveRebinding()
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => RebindCancelled())
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindComplete())
            .Start();

        while (!rebindingOperation.completed)
        {
            yield return null;
        }

        onFinish();
    }

    private void RebindCancelled()
    {
        Debug.Log("cancelled rebinding");
    }

    private void RebindComplete()
    {
        Save();
        rebindingOperation.Dispose();
    }
}