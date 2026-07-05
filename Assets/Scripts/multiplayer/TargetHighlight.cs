using UnityEngine;

// Owns the whole "this enemy is being targeted" visual: an overlay material on the body meshes
// plus a tinted nameplate. Ref-counted because a vehicle can mount up to 4 weapons (see
// Composer's weaponLeft/Right/Front/Back) that may target the same enemy at the same time -
// a plain on/off would let one weapon losing its target turn off a highlight another still wants.
public class TargetHighlight : MonoBehaviour
{
    [SerializeField] private Material overlayMaterial;
    [SerializeField] private Color highlightColor = Color.red;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private int _refCount;
    private MaterialPropertyBlock _mpb;
    private MeshRenderer _plateRenderer;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        var plate = transform.Find("Visuals/NamePlate/Plate");
        if (plate != null)
            _plateRenderer = plate.GetComponent<MeshRenderer>();
    }

    public void SetHighlighted(bool highlighted)
    {
        _refCount = Mathf.Max(0, _refCount + (highlighted ? 1 : -1));
        bool shouldHighlight = _refCount > 0;

        ApplyBodyOverlay(shouldHighlight);
        ApplyPlateColor(shouldHighlight);
    }

    private void ApplyBodyOverlay(bool shouldHighlight)
    {
        if (overlayMaterial == null) return;

        var composed = transform.Find("Visuals/Composed");
        if (composed == null) return;

        foreach (var bodyRenderer in composed.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials = bodyRenderer.sharedMaterials;
            bool hasOverlay = System.Array.IndexOf(materials, overlayMaterial) >= 0;

            if (shouldHighlight && !hasOverlay)
            {
                var newMaterials = new Material[materials.Length + 1];
                materials.CopyTo(newMaterials, 0);
                newMaterials[materials.Length] = overlayMaterial;
                bodyRenderer.sharedMaterials = newMaterials;
            }
            else if (!shouldHighlight && hasOverlay)
            {
                var newMaterials = new Material[materials.Length - 1];
                int index = 0;
                foreach (var material in materials)
                {
                    if (material == overlayMaterial) continue;
                    newMaterials[index++] = material;
                }
                bodyRenderer.sharedMaterials = newMaterials;
            }
        }
    }

    private void ApplyPlateColor(bool shouldHighlight)
    {
        if (_plateRenderer == null) return;

        var mat = _plateRenderer.sharedMaterial;
        if (mat == null) return;

        _plateRenderer.GetPropertyBlock(_mpb);

        if (shouldHighlight)
        {
            if (mat.HasProperty(BaseColorID))
                _mpb.SetColor(BaseColorID, highlightColor);
            else if (mat.HasProperty(ColorID))
                _mpb.SetColor(ColorID, highlightColor);
        }
        else
        {
            _mpb.Clear();
        }

        _plateRenderer.SetPropertyBlock(_mpb);
    }
}
