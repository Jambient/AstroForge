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
    private bool isHovering;
    private bool isSelected;

    private void Start()
    {
        buttonComponent = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        normalColor = buttonText.color;

        buttonComponent.onClick.AddListener(() =>
        {
            isHovering = false;
            isSelected = false;
            EventSystem.current.SetSelectedGameObject(null);
        });
    }

    private void Update()
    {
        if (!buttonComponent.interactable) { return; }
        buttonText.color = isHovering || isSelected ? hoverColor : normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }
}
