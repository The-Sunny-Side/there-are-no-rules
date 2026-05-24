using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UiAnimationManager: MonoBehaviour
{

    [SerializeField]
    private string key;

    [SerializeField]
    private List<UiTransition> transitions = new List<UiTransition>();

    [SerializeField]
    private List<UiAnimator> animators = new List<UiAnimator>();

    public void Animate() {         
        
        foreach(UiAnimator animator in animators)
        {
            animator.Animate();
        }
    }

    public void StopAnimation() {         
        
        foreach(UiAnimator animator in animators)
        {
            animator.StopAnimation();
        }
    }

    public void Show()
    {
       foreach(UiTransition animator in transitions)
        {
            animator.Show();
        }
    }

    public void Hide()
    {
        foreach(UiTransition animator in transitions)
        {
            animator.Hide();
        }
    }
}
