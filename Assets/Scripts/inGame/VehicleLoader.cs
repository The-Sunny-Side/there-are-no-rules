using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Non legge piu' dal file in Awake.
/// Viene chiamato dalla rete (PurrNet) passando la config del veicolo del player.
/// </summary>
public class VehicleLoader : MonoBehaviour
{
    [SerializeField] private Composer composer;
    private string _currentJson;

    private void OnEnable()
    {
        // Force a rebuild after object re-enable/replay.
        _currentJson = null;
        EnsureComposer();
    }

    /// <summary>
    /// Applica una configurazione veicolo ricevuta via rete.
    /// json deve essere JsonUtility.ToJson(new Vehicle(...))
    /// </summary>
    public void ApplyConfigJson(string json)
    {
        if (_currentJson == json)
            return;

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
            list[VehicleElementsKeys.Base] = Resources.Load<GameObject>("baseElements/" + data.baseElement);

        if (!string.IsNullOrEmpty(data.bodyElement))
            list[VehicleElementsKeys.Body] = Resources.Load<GameObject>("bodyElements/" + data.bodyElement);

        // weapons (opzionali)
        if (!string.IsNullOrEmpty(data.weaponBack))
            list[VehicleElementsKeys.WeaponBack] = Resources.Load<GameObject>("weapons/" + data.weaponBack);

        if (!string.IsNullOrEmpty(data.weaponFront))
            list[VehicleElementsKeys.WeaponFront] = Resources.Load<GameObject>("weapons/" + data.weaponFront);

        if (!string.IsNullOrEmpty(data.weaponLeft))
            list[VehicleElementsKeys.WeaponLeft] = Resources.Load<GameObject>("weapons/" + data.weaponLeft);

        if (!string.IsNullOrEmpty(data.weaponRight))
            list[VehicleElementsKeys.WeaponRight] = Resources.Load<GameObject>("weapons/" + data.weaponRight);

        // Log (come facevi prima)
        foreach (var kv in list)
        {
            if (kv.Value)
                Debug.Log($"{kv.Key}: {kv.Value.name}");
            else
                Debug.LogWarning($"{kv.Key}: prefab non trovato in Resources");
        }

        // Base/body obbligatori
        if (list.TryGetValue(VehicleElementsKeys.Body, out var bodyPrefab) && bodyPrefab &&
            list.TryGetValue(VehicleElementsKeys.Base, out var basePrefab) && basePrefab)
        {
            if (!EnsureComposer())
            {
                Debug.LogWarning("VehicleLoader: composer non assegnato");
                return;
            }

            try
            {
                CleanupPreviousComposition();

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
                _currentJson = json;
            }
            catch (Exception ex)
            {
                Debug.LogError($"VehicleLoader: errore durante composizione veicolo: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("VehicleLoader: Errore durante il caricamento del veicolo (base/body mancanti)");
        }
    }

    private void CleanupPreviousComposition()
    {
        if (!EnsureComposer())
            return;

        composer.ClearRuntimeParts();
    }

    private bool EnsureComposer()
    {
        if (composer != null)
            return true;

        composer = GetComponentInChildren<Composer>(true);
        return composer != null;
    }
}

