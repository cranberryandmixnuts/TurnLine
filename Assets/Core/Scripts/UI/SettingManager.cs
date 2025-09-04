using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [SerializeField] private Slider timeScaleSlider;
    [SerializeField] private TMP_Text timeScaleLabel;
    [SerializeField] private TMP_Dropdown aiDropdown;

    public int AI;

    private const string KeyTimeScale = "settings.timescale.slider";
    private const string KeyAIIndex = "settings.ai.index";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Another instance of GameController exists, destroying this.");
            Destroy(this);
            return;
        }
        Instance = this;

        timeScaleSlider.wholeNumbers = true;
        timeScaleSlider.minValue = 0;
        timeScaleSlider.maxValue = 20;

        int savedSlider = PlayerPrefs.HasKey(KeyTimeScale) ? PlayerPrefs.GetInt(KeyTimeScale) : 10;
        if (savedSlider < 0) savedSlider = 0;
        if (savedSlider > 20) savedSlider = 20;
        timeScaleSlider.value = savedSlider;
        UpdateLabel(savedSlider);

        int savedAIIndex = PlayerPrefs.HasKey(KeyAIIndex) ? PlayerPrefs.GetInt(KeyAIIndex) : 1;
        if (savedAIIndex < 0) savedAIIndex = 0;
        if (savedAIIndex > 3) savedAIIndex = 3;
        aiDropdown.value = savedAIIndex;
        AI = savedAIIndex + 1;
    }

    private void OnEnable()
    {
        timeScaleSlider.onValueChanged.AddListener(OnSliderChanged);
        aiDropdown.onValueChanged.AddListener(OnDropdownChanged);
        UpdateLabel((int)timeScaleSlider.value);
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        timeScaleSlider.onValueChanged.RemoveListener(OnSliderChanged);
        aiDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        Time.timeScale = ComputeScale((int)timeScaleSlider.value);
    }

    private void OnSliderChanged(float v)
    {
        int iv = Mathf.RoundToInt(v);
        if (!Mathf.Approximately(timeScaleSlider.value, iv)) timeScaleSlider.value = iv;
        UpdateLabel(iv);
        PlayerPrefs.SetInt(KeyTimeScale, iv);
        PlayerPrefs.Save();
    }

    private void OnDropdownChanged(int index)
    {
        if (index < 0) index = 0;
        if (index > 3) index = 3;
        aiDropdown.value = index;
        AI = index + 1;
        PlayerPrefs.SetInt(KeyAIIndex, index);
        PlayerPrefs.Save();
    }

    private float ComputeScale(int v)
    {
        if (v <= 0) return 0f;
        if (v >= 20) return 2f;
        return v / 10f;
    }

    private string FormatLabel(int v)
    {
        if (v == 0) return "일시정지";
        if (v == 10) return "1 (기본값)";
        if (v < 10) return $"0.{v}";
        if (v == 20) return "2.0";
        return $"1.{v - 10}";
    }

    private void UpdateLabel(int v)
    {
        timeScaleLabel.text = FormatLabel(v);
    }
}