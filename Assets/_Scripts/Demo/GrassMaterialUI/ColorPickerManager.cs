using HSVPicker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ColorPickerManager : MonoBehaviour
{
    [SerializeField] private GrassMaster grassMaster;
    [SerializeField] private GrassMaterialParameters_SO originalParameters;
    [SerializeField] private GrassMaterialParameters_SO currentParameters;

    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private UnityEngine.UI.Slider scaleSlider;
    [SerializeField] private UnityEngine.UI.Slider scaleRandomSlider;
    [SerializeField] private UnityEngine.UI.Slider bendSlider;

    private GrassColorTypeEnum currentColorType;
    private bool disableColorChange = false;

    private void Start()
    {
        SetParameters();
    }

    public void ResetParameters()
    {
        SetParameters();
    }

    private void SetParameters()
    {
        currentParameters = Instantiate(originalParameters);
        grassMaster.grassParameters = currentParameters;

        scaleSlider.value = currentParameters.scaleY;
        scaleRandomSlider.value = currentParameters.randomYScaleNoise;
        bendSlider.value = currentParameters.maxBend;
    }

    public void OnColorPressed(GrassColorType type)
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

}
