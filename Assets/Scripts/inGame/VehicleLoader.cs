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
            list[VehicleElementsKeys.Base] = GameConfig.VehiclePrefabRegistry.GetBase(data.baseElement).element;

        if (!string.IsNullOrEmpty(data.bodyElement))
            list[VehicleElementsKeys.Body] = GameConfig.VehiclePrefabRegistry.GetBody(data.bodyElement).element;

        // weapons (opzionali)
        if (!string.IsNullOrEmpty(data.weaponBack))
            list[VehicleElementsKeys.WeaponBack] = GameConfig.VehiclePrefabRegistry.GetWeapon(data.weaponBack).element;

        if (!string.IsNullOrEmpty(data.weaponFront))
            list[VehicleElementsKeys.WeaponFront] = GameConfig.VehiclePrefabRegistry.GetWeapon(data.weaponFront).element;

        if (!string.IsNullOrEmpty(data.weaponLeft))
            list[VehicleElementsKeys.WeaponLeft] = GameConfig.VehiclePrefabRegistry.GetWeapon(data.weaponLeft).element;

        if (!string.IsNullOrEmpty(data.weaponRight))
            list[VehicleElementsKeys.WeaponRight] = GameConfig.VehiclePrefabRegistry.GetWeapon(data.weaponRight).element;


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
                if (list.TryGetValue(VehicleElementsKeys.WeaponBack, out var back) && back)
                    composer.weaponBackElement = Instantiate(back);

                if (list.TryGetValue(VehicleElementsKeys.WeaponFront, out var front) && front)
                    composer.weaponFrontElement = Instantiate(front);

                if (list.TryGetValue(VehicleElementsKeys.WeaponLeft, out var left) && left)
                    composer.weaponLeftElement = Instantiate(left);

                if (list.TryGetValue(VehicleElementsKeys.WeaponRight, out var right) && right)
                    composer.weaponRightElement = Instantiate(right);

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

