using System.Collections.Generic;
using UnityEngine;

// Singleton di scena: mantiene la lista ordinata di tutti i SpawnPoint marker presenti.
// L'ordine è dato dall'ordine dei figli nella gerarchia.
public class RaceSpawnPoints : MonoBehaviour
{
    public static RaceSpawnPoints Instance { get; private set; }

    private readonly List<Transform> _points = new();
    public IReadOnlyList<Transform> Points => _points;
    public int Count => _points.Count;

    void Awake()
    {
        Instance = this;
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [ContextMenu("Refresh Spawn Points")]
    public void Refresh()
    {
        _points.Clear();
        foreach (var sp in GetComponentsInChildren<RaceSpawnPoint>(true))
            _points.Add(sp.transform);
    }

    public Transform GetAt(int index)
    {
        if (index < 0 || index >= _points.Count) return null;
        return _points[index];
    }
}
