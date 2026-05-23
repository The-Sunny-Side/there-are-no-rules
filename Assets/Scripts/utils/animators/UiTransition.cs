using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public abstract class UiTransition : MonoBehaviour
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

    [Header("Element animation show on enable")]
    public bool showOnEnable = false;

    [Header("Element animation hide on enable")]
    public bool hideOnEnable = false;

    [Header("Element animation show on disable")]
    public bool showOnDisable = false;

    [Header("Element animation hide on disable")]
    public bool hideOnDisable = false;

    [Header("On Show callback")]
    public UnityEvent onShow;

    [Header("On Hide callback")]
    public UnityEvent onHide;

    [Header("Delays")]

    public float animationDelay = 0f;

    public float onShowDelay = 0f;

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

    public void OnEnable()
    {
        if (showOnEnable) Show();
        else if (hideOnEnable) Hide();
    }

    public void OnDisable()
    {
        if (showOnDisable) Show();
        else if (hideOnDisable) Hide();
    }

    public void ToggleAnimator() { 
        if (IsVisible) Hide();
        else Show();
    }
}
