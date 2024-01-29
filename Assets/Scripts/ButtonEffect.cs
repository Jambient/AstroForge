using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private Button buttonComponent;
    private TextMeshProUGUI buttonText;
    private Color hoverColor = new Color(32 / 255, 32 / 255, 32 / 255);
    private Color normalColor;

    private void Start()
    {
        buttonComponent = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        normalColor = buttonText.color;
    }

    private void ButtonEnter()
    {
        if (!buttonComponent.interactable) { return; }
        buttonText.color = hoverColor;
    }

    private void ButtonLeave()
    {
        if (!buttonComponent.interactable) { return; }
        buttonText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ButtonEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ButtonLeave();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ButtonEnter();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ButtonLeave();
    }
}
