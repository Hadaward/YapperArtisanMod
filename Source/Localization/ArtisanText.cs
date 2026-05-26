using System.Collections.Generic;
using UnityEngine;
using YAPYAP;

namespace Artisan.Localization
{
    public static class ArtisanText
    {
        private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> Translations =
            new Dictionary<SystemLanguage, Dictionary<string, string>>();

        public static void Add(SystemLanguage language, string key, string value)
        {
            if (!Translations.TryGetValue(language, out Dictionary<string, string> values))
            {
                values = new Dictionary<string, string>();
                Translations[language] = values;
            }

            values[key] = value;
        }

        public static string Translate(string key)
        {
            SystemLanguage language = SystemLanguage.English;

            LocalisationManager manager;
            if (Service.Get(out manager) && manager.CurrentTranslator != null)
                language = manager.CurrentTranslator.Language;

            if (Translations.TryGetValue(language, out Dictionary<string, string> values) &&
                values.TryGetValue(key, out string text))
                return text;

            if (Translations.TryGetValue(SystemLanguage.English, out Dictionary<string, string> fallback) &&
                fallback.TryGetValue(key, out string fallbackText))
                return fallbackText;

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Translate(key), args);
        }
    }
}
