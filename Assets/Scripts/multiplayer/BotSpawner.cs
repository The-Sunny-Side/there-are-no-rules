using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Server-only: istanzia bot ai punti di spawn liberi al momento dello start gara.
// Convenzione: gli umani occupano i primi N spawn point, i bot riempiono i restanti fino a targetRacerCount.
public class BotSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab del bot (variante del player con AIInputProvider + AIDriver + AIRespawner).")]
    [SerializeField] private GameObject botPrefab;

    [Header("Roads")]
    [Tooltip("Sequenza di RoadMeshGenerator che ogni bot dovrà seguire. Iniettata sull'AIDriver dopo lo spawn.")]
    [SerializeField] private List<RoadMeshGenerator> roadSequence = new();

    [Header("Settings")]
    [SerializeField] private int targetRacerCount = 8;

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

        int totalSpawns = spawns.Count;
        int botsToSpawn = Mathf.Min(targetRacerCount, totalSpawns) - humanCount;
        if (botsToSpawn <= 0) return;

        for (int i = 0; i < botsToSpawn; i++)
        {
            int spawnIdx = humanCount + i;
            if (spawnIdx >= totalSpawns) break;

            Transform sp = spawns.GetAt(spawnIdx);
            if (sp == null) continue;

            GameObject bot = Instantiate(botPrefab, sp.position, sp.rotation);

            // Inietta la roadSequence prima dello spawn così l'AIDriver è subito pronto.
            var driver = bot.GetComponent<AIDriver>();
            if (driver != null)
                driver.Initialize(roadSequence);

            // Spawn networkato — server-owned, nessun client owner.
            networkManager.Spawn(bot);
        }
    }
}
