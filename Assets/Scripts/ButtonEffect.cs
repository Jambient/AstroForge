using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    #region Variables
    private Button buttonComponent;
    private TextMeshProUGUI buttonText;
    private Color hoverColor = new Color(32f / 255f, 32f / 255f, 32f / 255f);
    private Color normalColor;
    private bool isHovering;
    private bool isSelected;
    #endregion

    #region Interface Methods
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
    #endregion

    #region MonoBehaviour Messages
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
    #endregion
}
