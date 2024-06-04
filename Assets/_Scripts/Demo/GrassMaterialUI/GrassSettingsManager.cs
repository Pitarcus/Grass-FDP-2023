using HSVPicker;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class GrassSettingsManager : MonoBehaviour
{
    [SerializeField] private GrassMaster grassMaster;
    [SerializeField] private StaticWindMaster windMaster;
    [SerializeField] private LightingManager lightingManager;
    [SerializeField] private GrassMaterialParameters_SO originalParameters;
    [SerializeField] private GrassMaterialParameters_SO currentParameters;

    [Header("Grass")]
    [SerializeField] private ColorPicker colorPicker;

    [SerializeField] private List<Button> colorButtons;
    [SerializeField] private TMP_Dropdown densityDropdown;
    [SerializeField] private UnityEngine.UI.Slider scaleSlider;
    [SerializeField] private UnityEngine.UI.Slider scaleRandomSlider;
    [SerializeField] private UnityEngine.UI.Slider bendSlider;

    [Header("Wind")]
    [SerializeField] private UnityEngine.UI.Toggle windToggle;
    [SerializeField] private UnityEngine.UI.Slider strengthSlider;
    [SerializeField] private UnityEngine.UI.Slider speedSlider;
    [SerializeField] private UnityEngine.UI.Slider rotationSlider;

    [Header("Time")]
    [SerializeField] private UnityEngine.UI.Toggle runTimeToggle;
    [SerializeField] private UnityEngine.UI.Slider timeOfDay;
    [SerializeField] private UnityEngine.UI.Slider speedOfTime;

    private GrassColorTypeEnum currentColorType;
    private bool disableColorChange = false;
    private UnityAction UA;

    private void Start()
    {
        SetGrassParameters();
        SetWindParameters();
        SetTimeParameters();
    }

    private void OnEnable()
    {
        SetButtonsListeners(true);

        densityDropdown.onValueChanged.AddListener(OnDensityChanged);
        scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        scaleRandomSlider.onValueChanged.AddListener(OnScaleRandomChanged);
        bendSlider.onValueChanged.AddListener(OnBendChanged);

        colorPicker.onValueChanged.AddListener(OnColorChanged);

        windToggle.onValueChanged.AddListener(OnWindToggle);
        strengthSlider.onValueChanged.AddListener(OnStrengthChanged);
        speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        rotationSlider.onValueChanged.AddListener(OnRotationChanged);

        runTimeToggle.onValueChanged.AddListener(OnTimeToggleChanged);
        timeOfDay.onValueChanged.AddListener(OnTimeOfDayChanged);
        speedOfTime.onValueChanged.AddListener(OnTimeSpeedChanged);
    }
    private void OnDisable()
    {
        SetButtonsListeners(false);

        densityDropdown.onValueChanged.RemoveListener(OnDensityChanged);
        scaleSlider.onValueChanged.RemoveListener(OnScaleChanged);
        scaleRandomSlider.onValueChanged.RemoveListener(OnScaleRandomChanged);
        bendSlider.onValueChanged.RemoveListener(OnBendChanged);

        colorPicker.onValueChanged.RemoveListener(OnColorChanged);

        windToggle.onValueChanged.RemoveListener(OnWindToggle);
        strengthSlider.onValueChanged.RemoveListener(OnStrengthChanged);
        speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
        rotationSlider.onValueChanged.RemoveListener(OnRotationChanged);

        runTimeToggle.onValueChanged.RemoveListener(OnTimeToggleChanged);
        timeOfDay.onValueChanged.RemoveListener(OnTimeOfDayChanged);
        speedOfTime.onValueChanged.RemoveListener(OnTimeSpeedChanged);
    }

    private void SetButtonsListeners(bool add)
    {
        foreach (Button button in colorButtons)
        {
            GrassColorType colorType = button.GetComponent<GrassColorType>();

            UA = new UnityAction(() => OnColorButtonPressed(colorType));

            if (add)
            {
                button.onClick.AddListener(UA);
            }
            else
            {
                button.onClick.RemoveListener(UA);
            }
        }
    }

    public void ResetGrassParameters()
    {
        SetGrassParameters();
    }

    private void SetGrassParameters()
    {
        currentParameters = Instantiate(originalParameters);
        grassMaster.grassParameters = currentParameters;

        densityDropdown.value = 1;
        scaleSlider.value = currentParameters.scaleY;
        scaleRandomSlider.value = currentParameters.randomYScaleNoise;
        bendSlider.value = currentParameters.maxBend;
    }

    private void SetWindParameters()
    {
        windToggle.isOn = !originalParameters.randomBend;

        strengthSlider.value = windMaster.WindStrength;
        speedSlider.value = windMaster.WindSpeed;
        rotationSlider.value = windMaster.WindRotation;
    }

    private void SetTimeParameters()
    {
        runTimeToggle.isOn = lightingManager.runCycle;

        timeOfDay.value = lightingManager.timeOfDayNormalized;
        speedOfTime.value = lightingManager.dayPeriod;
    }

    public void OnColorButtonPressed(GrassColorType type)
    {
        disableColorChange = true;

        switch (type.colorType)
        {
            case GrassColorTypeEnum.bottom:
                colorPicker.CurrentColor = currentParameters.bottomColor;
                currentColorType = GrassColorTypeEnum.bottom;
                break;

            case GrassColorTypeEnum.top:
                colorPicker.CurrentColor = currentParameters.topColor;
                currentColorType = GrassColorTypeEnum.top;
                break;

            case GrassColorTypeEnum.tip:
                colorPicker.CurrentColor = currentParameters.tipColor;
                currentColorType = GrassColorTypeEnum.tip;
                break;

            case GrassColorTypeEnum.sss:
                colorPicker.CurrentColor = currentParameters.SSSColor;
                currentColorType = GrassColorTypeEnum.sss;
                break;
        }
    }

    public void OnColorChanged(Color color)
    {
        if (disableColorChange)
        {
            disableColorChange = false;
            return;
        }
        switch (currentColorType)
        {
            case GrassColorTypeEnum.bottom:
                currentParameters.bottomColor = color;
                break;

            case GrassColorTypeEnum.top:
                currentParameters.topColor = color;
                break;

            case GrassColorTypeEnum.tip:
                currentParameters.tipColor = color;
                break;

            case GrassColorTypeEnum.sss:
                currentParameters.SSSColor = color;
                break;
        }
    }

    public void OnScaleChanged(float value)
    {
        currentParameters.scaleY = value;
    }
    public void OnScaleRandomChanged(float value)
    {
        currentParameters.randomYScaleNoise = value;
    }
    public void OnBendChanged(float value)
    {
        currentParameters.maxBend = value;
    }
    public void OnDensityChanged(int value)
    {
        switch (value)
        {
            case 0:
                grassMaster.Density = 2;
                break;
            case 1:
                grassMaster.Density = 4;
                break;
            case 2:
                grassMaster.Density = 6;
                break;
            case 3:
                grassMaster.Density = 9;
                break;
        }

        grassMaster.enabled = false;
        grassMaster.enabled = true;
    }

    public void OnWindToggle(bool toggle)
    {
        currentParameters.randomBend = !toggle;
    }
    public void OnStrengthChanged(float value)
    {
        windMaster.WindStrength = value;
    }
    public void OnSpeedChanged(float value)
    {
        windMaster.WindSpeed = value;
    }
    public void OnRotationChanged(float value)
    {
        windMaster.WindRotation = value;
    }

    public void OnTimeToggleChanged(bool value)
    {
        Debug.Log("Changing time run to: " + value);
        lightingManager.runCycle = value;
    }

    public void OnTimeOfDayChanged(float value)
    {
        lightingManager.SetTimeOfDay(value);
    }
    public void OnTimeSpeedChanged(float value)
    {
        lightingManager.ChangePeriod(240-value);
    }
}
