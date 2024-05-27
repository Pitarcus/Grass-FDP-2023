using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassColorType : MonoBehaviour
{
    public GrassColorTypeEnum colorType;

}
[Serializable]
public enum GrassColorTypeEnum
{
    bottom,
    top,
    tip,
    sss
}
