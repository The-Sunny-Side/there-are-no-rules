using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private string tableCollectionName = "ui";

    private TMP_Dropdown _dropdown;
    private List<Locale> _locales;
    private bool _initialized;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        _dropdown.onValueChanged.RemoveListener(OnLanguageSelected);
    }

    private void Start()
    {
        _locales = LocalizationSettings.AvailableLocales.Locales;

        InitializeOptions();
        UpdateTexts();
        UpdateSelection();
    }

    // =========================
    // INITIALIZATION
    // =========================

    private void InitializeOptions()
    {
        if (_initialized)
            return;

        _dropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var _ in _locales)
            options.Add(new TMP_Dropdown.OptionData(""));

        _dropdown.AddOptions(options);

        _dropdown.onValueChanged.AddListener(OnLanguageSelected);

        _initialized = true;
    }

    // =========================
    // UPDATE
    // =========================

    private void UpdateTexts()
    {
        for (int i = 0; i < _locales.Count; i++)
        {
            _dropdown.options[i].text = GetTranslatedLocaleName(_locales[i]);
        }

        _dropdown.RefreshShownValue();
    }

    private void UpdateSelection()
    {
        int index = _locales.IndexOf(LocalizationSettings.SelectedLocale);

        if (index >= 0)
            _dropdown.SetValueWithoutNotify(index);

        _dropdown.RefreshShownValue();
    }

    private void OnLocaleChanged(Locale locale)
    {
        UpdateTexts();
        UpdateSelection();
    }

    private void OnLanguageSelected(int index)
    {
        if (index >= 0 && index < _locales.Count)
            LocalizationSettings.SelectedLocale = _locales[index];
    }

    // =========================
    // HELPERS
    // =========================

    private string GetTranslatedLocaleName(Locale locale)
    {
        var localizedString = new LocalizedString
        {
            TableReference = tableCollectionName,
            TableEntryReference = locale.Identifier.Code,
        };

        return localizedString.GetLocalizedString();
    }
}