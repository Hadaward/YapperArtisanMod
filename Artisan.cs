using Artisan.Spells;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace Artisan
{
    [BepInPlugin("gamedroit.artisan", "Artisan", "1.0.1")]
    public sealed class ArtisanMod : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; private set; }

        private Harmony harmony;

        public event Action OnGameReady = delegate { };

        public static ConfigEntry<bool> EnableTeleBlastDamagePatch;
        public static ConfigEntry<bool> MakeTeleBlastDamageOtherPlayers;
        public static ConfigEntry<int> TeleBlastSpellDamage;

        public static ConfigEntry<bool> MakeAeroDamageOtherPlayers;
        public static ConfigEntry<bool> EnableAeroDamagePatch;
        public static ConfigEntry<int> AeroSpellDamage;

        public static ConfigEntry<bool> EnableAstralThornsSpell;
        public static ConfigEntry<float> AstralThornsTargetDistance;
        public static ConfigEntry<float> AstralThornsSphereCastRadius;

        public static ConfigEntry<bool> EnableExtendedInventorySlots;
        public static ConfigEntry<int> MaxInventorySlots;

        public static ConfigEntry<bool> EnableShowMonstersHealthBar;
        public static ConfigEntry<bool> EnableShowMonsterDamageIndicator;
        public static ConfigEntry<float> MonsterHealthBarExtraHeightOffset;

        public void Awake()
        {
            Logger = base.Logger;

            EnableAeroDamagePatch = Config.Bind("Aero", "Enable Aero Damage Patch", true, "Enable or disable the damage patch for Aero spell.");
            MakeAeroDamageOtherPlayers = Config.Bind("Aero", "Make Aero Damage Other Players", false, "Allow Aero spell to damage other players when the damage patch is enabled.");
            AeroSpellDamage = Config.Bind("Aero", "Aero Spell Damage", 2, "Amount of damage the Aero spell will deal when the damage patch is enabled.");

            EnableTeleBlastDamagePatch = Config.Bind("TeleBlast", "Enable TeleBlast Damage Patch", true, "Enable or disable the damage patch for TeleBlast spell.");
            MakeTeleBlastDamageOtherPlayers = Config.Bind("TeleBlast", "Make TeleBlast Damage Other Players", false, "Allow TeleBlast spell to damage other players when the damage patch is enabled.");
            TeleBlastSpellDamage = Config.Bind("TeleBlast", "TeleBlast Spell Damage", 5, "Amount of damage the TeleBlast spell will deal when the damage patch is enabled.");

            EnableAstralThornsSpell = Config.Bind("Astral Thorns", "Enable Astral Thorns Spell", false, "Enable or disable the Astral Thorns spell.");
            AstralThornsTargetDistance = Config.Bind("Astral Thorns", "Astral Thorns Target Distance", 20f, "Maximum distance for Astral Thorns to target enemies.");
            AstralThornsSphereCastRadius = Config.Bind("Astral Thorns", "Astral Thorns Sphere Cast Radius", 0.5f, "Radius of the sphere cast used by Astral Thorns to detect targets.");

            EnableExtendedInventorySlots = Config.Bind("Inventory", "Enable Extended Inventory Slots", true, "Enable or disable increasing the inventory from 3 to 6 slots.");
            MaxInventorySlots = Config.Bind("Inventory", "Max Inventory Slots", 6, "Maximum inventory slots. Vanilla is 3. Recommended value is 6.");

            EnableShowMonstersHealthBar = Config.Bind("UI", "Enable Show Monsters Health Bar", true, "Enable or disable showing monsters' health bars.");
            EnableShowMonsterDamageIndicator = Config.Bind("UI", "Enable Show Monster Damage Indicator", true, "Enable or disable showing damage indicators on monsters.");
            MonsterHealthBarExtraHeightOffset = Config.Bind("UI", "Monster Health Bar Extra Height Offset", 0.45f, "Additional height offset for monster health bars to prevent overlap with the monster's model.");

            harmony = new Harmony("gamedroit.artisan");

            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                foreach (MethodBase method in harmony.GetPatchedMethods())
                {
                    Logger.LogInfo($"Patched: {method.DeclaringType?.FullName}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Harmony patch failed: {ex}");
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            Logger.LogInfo("Artisan mod is loaded and initialized.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "NetworkDungeon")
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            OnGameReady?.Invoke();

            SpellRegistry.Initialize();
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}