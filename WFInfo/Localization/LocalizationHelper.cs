using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WFInfo.Settings;

namespace WFInfo.Localization
{
    /// <summary>
    /// Provides access to localized in-game item names.
    /// Loads translation data from fallback JSON files (en → locale).
    /// </summary>
    public static class LocalizationHelper
    {
        /// <summary>
        /// English is always the base locale.
        /// </summary>
        private const string BaseLocale = "en";

        /// <summary>
        /// Reference to the global application settings.
        /// </summary>
        private static IReadOnlyApplicationSettings _settings;

        /// <summary>
        /// Paths to localization fallback files per locale (e.g., "de" → "fallback_names_de.json").
        /// </summary>
        private static Dictionary<string, string> _wfmItemsFallbackPaths;

        /// <summary>
        /// Cached translation dictionaries per locale (en → localized).
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> _translations =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes the localization system.
        /// Must be called once during startup.
        /// </summary>
        /// <param name="settings">Global application settings reference.</param>
        /// <param name="fallbackPaths">Dictionary of locale → fallback file paths.</param>
        public static void Init(IReadOnlyApplicationSettings settings, Dictionary<string, string> fallbackPaths)
        {
            _settings = settings;
            _wfmItemsFallbackPaths = fallbackPaths;
        }

        /// <summary>
        /// Returns the localized version of a given English item name.
        /// Automatically loads the current locale file if not already cached.
        /// </summary>
        /// <param name="item">English name of the item.</param>
        /// <returns>Localized item name, or original English name if not found.</returns>
        public static string GetLocalizationFromItem(string item)
        {
            // Skip if input is invalid or locale is English
            if (string.IsNullOrEmpty(item) ||
                _settings.Locale.Equals(BaseLocale, StringComparison.OrdinalIgnoreCase))
                return item;

            // Load translations if not yet available
            if (!_translations.ContainsKey(_settings.Locale) || _translations[_settings.Locale].Count == 0)
                LoadLocale();

            // Try to find localized version
            if (_translations[_settings.Locale].TryGetValue(item, out var localized))
                return localized;

            // Fallback to English
            return item;
        }

        /// <summary>
        /// Loads all translations for the current locale from its fallback file.
        /// Builds an in-memory dictionary mapping English → localized.
        /// </summary>
        private static void LoadLocale()
        {
            // Validate file path
            if (!_wfmItemsFallbackPaths.TryGetValue(_settings.Locale, out var filePath) || !File.Exists(filePath))
            {
                Debug.WriteLine($"[Localization] File for locale '{_settings.Locale}' not found.");
                _translations[_settings.Locale] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            // Create or reset dictionary
            _translations[_settings.Locale] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var sr = new StreamReader(filePath))
                using (var reader = new JsonTextReader(sr))
                {
                    string currentLang = null;
                    string en = null;
                    string localized = null;
                    bool inName = false;

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            string prop = (string)reader.Value;
                            if (prop == "en") currentLang = "en";
                            else if (prop == _settings.Locale) currentLang = _settings.Locale;
                            else if (prop == "name") inName = true;
                            else inName = false;
                        }
                        else if (reader.TokenType == JsonToken.String && inName)
                        {
                            string val = (string)reader.Value;

                            // Capture English and localized names
                            if (currentLang == "en")
                                en = val;
                            else if (currentLang == _settings.Locale)
                                localized = val;

                            // Once both are captured, store and reset
                            if (en != null && localized != null)
                            {
                                if (!_translations[_settings.Locale].ContainsKey(en))
                                    _translations[_settings.Locale].Add(en, localized);

                                en = localized = null;
                            }

                            inName = false;
                        }
                    }
                }

                Debug.WriteLine($"[Localization] Loaded {_translations[_settings.Locale].Count} entries for '{_settings.Locale}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Localization] Failed to load locale '{_settings.Locale}': {ex.Message}");
                _translations[_settings.Locale] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
