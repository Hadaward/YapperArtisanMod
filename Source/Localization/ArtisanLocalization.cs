using Artisan.Shared.Reflection;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using YAPYAP;

namespace Artisan.Localization
{
    public static class ArtisanLocalization
    {
        internal static IList VoskTranslators;
        internal static object DefaultVoskTranslator;
        internal static VoskLocalisation[] VoskLocalisations;
        internal static bool IsVoskLoaded = false;

        internal static Dictionary<SystemLanguage, object> QueuedTranslations = new Dictionary<SystemLanguage, object>();

        internal static void ProcessQueuedTranslations()
        {
            if (!IsVoskLoaded)
                return;

            foreach (KeyValuePair<SystemLanguage, object> kvp in QueuedTranslations)
            {
                if (kvp.Value is ArtisanVoiceCommand[] voiceCommands)
                {
                    AddVoiceCommand(kvp.Key, voiceCommands);
                }
            }

            QueuedTranslations.Clear();
        }

        public static void AddVoiceCommand(SystemLanguage language, ArtisanVoiceCommand[] voiceCommands)
        {
            if (!IsVoskLoaded)
            {
                ArtisanMod.Logger.LogWarning($"Vosk is not loaded yet. Queuing {voiceCommands.Length} voice commands for language {language} to be added later.");
                QueuedTranslations[language] = voiceCommands;
                return;
            }

            int translatorIndex = GetVoskTranslatorIndex(language);
            object translator;

            if (translatorIndex != -1)
            {
                translator = VoskTranslators[translatorIndex];
            }
            else
            {
                translator = DefaultVoskTranslator;
            }

            if (translator == null)
            {
                ArtisanMod.Logger.LogError($"Failed to find a Vosk translator for language {language}.");
                return;
            }

            Dictionary<string, string[]> commands = HarmonyUtil.GetFieldValue<Dictionary<string, string[]>>(translator, "_commands");
            string[] grammar = HarmonyUtil.GetFieldValue<string[]>(translator, "_grammar");

            if (commands == null || grammar == null)
            {
                ArtisanMod.Logger.LogError($"Failed to access the commands or grammar of the Vosk translator for language {language}.");
                return;
            }

            HashSet<string> newGrammar = new HashSet<string>(grammar);

            foreach (ArtisanVoiceCommand voiceCommand in voiceCommands)
            {
                if (commands.ContainsKey(voiceCommand.Key))
                {
                    if (voiceCommand.OverwriteExisting)
                    {
                        ArtisanMod.Logger.LogWarning($"Overwriting existing command for key '{voiceCommand.Key}' in language {language}.");
                    }
                    else
                    {
                        ArtisanMod.Logger.LogWarning($"Command for key '{voiceCommand.Key}' already exists in language {language}. Use overwrite=true to replace it.");
                        continue;
                    }
                }

                string[] commandWords = ToCommandWords(voiceCommand.Command);

                if (commandWords.Length == 0)
                {
                    ArtisanMod.Logger.LogWarning($"Command '{voiceCommand.Command}' for key '{voiceCommand.Key}' in language {language} does not contain any valid words.");
                    continue;
                }

                commands[voiceCommand.Key] = commandWords;

                foreach (string word in commandWords)
                {
                    if (!newGrammar.Contains(word))
                    {
                        newGrammar.Add(word);
                    }
                }
            }

            HarmonyUtil.SetFieldValue(translator, "_grammar", newGrammar.ToArray());
            ArtisanMod.Logger.LogInfo($"Added {voiceCommands.Length} voice commands to language {language}. Total grammar size is now {newGrammar.Count}.");
        }

        private static string[] ToCommandWords(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new string[0];

            return value
                .Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim())
                .Where(word => !string.IsNullOrEmpty(word))
                .Select(word => word.ToUpperInvariant())
                .ToArray();
        }

        private static int GetVoskTranslatorIndex(SystemLanguage language)
        {
            string systemCulture = CultureInfo.CurrentCulture.Name;
            int num = VoskLocalisations.ToList<VoskLocalisation>().FindIndex((VoskLocalisation l) => l.Language == language && l.CultureCode == systemCulture && !l.IsOptional);
            if (num == -1)
            {
                num = VoskLocalisations.ToList<VoskLocalisation>().FindIndex((VoskLocalisation l) => l.Language == language && !l.IsOptional);
            }
            if (num == -1)
            {
                num = VoskLocalisations.ToList<VoskLocalisation>().FindIndex((VoskLocalisation l) => l.Language == language);
            }

            return num;
        }
    }

    [HarmonyPatch(typeof(VoiceManager), "LoadLocalisationData")]
    public static class VoiceLocalizationPatches
    {
        [HarmonyPostfix]
        private static void Postfix(VoiceManager __instance)
        {
            IList translators = HarmonyUtil.GetFieldValue<IList>(__instance, "_voskTranslators");
            object defaultTranslator = HarmonyUtil.GetFieldValue<object>(__instance, "_defaultTranslator");

            VoskLocalisation[] localizations = HarmonyUtil.GetFieldValue<VoskLocalisation[]>(__instance, "_voskLocalisations");

            ArtisanLocalization.VoskTranslators = translators;
            ArtisanLocalization.VoskLocalisations = localizations;
            ArtisanLocalization.DefaultVoskTranslator = defaultTranslator;

            ArtisanMod.Logger.LogInfo($"Loaded {translators.Count} Vosk translators and {localizations.Length} Vosk localisations");

            ArtisanLocalization.IsVoskLoaded = true;
            ArtisanLocalization.ProcessQueuedTranslations();
        }
    }
}
