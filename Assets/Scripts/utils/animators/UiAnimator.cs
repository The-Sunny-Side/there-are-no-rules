using System;
using UnityEngine;

public abstract class UiAnimator : MonoBehaviour
{
    [SerializeField]
    public bool animate;

    public abstract void Animate();

    public virtual void StopAnimation() { }

    public void Update()
    {
        if (animate)
        {
            Animate();
        }else
        {
            StopAnimation();
        }
    }
}
