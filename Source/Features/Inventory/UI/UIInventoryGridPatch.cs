using Artisan.Shared.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YAPYAP;

namespace Artisan.Features.Inventory
{
    [HarmonyPatch(typeof(UIInventory), "InitializeSlots")]
    public static class UIInventoryGridPatch
    {
        private const int Columns = 3;
        private const float SelectedItemNameFontSize = 18f;

        private static readonly Vector2 GridOrigin = new Vector2(-34f, 20f);
        private static readonly Vector2 SlotSpacing = new Vector2(40f, 52f);

        private static readonly Vector2 SlotFrameSizeOffset = new Vector2(1f, 1f);

        private static readonly Vector2 SlotNumberPosition = new Vector2(0f, -24f);

        private static readonly Vector2 LeftHandOffset = new Vector2(-48f, -26f);
        private static readonly Vector2 LeftHandFrameSizeOffset = new Vector2(1f, 1f);

        private static readonly Vector2 PromptContainerOffset = new Vector2(-70f, 20f);
        private static readonly Vector2 NameOffset = new Vector2(0f, 42f);

        private static void Postfix(UIInventory __instance)
        {
            if (ArtisanMod.EnableExtendedInventorySlots == null || !ArtisanMod.EnableExtendedInventorySlots.Value)
                return;

            if (__instance == null)
                return;

            UIInventorySlot[] slots = HarmonyUtil.GetFieldValue<UIInventorySlot[]>(
                __instance,
                "inventorySlots"
            );

            if (slots == null || slots.Length == 0 || slots[0] == null)
                return;

            RectTransform container = slots[0].transform.parent as RectTransform;

            if (container == null)
                return;

            DisableLayoutComponents(container);
            HideInventoryBackground(__instance);

            RectTransform firstSlotRect = slots[0].GetComponent<RectTransform>();
            Vector2 slotSize = firstSlotRect != null
                ? firstSlotRect.sizeDelta
                : new Vector2(36f, 36f);

            Transform frameTemplate = FindDeepChild(__instance.transform, "OffhandItem");

            PositionInventorySlots(slots);
            CreateSlotFramesFromSlots(__instance, slots);

            PositionSlotNumbers(slots);

            PositionLeftHandSlot(__instance, container, slotSize, frameTemplate);
            CreateLeftHandFrameFromSlot(__instance);

            PositionInputPrompts(__instance, container);
            PositionSelectedItemName(__instance, container);

            LayoutRebuilder.MarkLayoutForRebuild(container);
        }

        private static Vector2 GetSlotPosition(int index)
        {
            int row = index / Columns;
            int column = index % Columns;

            return GridOrigin + new Vector2(
                column * SlotSpacing.x,
                -row * SlotSpacing.y
            );
        }

        private static void PositionInventorySlots(UIInventorySlot[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                UIInventorySlot slot = slots[i];

                if (slot == null)
                    continue;

                RectTransform rect = slot.GetComponent<RectTransform>();

                if (rect == null)
                    continue;

                SetCentered(rect);
                rect.anchoredPosition = GetSlotPosition(i);
                rect.SetSiblingIndex(i);
            }
        }

        private static void CreateSlotFramesFromSlots(UIInventory inventory, UIInventorySlot[] slots)
        {
            Transform frameTemplate = FindDeepChild(inventory.transform, "OffhandItem");

            if (frameTemplate == null)
            {
                ArtisanMod.Logger.LogWarning("OffhandItem frame template not found.");
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                UIInventorySlot slot = slots[i];

                if (slot == null)
                    continue;

                RectTransform slotRect = slot.GetComponent<RectTransform>();

                if (slotRect == null || slotRect.parent == null)
                    continue;

                string frameName = "ArtisanSlotFrame_" + i;
                Transform oldFrame = slotRect.parent.Find(frameName);

                if (oldFrame != null)
                    Object.Destroy(oldFrame.gameObject);

                GameObject frameObject = Object.Instantiate(frameTemplate.gameObject, slotRect.parent);
                frameObject.name = frameName;
                frameObject.SetActive(true);

                RectTransform frameRect = frameObject.GetComponent<RectTransform>();

                if (frameRect == null)
                    continue;

                CopySlotTransform(slotRect, frameRect);
                frameRect.sizeDelta = slotRect.sizeDelta + SlotFrameSizeOffset;

                frameRect.SetSiblingIndex(slotRect.GetSiblingIndex());
                slotRect.SetSiblingIndex(frameRect.GetSiblingIndex() + 1);
            }
        }

        private static void CreateLeftHandFrameFromSlot(UIInventory inventory)
        {
            Transform frameTemplate = FindDeepChild(inventory.transform, "OffhandItem");

            if (frameTemplate == null)
                return;

            UIInventorySlot leftHandSlot = HarmonyUtil.GetFieldValue<UIInventorySlot>(
                inventory,
                "leftHandSlot"
            );

            if (leftHandSlot == null)
                return;

            RectTransform slotRect = leftHandSlot.GetComponent<RectTransform>();
            RectTransform frameRect = frameTemplate as RectTransform;

            if (slotRect == null || frameRect == null)
                return;

            frameRect.SetParent(slotRect.parent, false);
            CopySlotTransform(slotRect, frameRect);
            frameRect.sizeDelta = slotRect.sizeDelta + LeftHandFrameSizeOffset;

            frameTemplate.gameObject.SetActive(true);
            frameRect.SetSiblingIndex(slotRect.GetSiblingIndex());
            slotRect.SetSiblingIndex(frameRect.GetSiblingIndex() + 1);
        }

        private static void CopySlotTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }

        private static void PositionSlotNumbers(UIInventorySlot[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                UIInventorySlot slot = slots[i];

                if (slot == null)
                    continue;

                UpdateSlotNumberText(slot, i + 1);

                TextMeshProUGUI quantityText = HarmonyUtil.GetFieldValue<TextMeshProUGUI>(
                    slot,
                    "quantityText"
                );

                if (quantityText == null)
                    continue;

                RectTransform textRect = quantityText.GetComponent<RectTransform>();

                if (textRect == null)
                    continue;

                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = SlotNumberPosition;

                quantityText.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void UpdateSlotNumberText(UIInventorySlot slot, int number)
        {
            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];

                if (text == null)
                    continue;

                string value = text.text != null ? text.text.Trim() : "";

                int parsed;

                if (!int.TryParse(value, out parsed))
                    continue;

                if (parsed < 1 || parsed > 30)
                    continue;

                text.text = number.ToString();
            }
        }

        private static void PositionLeftHandSlot(
            UIInventory inventory,
            RectTransform container,
            Vector2 slotSize,
            Transform frameTemplate)
        {
            UIInventorySlot leftHandSlot = HarmonyUtil.GetFieldValue<UIInventorySlot>(
                inventory,
                "leftHandSlot"
            );

            if (leftHandSlot == null)
                return;

            RectTransform slotRect = leftHandSlot.GetComponent<RectTransform>();

            if (slotRect == null)
                return;

            Vector2 leftHandPosition = GridOrigin + LeftHandOffset;

            slotRect.SetParent(container, false);
            SetCentered(slotRect);
            slotRect.anchoredPosition = leftHandPosition;
            slotRect.localScale = Vector3.one;
            slotRect.localRotation = Quaternion.identity;

            slotRect.SetAsLastSibling();
        }

        private static void PositionInputPrompts(UIInventory inventory, RectTransform container)
        {
            CanvasGroup scrollGroup = HarmonyUtil.GetFieldValue<CanvasGroup>(
                inventory,
                "scrollCanvasGroup"
            );

            if (scrollGroup == null)
                return;

            RectTransform promptRect = scrollGroup.transform.parent as RectTransform;

            if (promptRect == null)
                return;

            promptRect.SetParent(container, false);
            SetCentered(promptRect);
            promptRect.localScale = Vector3.one;
            promptRect.localRotation = Quaternion.identity;
            promptRect.anchoredPosition = GridOrigin + LeftHandOffset + PromptContainerOffset;
            promptRect.SetAsLastSibling();
        }

        private static void PositionSelectedItemName(UIInventory inventory, RectTransform container)
        {
            TextMeshProUGUI itemName = HarmonyUtil.GetFieldValue<TextMeshProUGUI>(
                inventory,
                "currentSelectedItemNameText"
            );

            if (itemName == null)
                return;

            RectTransform textRect = itemName.GetComponent<RectTransform>();

            if (textRect == null)
                return;

            float leftX = GetSlotPosition(0).x;
            float rightX = GetSlotPosition(Columns - 1).x;
            float centerX = (leftX + rightX) * 0.5f;
            float width = (rightX - leftX) + 80f;

            textRect.SetParent(container, false);
            SetCentered(textRect);
            textRect.anchoredPosition = new Vector2(centerX, GridOrigin.y + NameOffset.y);
            textRect.sizeDelta = new Vector2(width, textRect.sizeDelta.y);

            itemName.alignment = TextAlignmentOptions.Center;
            itemName.textWrappingMode = TextWrappingModes.NoWrap;
            itemName.fontSize = SelectedItemNameFontSize;
            itemName.margin = Vector4.zero;

            textRect.SetAsLastSibling();
        }

        private static void DisableLayoutComponents(RectTransform container)
        {
            LayoutGroup[] layouts = container.GetComponents<LayoutGroup>();

            for (int i = 0; i < layouts.Length; i++)
            {
                if (layouts[i] != null)
                    layouts[i].enabled = false;
            }

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();

            if (fitter != null)
                fitter.enabled = false;
        }

        private static void HideInventoryBackground(UIInventory inventory)
        {
            Image[] images = inventory.transform.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null)
                    continue;

                if (IsSlotImage(image))
                    continue;

                if (image.transform.name == "OffhandItem")
                    continue;

                string name = image.gameObject.name.ToLowerInvariant();

                if (name.Contains("panel") ||
                    name.Contains("background") ||
                    name.Contains("back") ||
                    name.Contains("frame") ||
                    name.Contains("inventory") ||
                    name.Contains("container") ||
                    name.Contains("ornament") ||
                    name.Contains("border"))
                {
                    image.enabled = false;
                }
            }
        }

        private static bool IsSlotImage(Image image)
        {
            return image.GetComponentInParent<UIInventorySlot>() != null;
        }

        private static void SetCentered(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform result = FindDeepChild(child, childName);

                if (result != null)
                    return result;
            }

            return null;
        }
    }
}