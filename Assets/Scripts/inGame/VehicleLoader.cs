using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Non legge più dal file in Awake.
/// Viene chiamato dalla rete (PurrNet) passando la config del veicolo del player.
/// </summary>
public class VehicleLoader : MonoBehaviour
{
    [SerializeField] private Composer composer;

    /// <summary>
    /// Applica una configurazione veicolo ricevuta via rete.
    /// json deve essere JsonUtility.ToJson(new Vehicle(...))
    /// </summary>
    public void ApplyConfigJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("VehicleLoader: ApplyConfigJson ricevuto json vuoto");
            return;
        }

        Vehicle data = JsonUtility.FromJson<Vehicle>(json);
        if (data == null)
        {
            Debug.LogWarning("VehicleLoader: ApplyConfigJson json non valido");
            return;
        }

        var list = new Dictionary<string, GameObject>();

        // base / body
        if (!string.IsNullOrEmpty(data.baseElement))
            list["base"] = Resources.Load<GameObject>("baseElements/" + data.baseElement);

        if (!string.IsNullOrEmpty(data.bodyElement))
            list["body"] = Resources.Load<GameObject>("bodyElements/" + data.bodyElement);

        // armors (opzionali)
        if (!string.IsNullOrEmpty(data.armorBackElement))
            list["armorBackElement"] = Resources.Load<GameObject>("armors/" + data.armorBackElement);

        if (!string.IsNullOrEmpty(data.armorFrontElement))
            list["armorFrontElement"] = Resources.Load<GameObject>("armors/" + data.armorFrontElement);

        if (!string.IsNullOrEmpty(data.armorLeftElement))
            list["armorLeftElement"] = Resources.Load<GameObject>("armors/" + data.armorLeftElement);

        if (!string.IsNullOrEmpty(data.armorRightElement))
            list["armorRightElement"] = Resources.Load<GameObject>("armors/" + data.armorRightElement);

        // Log (come facevi prima)
        foreach (var kv in list)
        {
            if (kv.Value)
                Debug.Log($"{kv.Key}: {kv.Value.name}");
            else
                Debug.LogWarning($"{kv.Key}: prefab non trovato in Resources");
        }

        // Base/body obbligatori
        if (list.TryGetValue("body", out var bodyPrefab) && bodyPrefab &&
            list.TryGetValue("base", out var basePrefab) && basePrefab)
        {
            composer.baseElement = Instantiate(basePrefab);
            composer.bodyElement = Instantiate(bodyPrefab);

            // Armature: istanzia solo se presenti
            if (list.TryGetValue("armorBackElement", out var back) && back)
                composer.armorBackElement = Instantiate(back);

            if (list.TryGetValue("armorFrontElement", out var front) && front)
                composer.armorFrontElement = Instantiate(front);

            if (list.TryGetValue("armorLeftElement", out var left) && left)
                composer.armorLeftElement = Instantiate(left);

            if (list.TryGetValue("armorRightElement", out var right) && right)
                composer.armorRightElement = Instantiate(right);

            composer.AlignComponents();
        }
        else
        {
            Debug.Log("VehicleLoader: Errore durante il caricamento del veicolo (base/body mancanti)");
        }
    }
}
