using System;
using UnityEngine;

[Serializable]
public abstract class Animator: MonoBehaviour
{
    public bool IsVisible = false;
    public bool initiallyVisible = false;

    abstract public void Show();
    abstract public void Hide();
    abstract public void ShowInstant();
    abstract public void HideInstant();
    abstract public void SetVisibility(bool visible);
}
