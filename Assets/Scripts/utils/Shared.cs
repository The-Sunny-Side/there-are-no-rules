using System;
using UnityEngine;

[Serializable]
public class HighlightableElement
{
    public GameObject element;
    public GameObject highlight;
}

[Serializable]
public class VehicleElement
{
    public GameObject element;
    public Sprite icon;
}
public enum Mode { Host, ServerOnly, Client }
