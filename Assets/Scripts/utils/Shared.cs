using System;
using UnityEngine;
using UnityEngine.Localization;

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

[Serializable]
public class VehicleEntry : VehicleElement
{
    public string key;
}

[Serializable]
public class VehicleWeaponEntry : VehicleEntry
{
    public int cooldown = 0;
    public bool front = true;
    public bool back = true;
    public bool left = true;
    public bool right = true;
}

[Serializable]
public class LanguageItem
{
    public Locale locale;
    public string text;
    public Sprite icon;
}