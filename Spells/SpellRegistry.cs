using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YAPYAP;

namespace Artisan.Spells
{
    public static class SpellRegistry
    {
        internal static Dictionary<string, NetworkPuppetWandProp> RegisteredWands { get; private set; } = new Dictionary<string, NetworkPuppetWandProp>();

        internal static bool IsInitialized { get; private set; } = false;

        internal static void Initialize()
        {
            if (IsInitialized)
                return;

            ArtisanMod.Logger.LogInfo("Initializing SpellRegistry...");

            NetworkPuppetWandProp[] GameWands = Resources.FindObjectsOfTypeAll<NetworkPuppetWandProp>();

            foreach (NetworkPuppetWandProp wand in GameWands)
            {
                if (wand == null || string.IsNullOrEmpty(wand.name))
                {
                    ArtisanMod.Logger.LogWarning($"Encountered a NetworkPuppetWandProp with null or empty wandName. Skipping registration.");
                    continue;
                }

                if (!RegisteredWands.ContainsKey(wand.name))
                {
                    RegisteredWands.Add(wand.name, wand);
                }
            }

            ArtisanMod.Logger.LogInfo($"SpellRegistry is initialized.\n\tFound {RegisteredWands.Count} wands with the following names: {string.Join(", ", RegisteredWands.Keys)}");

            IsInitialized = true;
        }

        public static Spell GetSpellByVoiceCommandKey(string voiceCommandKey)
        {
            if (!IsInitialized)
            {
                ArtisanMod.Logger.LogWarning($"Tried to get spell '{voiceCommandKey}' before SpellRegistry was initialized. Make sure to call SpellRegistry.Initialize() during your mod's initialization phase.");
                return null;
            }

            foreach (NetworkPuppetWandProp wand in RegisteredWands.Values)
            {
                if (wand == null || wand.Spell == null)
                    continue;

                var wandSpell = wand.Spell;

                if (wandSpell is VoiceSpell voiceSpell)
                {
                    Spell[] spells = HarmonyUtil.GetFieldValue<Spell[]>(voiceSpell, "spells");

                    foreach (Spell spell in spells)
                    {
                        var name = spell.name ?? spell.SpellName;

                        if (string.Equals(spell.VoiceCommandKey, voiceCommandKey, StringComparison.OrdinalIgnoreCase))
                        {
                            return spell;
                        }
                    }
                } else if (wandSpell is Spell spell)
                {
                    if (string.Equals(wandSpell.VoiceCommandKey, voiceCommandKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return wandSpell;
                    }
                }
            }

            return null;
        }

        public static void Register<TSpell>(string wandName) where TSpell : Spell
        {
            Type spellType = typeof(TSpell);
            string spellName = spellType.Name;

            if (!IsInitialized)
            {
                ArtisanMod.Logger.LogWarning($"Tried to register spell '{spellName}' before SpellRegistry was initialized.");
                return;
            }

            if (!RegisteredWands.TryGetValue(wandName, out NetworkPuppetWandProp wandProp))
            {
                ArtisanMod.Logger.LogError($"Failed to register spell '{spellName}': No wand found with name '{wandName}'.");
                return;
            }

            if (!(wandProp.Spell is VoiceSpell voiceSpell))
            {
                ArtisanMod.Logger.LogWarning($"Failed to register spell '{spellName}': Wand '{wandName}' does not have a VoiceSpell component.");
                return;
            }

            GameObject spellHost = voiceSpell.gameObject;
            TSpell spell = spellHost.AddComponent<TSpell>();

            Spell[] spells = HarmonyUtil.GetFieldValue<Spell[]>(voiceSpell, "spells");

            if (spells == null)
            {
                ArtisanMod.Logger.LogError($"Failed to register spell '{spellName}': VoiceSpell.spells field is null.");
                DestroyIfPossible(spell);
                return;
            }

            Spell[] newSpells = spells.Concat(new Spell[] { spell }).ToArray();
            HarmonyUtil.SetFieldValue(voiceSpell, "spells", newSpells);

            ArtisanMod.Logger.LogInfo(
                $"Registered spell '{spellName}' with wand '{wandName}' on GameObject '{spellHost.name}'."
            );
        }

        private static void DestroyIfPossible(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            UnityEngine.Object.Destroy(obj);
        }
    }
}
