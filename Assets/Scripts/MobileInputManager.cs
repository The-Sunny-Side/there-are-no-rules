using TMPro;
using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;

    [Header("Output")]
    public bool rightTapped;
    public bool leftTapped;
    public bool jumpTapped;

    [Header("Timing")]
    public float lastTapTime = 0f;               // Tempo da quando il dito/puntatore è sullo schermo
    [SerializeField] private float rotateThreshold = 0.5f; // Dopo quanto inizia la rotazione
    [SerializeField] private float jumpThreshold = 0.3f;   // Finestra per il doppio tap

    [Header("State")]
    public bool isFirstTapped = false;   // Primo tap rilevato (in attesa del secondo)
    public bool isTapping = false;       // Puntatore attualmente sullo schermo
    public bool isRotating = false;      // Abbiamo superato la soglia per ruotare
    public float delayDoubleTap = 0f;    // Tempo trascorso dal primo tap

    // Variabili interne per unificare mouse e touch
    private bool pointerDownThisFrame;
    private bool pointerUpThisFrame;
    private bool pointerHeld;
    private Vector2 pointerPosition;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Update()
    {
        // Prima leggiamo lo stato “grezzo” del puntatore (mouse o touch)
        ReadPointerState();

        // Poi applichiamo la stessa logica di prima
        ReadInput();
        UpdateRotateTimer();
        UpdateDoubleTapTimer();
    }

    #region Pointer Abstraction (mouse + touch)

    /// <summary>
    /// Legge lo stato del puntatore per questo frame, unificando mouse e primo touch.
    /// </summary>
    private void ReadPointerState()
    {
        pointerDownThisFrame = false;
        pointerUpThisFrame = false;
        pointerHeld = false;

        // --- PRIORITÀ TOUCH ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    pointerDownThisFrame = true;
                    pointerHeld = true;
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    pointerHeld = true;
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    pointerUpThisFrame = true;
                    pointerHeld = false;
                    break;
            }
        }
        else
        {
            // --- FALLBACK MOUSE (PC) ---
            pointerPosition = Input.mousePosition;

            pointerDownThisFrame = Input.GetMouseButtonDown(0);
            pointerUpThisFrame = Input.GetMouseButtonUp(0);
            pointerHeld = Input.GetMouseButton(0);
        }
    }

    #endregion

    #region Update Timers (logica temporale SEPARATA)

    /// <summary>
    /// Gestisce il timer per la rotazione (tap lungo).
    /// </summary>
    private void UpdateRotateTimer()
    {
        if (isTapping)
        {
            lastTapTime += Time.deltaTime;

            // Se il puntatore è tenuto abbastanza a lungo, abilita la modalità rotazione
            if (lastTapTime > rotateThreshold)
            {
                isRotating = true;
            }
        }
        else
        {
            // Quando il puntatore viene sollevato, resetta il timer e lo stato
            isRotating = false;
            lastTapTime = 0f;
        }
    }

    /// <summary>
    /// Gestisce il timer per il doppio tap (salto).
    /// </summary>
    private void UpdateDoubleTapTimer()
    {
        if (!isFirstTapped)
            return;

        // Incrementa il tempo da quando è stato fatto il primo tap
        delayDoubleTap += Time.deltaTime;

        // Se il secondo tap non arriva in tempo, invalida il tentativo di doppio tap
        if (delayDoubleTap > jumpThreshold)
        {
            isFirstTapped = false;
            delayDoubleTap = 0f;
        }
    }

    #endregion

    #region Input Raw Handling

    /// <summary>
    /// Resetta i flag di input a ogni frame.
    /// </summary>
    private void ResetInput()
    {
        rightTapped = false;
        leftTapped = false;
        jumpTapped = false;
    }

    /// <summary>
    /// Usa lo stato unificato del puntatore per applicare la logica di gioco.
    /// </summary>
    private void ReadInput()
    {
        ResetInput();

        HandleTapStartEnd();
        HandleRotateInput();
        HandleJumpDoubleTap();
    }

    /// <summary>
    /// Gestisce inizio/fine del tap (down/up) e aggiorna isTapping.
    /// </summary>
    private void HandleTapStartEnd()
    {
        if (pointerDownThisFrame)
        {
            isTapping = true;
        }

        if (pointerUpThisFrame)
        {
            isTapping = false;
        }
    }

    /// <summary>
    /// Se siamo in modalità rotazione, decide se andare a destra o sinistra
    /// in base alla posizione del puntatore rispetto al centro dello schermo.
    /// </summary>
    private void HandleRotateInput()
    {
        if (!pointerHeld)
            return;

        if (!isRotating)
            return;

        float x = pointerPosition.x;

        if (x > Screen.width / 2f)
        {
            rightTapped = true;
        }
        else
        {
            leftTapped = true;
        }
    }

    /// <summary>
    /// Gestisce il doppio tap per il salto.
    /// La parte temporale (entro quando) è gestita da UpdateDoubleTapTimer().
    /// Qui decidiamo solo cosa succede quando arriva un nuovo tap (down).
    /// </summary>
    private void HandleJumpDoubleTap()
    {
        if (!pointerDownThisFrame)
            return;

        // Primo tap: avvia la finestra per il doppio tap
        if (!isFirstTapped)
        {
            isFirstTapped = true;
            delayDoubleTap = 0f; // ripartiamo da zero
            return;
        }

        // Se siamo qui, un primo tap era già avvenuto
        // Controlliamo se il secondo tap è entro la finestra di tempo
        if (delayDoubleTap <= jumpThreshold)
        {
            // Doppio tap valido -> salto
            jumpTapped = true;
            isFirstTapped = false;
            delayDoubleTap = 0f;
        }
        else
        {
            // Troppo tardi: questo tap diventa il nuovo "primo tap"
            isFirstTapped = true;
            delayDoubleTap = 0f;
        }
    }

    #endregion
}
