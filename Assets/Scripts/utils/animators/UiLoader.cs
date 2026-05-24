using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UiLoader
{
    [SerializeField]
    public LoaderType key;

    [SerializeField]
    public List<UiTransition> transitions = new List<UiTransition>();

    [SerializeField]
    public List<UiAnimator> animators = new List<UiAnimator>();
}
