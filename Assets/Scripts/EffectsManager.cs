using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    [SerializeField] private GameObject hitBalloon;
    private Canvas _canvas;
    private Camera _camera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _canvas = GetComponent<Canvas>();
        _camera = Camera.main;
    }

    public void SpawnBalloon(Vector3 worldPosition)
    {
        Vector2 screenPos = _camera.WorldToScreenPoint(worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            screenPos,
            null, 
            out Vector2 localPoint
        );

        GameObject spawned = Instantiate(hitBalloon, _canvas.transform);
        spawned.GetComponent<RectTransform>().localPosition = localPoint;

        StartCoroutine(Utilities.DelayedEvent(() => Destroy(spawned), 0.3f));
    }
}