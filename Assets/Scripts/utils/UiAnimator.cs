using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public abstract class UiAnimator: MonoBehaviour
{
    [Header("Visibility")]
    public bool IsVisible = false;

    [Header("Element start visibility")]
    public bool initiallyVisible = false;

    [Header("Element interactible during animation")]
    public bool interactableOnAnimation = false;

    [Header("Element animation show on start")]
    public bool showOnStart = false;

    [Header("Element animation hide on start")]
    public bool hideOnStart = false;

    [Header("On Show callback")]
    public UnityEvent onShow;

    [Header("On Hide callback")]
    public UnityEvent onHide;

    [Header("Element animation delay")]
    public float animationDelay = 0f;

    [Header("On show delay")]
    public float onShowDelay = 0f;

    [Header("On hide delay")]
    public float onHideDelay = 0f;

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

    public void Start()
    {
        if(showOnStart) Show();
        else if (hideOnStart) Hide();
    }
}
