using UnityEngine;

public class UiAnimationManager : MonoBehaviour
{
    [SerializeField]
    private UiTransition[] transitions;

    void Start()
    {
        
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
