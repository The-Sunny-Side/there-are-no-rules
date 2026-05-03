using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[System.Serializable]
public class GameConfigData
{
    public float volume = 0.3f;
    public int language = 0;
    public bool audioEnabled = true;
    public string name;
    public bool showTutorial = true;
}

public class GameConfig : MonoBehaviour
{
    public static UiConfig UiConfig { get; private set; }
    
    public static VehiclePrefabRegistry VehiclePrefabRegistry { get; private set; }

    public static GameConfigData Data => _data ??= LoadOrCreate();
    private static GameConfigData _data;

    [SerializeField] private UiConfig uiConfig;
    [SerializeField] private VehiclePrefabRegistry vehiclePrefabRegistry;

    private void Awake()
    {
        if (UiConfig == null && VehiclePrefabRegistry == null)
        {
            UiConfig = uiConfig;
            VehiclePrefabRegistry = vehiclePrefabRegistry;

            StartCoroutine(InitLocalization());
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator InitLocalization()
    {
        yield return LocalizationSettings.InitializationOperation;

        List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
        if (Data.language >= 0 && Data.language < locales.Count)
            LocalizationSettings.SelectedLocale = locales[Data.language];
    }

    private static GameConfigData LoadOrCreate()
    {
        string path = Path.Combine(Application.persistentDataPath, "game_config.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameConfigData>(json);
        }

        GameConfigData defaults = new GameConfigData();
        Save(defaults);
        return defaults;
    }

    public static void Save(GameConfigData newData=null)
    {
        string path = Path.Combine(Application.persistentDataPath, "game_config.json");
        File.WriteAllText(path, JsonUtility.ToJson(newData?? Data, true));
    }

    public GameConfigData GetConfigData()
    {
        return Data;
    }
}