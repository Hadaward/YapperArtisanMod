using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YAPYAP;

namespace Artisan.Features.Inventory
{
    [HarmonyPatch(typeof(UISettings))]
    public static class ArtisanInventoryHotkeySettingsPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void AfterAwake(UISettings __instance)
        {
            ArtisanInventoryHotkeyManager.Install(__instance);
        }

        [HarmonyPatch("OnDisable")]
        [HarmonyPostfix]
        private static void AfterOnDisable()
        {
            ArtisanInventoryHotkeyManager.SaveOverrides();
        }
    }

    [HarmonyPatch(typeof(UISettingRebind), "UpdateBindingDisplay")]
    public static class ArtisanInventoryHotkeyRebindDisplayPatch
    {
        private static bool Prefix(UISettingRebind __instance)
        {
            return !ArtisanInventoryHotkeyManager.TryUpdateArtisanBindingDisplay(__instance);
        }
    }

    [HarmonyPatch(typeof(UISettingRebind), "Localise")]
    public static class ArtisanInventoryHotkeyRebindLocalisePatch
    {
        private static void Postfix(UISettingRebind __instance)
        {
            ArtisanInventoryHotkeyManager.RefreshRebindLabel(__instance);
        }
    }

    public static class ArtisanInventoryHotkeyManager
    {
        private const string ActionMapName = "Artisan Inventory";
        private const string OverridePrefsKey = "artisan_inventory_hotkey_rebinds";
        private const string HotkeyObjectPrefix = "ArtisanInventorySlotHotkey_";
        private const string KeyboardContentPath = "KeyboardScrollView/Viewport/Content";

        private static readonly Dictionary<int, InputAction> SlotActions = new Dictionary<int, InputAction>();
        private static readonly HashSet<int> InstalledSettingsIds = new HashSet<int>();

        private static InputActionAsset inputActionAsset;
        private static InputActionMap actionMap;

        public static void Install(UISettings settings)
        {
            if (settings == null)
                return;

            inputActionAsset = AccessTools.Field(typeof(UISettings), "inputActionAsset")
                ?.GetValue(settings) as InputActionAsset;

            if (inputActionAsset == null)
            {
                ArtisanMod.Logger.LogWarning("Unable to install Artisan inventory hotkeys because UISettings.inputActionAsset was not found.");
                return;
            }

            EnsureActions();
            LoadOverrides();
            AddControlsUi(settings);
        }

        public static InputAction GetSlotAction(int slotIndex)
        {
            InputAction action;

            return SlotActions.TryGetValue(slotIndex, out action)
                ? action
                : null;
        }

        public static void SaveOverrides()
        {
            if (inputActionAsset == null)
                return;

            PlayerPrefs.SetString(OverridePrefsKey, inputActionAsset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public static void RefreshRebindLabel(UISettingRebind rebind)
        {
            if (!TryGetArtisanSlotNumber(rebind, out int slotNumber))
                return;

            SetRebindLabel(rebind, "Slot " + slotNumber);
        }

        public static bool TryUpdateArtisanBindingDisplay(UISettingRebind rebind)
        {
            if (!TryGetArtisanSlotNumber(rebind, out int slotNumber))
                return false;

            InputAction action = GetSlotAction(slotNumber - 1);

            if (action == null || action.bindings.Count == 0)
                return true;

            SetRebindLabel(rebind, "Slot " + slotNumber);
            SetInputText(rebind, GetDisplayText(action));
            SetResetButtonState(rebind, action);

            return true;
        }

        private static void LoadOverrides()
        {
            string json = PlayerPrefs.GetString(OverridePrefsKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return;

            inputActionAsset.LoadBindingOverridesFromJson(json, true);
        }

        private static void EnsureActions()
        {
            int maxSlots = InventorySlotLimitPatch.GetMaxInventorySlots();

            bool wasEnabled = inputActionAsset.enabled;

            if (wasEnabled)
                inputActionAsset.Disable();

            try
            {
                actionMap = inputActionAsset.FindActionMap(ActionMapName, false);

                if (actionMap == null)
                {
                    actionMap = new InputActionMap(ActionMapName);
                    AddActionMapToAsset(inputActionAsset, actionMap);
                }

                for (int slotIndex = 3; slotIndex < maxSlots; slotIndex++)
                {
                    int slotNumber = slotIndex + 1;
                    string actionName = GetActionName(slotIndex);

                    InputAction action = actionMap.FindAction(actionName, false);

                    if (action == null)
                    {
                        action = actionMap.AddAction(actionName, InputActionType.Button);

                        string defaultBindingPath = GetDefaultBindingPath(slotNumber);

                        if (!string.IsNullOrEmpty(defaultBindingPath))
                            action.AddBinding(defaultBindingPath);
                    }
                    else if (action.bindings.Count == 0)
                    {
                        string defaultBindingPath = GetDefaultBindingPath(slotNumber);

                        if (!string.IsNullOrEmpty(defaultBindingPath))
                            action.AddBinding(defaultBindingPath);
                    }

                    SlotActions[slotIndex] = action;
                }
            }
            finally
            {
                if (wasEnabled)
                    inputActionAsset.Enable();

                foreach (InputAction action in SlotActions.Values)
                {
                    if (action != null && !action.enabled)
                        action.Enable();
                }
            }
        }

        private static void AddActionMapToAsset(InputActionAsset asset, InputActionMap map)
        {
            InputActionMap[] existingMaps = AccessTools.Field(typeof(InputActionAsset), "m_ActionMaps")
                ?.GetValue(asset) as InputActionMap[];

            if (existingMaps == null)
                existingMaps = new InputActionMap[0];

            InputActionMap[] newMaps = new InputActionMap[existingMaps.Length + 1];

            Array.Copy(existingMaps, newMaps, existingMaps.Length);
            newMaps[newMaps.Length - 1] = map;

            AccessTools.Field(typeof(InputActionAsset), "m_ActionMaps")?.SetValue(asset, newMaps);
            AccessTools.Field(typeof(InputActionMap), "m_Asset")?.SetValue(map, asset);
        }

        private static string GetActionName(int slotIndex)
        {
            return "Inventory Slot " + (slotIndex + 1);
        }

        private static string GetDefaultBindingPath(int slotNumber)
        {
            if (slotNumber >= 4 && slotNumber <= 9)
                return "<Keyboard>/" + slotNumber;

            return string.Empty;
        }

        private static void AddControlsUi(UISettings settings)
        {
            int settingsId = settings.GetInstanceID();

            if (InstalledSettingsIds.Contains(settingsId))
                return;

            InstalledSettingsIds.Add(settingsId);

            Transform content = FindDescendantPath(settings.transform, KeyboardContentPath);

            if (content == null)
            {
                ArtisanMod.Logger.LogWarning("Unable to find KeyboardScrollView/Viewport/Content.");
                return;
            }

            UISettingRebind template = FindBestTemplate(content);

            if (template == null)
            {
                ArtisanMod.Logger.LogWarning("Unable to find UISettingRebind template.");
                return;
            }

            RemovePreviousHotkeyUi(content);

            foreach (KeyValuePair<int, InputAction> pair in SlotActions)
                CreateRebindSetting(content, template, pair.Key, pair.Value);

            RectTransform contentRect = content.GetComponent<RectTransform>();

            if (contentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private static UISettingRebind FindBestTemplate(Transform content)
        {
            UISettingRebind[] rebinds = content.GetComponentsInChildren<UISettingRebind>(true);

            for (int i = 0; i < rebinds.Length; i++)
            {
                if (HasText(rebinds[i], "Slot 1"))
                    return rebinds[i];
            }

            return rebinds.Length > 0 ? rebinds[0] : null;
        }

        private static bool HasText(UISettingRebind rebind, string expectedText)
        {
            TMP_Text[] texts = rebind.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].text == expectedText)
                    return true;
            }

            return false;
        }

        private static void CreateRebindSetting(
            Transform parent,
            UISettingRebind template,
            int slotIndex,
            InputAction action)
        {
            if (action == null || action.bindings.Count == 0)
                return;

            bool templateWasActive = template.gameObject.activeSelf;
            template.gameObject.SetActive(false);

            UISettingRebind rebind = UnityEngine.Object.Instantiate(template, parent);
            int slotNumber = slotIndex + 1;

            rebind.gameObject.name = HotkeyObjectPrefix + slotNumber;

            InputActionReference reference = ScriptableObject.CreateInstance<InputActionReference>();
            reference.Set(action);

            AccessTools.Field(typeof(UISettingRebind), "m_Action")?.SetValue(rebind, reference);
            AccessTools.Field(typeof(UISettingRebind), "m_BindingId")?.SetValue(
                rebind,
                action.bindings[0].id.ToString()
            );

            template.gameObject.SetActive(templateWasActive);
            rebind.gameObject.SetActive(true);

            SetRebindLabel(rebind, "Slot " + slotNumber);
            SetInputText(rebind, GetDisplayText(action));
            SetResetButtonState(rebind, action);
        }

        private static bool TryGetArtisanSlotNumber(UISettingRebind rebind, out int slotNumber)
        {
            slotNumber = -1;

            if (rebind == null)
                return false;

            string name = rebind.gameObject.name;

            if (!name.StartsWith(HotkeyObjectPrefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(name.Substring(HotkeyObjectPrefix.Length), out slotNumber);
        }

        private static string GetDisplayText(InputAction action)
        {
            if (action == null || action.bindings.Count == 0)
                return "Sem atalho";

            string path = action.bindings[0].effectivePath;

            if (string.IsNullOrEmpty(path))
                return "Sem atalho";

            if (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
            {
                string key = path.Substring("<Keyboard>/".Length);

                if (key.Length == 1 && char.IsDigit(key[0]))
                    return key;

                if (key.StartsWith("digit", StringComparison.OrdinalIgnoreCase))
                    return key.Substring("digit".Length);

                return key.ToUpperInvariant();
            }

            return action.GetBindingDisplayString(0);
        }

        private static void SetInputText(UISettingRebind rebind, string text)
        {
            object input = AccessTools.Field(typeof(UISettingRebind), "input")?.GetValue(rebind);

            if (input == null)
                return;

            AccessTools.Method(input.GetType(), "SetKey", new[] { typeof(string) })?.Invoke(input, new object[] { text });
        }

        private static void SetResetButtonState(UISettingRebind rebind, InputAction action)
        {
            Button resetButton = AccessTools.Field(typeof(UISettingRebind), "resetButton")?.GetValue(rebind) as Button;

            if (resetButton == null || action == null || action.bindings.Count == 0)
                return;

            resetButton.interactable = action.bindings[0].hasOverrides &&
                                       action.bindings[0].overridePath != action.bindings[0].path;
        }

        private static void SetRebindLabel(UISettingRebind rebind, string label)
        {
            TMP_Text[] texts = rebind.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];

                if (text == null)
                    continue;

                string value = text.text != null
                    ? text.text.Trim()
                    : string.Empty;

                // Ignore input text values
                if (IsInputValueText(text))
                    continue;

                // Ignore listening text
                if (value == "...")
                    continue;

                // Ignore already modified labels
                if (value.StartsWith("Slot ", StringComparison.OrdinalIgnoreCase))
                    continue;

                text.text = label;
                return;
            }
        }

        private static bool IsInputValueText(TMP_Text text)
        {
            string value = text.text != null
                ? text.text.Trim()
                : string.Empty;

            if (string.IsNullOrEmpty(value))
                return false;

            value = value.ToUpperInvariant();

            // keyboard bindings
            if (value.Length == 1)
                return true;

            if (value.StartsWith("DIGIT"))
                return true;

            if (value == "W/A/S/D")
                return true;

            if (value == "SHIFT")
                return true;

            if (value == "CTRL")
                return true;

            if (value == "ALT")
                return true;

            if (value == "SPACE")
                return true;

            if (value == "MMB")
                return true;

            if (value == "LMB")
                return true;

            if (value == "RMB")
                return true;

            return false;
        }

        private static void RemovePreviousHotkeyUi(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);

                if (child.name.StartsWith(HotkeyObjectPrefix, StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static Transform FindDescendantPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            string[] parts = path.Split('/');
            return FindDescendantPath(root, parts, 0);
        }

        private static Transform FindDescendantPath(Transform root, string[] parts, int index)
        {
            if (root == null || parts == null || index >= parts.Length)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == parts[index])
                {
                    Transform directMatch = FindDescendantPath(child, parts, index + 1);

                    if (directMatch != null)
                        return directMatch;
                }

                Transform nestedMatch = FindDescendantPath(child, parts, index);

                if (nestedMatch != null)
                    return nestedMatch;
            }

            return null;
        }
    }
}