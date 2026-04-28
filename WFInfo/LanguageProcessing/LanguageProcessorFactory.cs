using System;
using System.Collections.Generic;
using System.Linq;
using WFInfo.Settings;

namespace WFInfo.LanguageProcessing
{
    /// <summary>
    /// Factory class for managing language processors
    /// Provides centralized access to language-specific OCR text processing
    /// </summary>
    public class LanguageProcessorFactory
    {
        private static readonly Dictionary<string, LanguageProcessor> _processors = new Dictionary<string, LanguageProcessor>();
        private static readonly object _lock = new object();
        private static IReadOnlyApplicationSettings _settings;

        /// <summary>
        /// Initializes the factory with application settings
        /// </summary>
        /// <param name="settings">Application settings</param>
        public static void Initialize(IReadOnlyApplicationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
        }

        /// <summary>
        /// Gets the language processor for the specified locale
        /// </summary>
        /// <param name="locale">Locale code (e.g., "en", "ko", "ja")</param>
        /// <returns>Language processor for the locale</returns>
        public static LanguageProcessor GetProcessor(string locale)
        {
            if (string.IsNullOrEmpty(locale))
                locale = "en";

            lock (_lock)
            {
                if (_processors.TryGetValue(locale, out LanguageProcessor processor))
                    return processor;

                // Create new processor if not exists
                processor = CreateProcessor(locale);
                _processors[locale] = processor;
                return processor;
            }
        }

        /// <summary>
        /// Gets the current language processor based on settings
        /// </summary>
        /// <returns>Current language processor</returns>
        public static LanguageProcessor GetCurrentProcessor()
        {
            if (_settings == null)
                throw new InvalidOperationException("Factory not initialized. Call Initialize() first.");

            return GetProcessor(_settings.Locale);
        }

        /// <summary>
        /// Gets all supported locales
        /// </summary>
        /// <returns>Array of supported locale codes</returns>
        public static string[] GetSupportedLocales()
        {
            return new[]
            {
                "en",        // English
                "ko",        // Korean
                "ja",        // Japanese
                "zh-hans",   // Simplified Chinese
                "zh-hant",   // Traditional Chinese
                "th",        // Thai
                "ru",        // Russian
                "uk",        // Ukrainian
                "tr",        // Turkish
                "pl",        // Polish
                "fr",        // French
                "de",        // German
                "es",        // Spanish
                "pt",        // Portuguese
                "it"         // Italian
            };
        }

        /// <summary>
        /// Creates a language processor for the specified locale
        /// </summary>
        /// <param name="locale">Locale code</param>
        /// <returns>New language processor instance</returns>
        private static LanguageProcessor CreateProcessor(string locale)
        {
            if (_settings == null)
                throw new InvalidOperationException("Factory not initialized. Call Initialize() first.");

            locale = locale.ToLowerInvariant();
            switch (locale)
            {
                case "en":
                    return new EnglishLanguageProcessor(_settings);
                case "ko":
                    return new KoreanLanguageProcessor(_settings);
                case "ja":
                    return new JapaneseLanguageProcessor(_settings);
                case "zh-hans":
                    return new SimplifiedChineseLanguageProcessor(_settings);
                case "zh-hant":
                    return new TraditionalChineseLanguageProcessor(_settings);
                case "th":
                    return new ThaiLanguageProcessor(_settings);
                case "ru":
                    return new RussianLanguageProcessor(_settings);
                case "uk":
                    return new UkrainianLanguageProcessor(_settings);
                case "tr":
                    return new TurkishLanguageProcessor(_settings);
                case "pl":
                    return new PolishLanguageProcessor(_settings);
                case "fr":
                    return new FrenchLanguageProcessor(_settings);
                case "de":
                    return new GermanLanguageProcessor(_settings);
                case "es":
                    return new SpanishLanguageProcessor(_settings);
                case "pt":
                    return new PortugueseLanguageProcessor(_settings);
                case "it":
                    return new ItalianLanguageProcessor(_settings);
                default:
                    return new EnglishLanguageProcessor(_settings); // Default to English
            }
        }

        /// <summary>
        /// Clears all cached processors
        /// Useful for testing or when settings change
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _processors.Clear();
            }
        }

        /// <summary>
        /// Checks if a locale is supported
        /// </summary>
        /// <param name="locale">Locale code to check</param>
        /// <returns>True if supported, false otherwise</returns>
        public static bool IsLocaleSupported(string locale)
        {
            if (string.IsNullOrEmpty(locale))
                return false;

            return GetSupportedLocales().Contains(locale, StringComparer.OrdinalIgnoreCase);
        }
    }
}
