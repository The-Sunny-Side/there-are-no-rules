using System.Collections.Generic;
using System.Linq;
using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

// Server-only: istanzia bot ai punti di spawn liberi al momento dello start gara.
// Convenzione: gli umani occupano i primi N spawn point, i bot riempiono i restanti fino a targetRacerCount.
public class BotSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab del bot (variante del player con AIInputProvider + AIDriver + AIRespawner).")]
    [SerializeField] private GameObject botPrefab;
    [Tooltip("Config veicolo usata per i bot. Se non assegnata, i bot clonano la config del veicolo locale dell'host/server.")]
    [SerializeField] private TextAsset defaultBotConfig;
    [SerializeField] private PredictionManager predictionManager;

    [Header("Roads")]
    [Tooltip("Sequenza di RoadMeshGenerator che ogni bot dovra' seguire. Iniettata sull'AIDriver dopo lo spawn.")]
    [SerializeField] private List<RoadMeshGenerator> roadSequence = new();

    [Header("Settings")]
    [SerializeField] private int targetRacerCount = 8;
    [Tooltip("Se attivo, ogni bot riceve una config casuale (base + corpo + 1-4 armi). Altrimenti usa defaultBotConfig/host.")]
    [SerializeField] private bool randomizeBotConfig = true;

    public int TargetRacerCount => targetRacerCount;

    // Spawna i bot necessari per arrivare a targetRacerCount, posizionandoli ai spawn point successivi
    // a quelli occupati dagli umani.
    public void SpawnBotsForHumanCount(int humanCount)
    {
        if (!isServer) return;
        if (botPrefab == null)
        {
            Debug.LogWarning("[BotSpawner] botPrefab non assegnato.");
            return;
        }

        var spawns = RaceSpawnPoints.Instance;
        if (spawns == null || spawns.Count == 0)
        {
            Debug.LogWarning("[BotSpawner] RaceSpawnPoints non trovato o vuoto.");
            return;
        }

        if (!EnsurePredictionManager())
        {
            Debug.LogWarning("[BotSpawner] PredictionManager non trovato: impossibile spawnare bot predicted.");
            return;
        }

        int totalSpawns = spawns.Count;
        int botsToSpawn = Mathf.Min(targetRacerCount, totalSpawns) - humanCount;
        if (botsToSpawn <= 0) return;

        string fixedConfigJson = randomizeBotConfig ? null : ResolveBotConfigJson();
        if (!randomizeBotConfig && string.IsNullOrWhiteSpace(fixedConfigJson))
            Debug.LogWarning("[BotSpawner] Nessuna config veicolo disponibile per i bot: verranno spawnati ma resteranno vuoti.");

        for (int i = 0; i < botsToSpawn; i++)
        {
            int spawnIdx = humanCount + i;
            if (spawnIdx >= totalSpawns) break;

            Transform sp = spawns.GetAt(spawnIdx);
            if (sp == null) continue;

            PredictedObjectID? botId = predictionManager.hierarchy.Create(botPrefab, sp.position, sp.rotation);
            if (!predictionManager.hierarchy.TryGetGameObject(botId, out GameObject bot) || bot == null)
            {
                Debug.LogWarning("[BotSpawner] Spawn predicted bot fallito.");
                continue;
            }

            var identity = bot.GetComponent<PlayerIdentity>();
            if (identity != null)
                identity.SetBotIdentity(i + 1);

            string botConfigJson = randomizeBotConfig ? BuildRandomBotConfigJson() : fixedConfigJson;

            var vehicleConfig = bot.GetComponentInChildren<VehicleNetConfig>(true);
            if (vehicleConfig != null && !string.IsNullOrWhiteSpace(botConfigJson))
                vehicleConfig.SetServerConfigJson(botConfigJson);

            // Inietta la roadSequence prima dello spawn cosi' l'AIDriver e' subito pronto.
            var driver = bot.GetComponent<AIDriver>();
            if (driver != null)
                driver.Initialize(roadSequence);
        }
    }

    private string ResolveBotConfigJson()
    {
        if (defaultBotConfig != null && !string.IsNullOrWhiteSpace(defaultBotConfig.text))
            return defaultBotConfig.text;

        if (VehicleManager.Instance != null)
            return VehicleManager.Instance.GetVehicleJson();

        return null;
    }

    // Crea una config veicolo casuale valida: 1 base, 1 corpo e 1-4 armi in slot casuali.
    private string BuildRandomBotConfigJson()
    {
        var reg = GameConfig.VehiclePrefabRegistry;
        if (reg == null)
        {
            Debug.LogWarning("[BotSpawner] VehiclePrefabRegistry non disponibile: config bot casuale saltata.");
            return null;
        }

        var bases = reg.GetAllBases();
        var bodies = reg.GetAllBodies();
        if (bases == null || bases.Count == 0 || bodies == null || bodies.Count == 0)
        {
            Debug.LogWarning("[BotSpawner] Registry senza basi o corpi: config bot casuale saltata.");
            return null;
        }

        var vehicle = new Vehicle
        {
            baseElement = bases[Random.Range(0, bases.Count)].key,
            bodyElement = bodies[Random.Range(0, bodies.Count)].key,
            weaponFront = "empty",
            weaponLeft = "empty",
            weaponBack = "empty",
            weaponRight = "empty",
        };

        // Armi reali disponibili (escludo il segnaposto "empty").
        var realWeapons = reg.GetAllWeapons()?
            .Where(w => w != null && !string.IsNullOrEmpty(w.key) && w.key != "empty")
            .ToList();

        if (realWeapons != null && realWeapons.Count > 0)
        {
            // Slot mescolati: ne riempiamo da 1 a 4.
            var slots = new List<string> { "front", "left", "back", "right" };
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            int weaponCount = Random.Range(1, slots.Count + 1); // 1..4
            for (int i = 0; i < weaponCount; i++)
            {
                string weaponKey = realWeapons[Random.Range(0, realWeapons.Count)].key;
                switch (slots[i])
                {
                    case "front": vehicle.weaponFront = weaponKey; break;
                    case "left": vehicle.weaponLeft = weaponKey; break;
                    case "back": vehicle.weaponBack = weaponKey; break;
                    case "right": vehicle.weaponRight = weaponKey; break;
                }
            }
        }

        return JsonUtility.ToJson(vehicle);
    }

    private bool EnsurePredictionManager()
    {
        if (predictionManager != null && predictionManager.hierarchy != null)
            return true;

        if (PredictionManager.TryGetInstance(gameObject.scene.handle, out predictionManager) &&
            predictionManager != null &&
            predictionManager.hierarchy != null)
            return true;

        predictionManager = FindFirstObjectByType<PredictionManager>();
        return predictionManager != null && predictionManager.hierarchy != null;
    }
}
