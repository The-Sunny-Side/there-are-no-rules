using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[Serializable]
public class LocaleIconEntry
{
    public string localeCode; // "it", "en", "es"
    public Sprite icon;
}

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private List<LocaleIconEntry> localeIcons;
    [SerializeField] private Image selectedLanguageIcon;

    private List<Locale> availableLocales;
    private int selectedLanguage;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        StartCoroutine(InitLocalization());
    }

    private IEnumerator InitLocalization()
    {
        yield return LocalizationSettings.InitializationOperation;

        availableLocales = LocalizationSettings.AvailableLocales.Locales;

        string currentCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "";
        string currentPrefix = currentCode.Substring(0, Math.Min(2, currentCode.Length));

        int found = availableLocales.FindIndex(l =>
            l.Identifier.Code.StartsWith(currentPrefix, StringComparison.OrdinalIgnoreCase));

        if (found < 0 && availableLocales.Count > 0)
            LocalizationSettings.SelectedLocale = availableLocales[0];

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (availableLocales == null || availableLocales.Count == 0) return;

        string currentCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "";
        string currentPrefix = currentCode.Substring(0, Math.Min(2, currentCode.Length));

        int found = availableLocales.FindIndex(l =>
            l.Identifier.Code.StartsWith(currentPrefix, StringComparison.OrdinalIgnoreCase));

        selectedLanguage = found >= 0 ? found : 0;

        string selectedCode = availableLocales[selectedLanguage].Identifier.Code;
        var entry = localeIcons.Find(e =>
            selectedCode.StartsWith(e.localeCode, StringComparison.OrdinalIgnoreCase) ||
            e.localeCode.StartsWith(selectedCode, StringComparison.OrdinalIgnoreCase));

        if (entry != null)
            selectedLanguageIcon.sprite = entry.icon;
    }

    private void OnLocaleChanged(Locale locale) => UpdateSelection();

    public void NextLanguage()
    {
        selectedLanguage = (selectedLanguage + 1) % availableLocales.Count;
        ApplySelection();
    }

    public void PreviousLanguage()
    {
        selectedLanguage = (selectedLanguage - 1 + availableLocales.Count) % availableLocales.Count;
        ApplySelection();
    }

    private void ApplySelection()
    {
        LocalizationSettings.SelectedLocale = availableLocales[selectedLanguage];
        GameConfig.Data.language = selectedLanguage;
        GameConfig.Save();
        UpdateSelection();
    }
}