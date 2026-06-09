using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class UiTransitionTypewriter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float charactersPerSecond = 30f;

    private TMP_Text textComponent;

    private Coroutine typingCoroutine;

    private bool isTyping;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();

        if (textComponent == null)
        {
            throw new MissingComponentException(
                $"{nameof(UiTransitionTypewriter)} requires a TMP_Text component.");
        }
    }

    /// <summary>
    /// Starts the typewriter animation.
    /// </summary>
    public void ShowText(string message)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(message));
    }

    /// <summary>
    /// Instantly completes the current text animation.
    /// </summary>
    public void Skip()
    {
        if (!isTyping)
            return;

        StopCoroutine(typingCoroutine);

        textComponent.maxVisibleCharacters = textComponent.text.Length;

        isTyping = false;
        typingCoroutine = null;
    }

    private IEnumerator TypeText(string message)
    {
        isTyping = true;

        textComponent.text = message;

        // Important: force TMP to generate text info immediately
        textComponent.ForceMeshUpdate();

        int totalCharacters = textComponent.textInfo.characterCount;

        textComponent.maxVisibleCharacters = 0;

        float delay = 1f / charactersPerSecond;

        for (int i = 0; i <= totalCharacters; i++)
        {
            textComponent.maxVisibleCharacters = i;

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingCoroutine = null;
    }
}