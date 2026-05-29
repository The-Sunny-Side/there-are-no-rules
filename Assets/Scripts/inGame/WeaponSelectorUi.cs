using UnityEngine;

/// <summary>
/// Gestisce i 4 spicchi dall'alto livello.
/// Metti questo script su un GameObject padre nel Canvas.
/// Assegna i 4 SliceFillController figli nell'Inspector.
/// </summary>
public class WeaponSelectorUI : MonoBehaviour
{
    [Header("Spicchi")]
    public SliceFillController SliceTop;
    public SliceFillController SliceRight;
    public SliceFillController SliceBottom;
    public SliceFillController SliceLeft;

    // ── Esempio d'uso ─────────────────────────────────────────────────────

    void Start()
    {
        // Esempio: imposta valori iniziali diversi per ogni spicchio
        SliceTop.SetImmediate(0f);
        SliceRight.SetImmediate(0f);
        SliceBottom.SetImmediate(0f);
        SliceLeft.SetImmediate(0f);
    }

    /// <summary>
    /// Imposta il fill di uno spicchio specifico con animazione.
    /// index: 0=Top, 1=Right, 2=Bottom, 3=Left
    /// </summary>
    public void SetSlice(int index, float value)
    {
        SliceFillController target = index switch
        {
            0 => SliceTop,
            1 => SliceRight,
            2 => SliceBottom,
            3 => SliceLeft,
            _ => null
        };

        target?.FillTo(value);
    }

    /// <summary>
    /// Imposta tutti e 4 gli spicchi con lo stesso valore.
    /// </summary>
    public void SetAll(float value)
    {
        SliceTop.FillTo(value);
        SliceRight.FillTo(value);
        SliceBottom.FillTo(value);
        SliceLeft.FillTo(value);
    }

    /// <summary>
    /// Imposta i 4 spicchi con valori indipendenti.
    /// </summary>
    public void SetValues(float top, float right, float bottom, float left)
    {
        SliceTop.FillTo(top);
        SliceRight.FillTo(right);
        SliceBottom.FillTo(bottom);
        SliceLeft.FillTo(left);
    }
}