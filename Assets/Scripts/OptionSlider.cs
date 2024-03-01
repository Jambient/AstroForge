using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private string playerPrefsKey;

    public void Start()
    {
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            slider.value = PlayerPrefs.GetFloat(playerPrefsKey);
        }
    }

    public void OnValueChanged(float value)
    {
        valueText.text = value.ToString();
    }
}
