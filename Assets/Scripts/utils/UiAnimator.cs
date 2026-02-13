using System;
using UnityEngine;

[Serializable]
public abstract class UiAnimator: MonoBehaviour
{
    [Header("Visibility")]
    public bool IsVisible = false;

    [Header("Element start visibility")]
    public bool initiallyVisible = false;

    [Header("Element interactible during animation")]
    public bool interactableOnAnimation = false;

    abstract public void Show();
    abstract public void Hide();
    abstract public void ShowInstant();
    abstract public void HideInstant();
    public void SetVisibility(bool visible)
    {
        IsVisible = visible;
        if (visible) Show();
        else Hide();
    }
}
