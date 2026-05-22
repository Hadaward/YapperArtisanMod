using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YAPYAP;

namespace Artisan.Patches
{
    [HarmonyPatch(typeof(UIInventory), "InitializeSlots")]
    public static class UIInventoryGridPatch
    {
        private const int Columns = 3;
        private const float SelectedItemNameFontSize = 18f;

        private static readonly Vector2 GridOffset = new Vector2(-34f, 14f);
        private static readonly Vector2 NameOffset = new Vector2(0f, 42f);

        private static readonly Vector2 SlotFramePadding = new Vector2(10f, 10f);
        private static readonly Vector2 SlotVisualSpacing = new Vector2(2f, 8f);
        private static readonly Vector2 SlotNumberOffset = new Vector2(0f, -6f);
        private static readonly Vector2 SlotFrameOffset = new Vector2(0f, 0f);
        private static readonly Vector2 LeftHandExtraOffset = new Vector2(-6f, -2f);

        private static readonly Vector2 PromptContainerOffset = new Vector2(-70f, 20f);

        private static void Postfix(UIInventory __instance)
        {
            if (ArtisanMod.EnableExtendedInventorySlots == null || !ArtisanMod.EnableExtendedInventorySlots.Value)
                return;

            if (__instance == null)
                return;

            UIInventorySlot[] slots =
                HarmonyUtil.GetFieldValue<UIInventorySlot[]>(
                    __instance,
                    "inventorySlots"
                );

            if (slots == null || slots.Length == 0 || slots[0] == null)
                return;

            RectTransform container =
                slots[0].transform.parent as RectTransform;

            if (container == null)
                return;

            DisableLayoutComponents(container);
            HideInventoryBackground(__instance, container);

            RectTransform firstRect =
                slots[0].GetComponent<RectTransform>();

            Vector2 cellSize =
                firstRect != null
                    ? firstRect.sizeDelta
                    : new Vector2(36f, 36f);

            PositionInventorySlots(slots, cellSize);
            CreateSlotFramesFromOffhandItem(__instance, slots, cellSize);

            MoveSlotNumbers(slots);

            PositionLeftHandSlot(__instance, container, cellSize);
            PositionInputPrompts(__instance, container, cellSize);

            MoveSelectedItemName(__instance, container, cellSize);

            LayoutRebuilder.MarkLayoutForRebuild(container);
        }

        private static void PositionInputPrompts(UIInventory inventory, RectTransform container, Vector2 cellSize)
        {
            CanvasGroup scrollGroup =
                HarmonyUtil.GetFieldValue<CanvasGroup>(
                    inventory,
                    "scrollCanvasGroup"
                );

            if (scrollGroup == null)
                return;

            RectTransform promptRect =
                scrollGroup.transform.parent as RectTransform;

            if (promptRect == null)
                return;

            Vector2 frameSize = cellSize + SlotFramePadding;
            Vector2 step = frameSize + SlotVisualSpacing;

            Vector2 leftHandPosition =
                GridOffset +
                new Vector2(
                    -step.x,
                    -step.y * 0.5f
                ) +
                LeftHandExtraOffset;

            promptRect.SetParent(container, false);
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.localScale = Vector3.one;
            promptRect.localRotation = Quaternion.identity;

            promptRect.anchoredPosition =
                leftHandPosition +
                PromptContainerOffset;

            promptRect.SetAsLastSibling();
        }

        private static void MoveSlotNumbers(UIInventorySlot[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                UIInventorySlot slot = slots[i];

                if (slot == null)
                    continue;

                UpdateSlotNumberText(slot, i + 1);
                MoveQuantityText(slot);
            }
        }

        private static void UpdateSlotNumberText(UIInventorySlot slot, int number)
        {
            TMP_Text[] texts =
                slot.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                string value = text.text != null
                    ? text.text.Trim()
                    : "";

                int parsed;

                if (!int.TryParse(value, out parsed))
                    continue;

                if (parsed < 1 || parsed > 30)
                    continue;

                text.text = number.ToString();
            }
        }

        private static void MoveQuantityText(UIInventorySlot slot)
        {
            TextMeshProUGUI quantityText =
                HarmonyUtil.GetFieldValue<TextMeshProUGUI>(
                    slot,
                    "quantityText"
                );

            if (quantityText == null)
                return;

            RectTransform textRect =
                quantityText.GetComponent<RectTransform>();

            if (textRect == null)
                return;

            textRect.anchoredPosition += SlotNumberOffset;
        }

        private static void PositionInventorySlots(UIInventorySlot[] slots, Vector2 cellSize)
        {
            Vector2 frameSize = cellSize + SlotFramePadding;
            Vector2 step = frameSize + SlotVisualSpacing;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                RectTransform rect = slots[i].GetComponent<RectTransform>();

                if (rect == null)
                    continue;

                int row = i / Columns;
                int col = i % Columns;

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;

                rect.anchoredPosition =
                    GridOffset +
                    new Vector2(
                        col * step.x,
                        -row * step.y
                    );

                rect.SetSiblingIndex(i);
            }
        }

        private static void PositionLeftHandSlot(UIInventory inventory, RectTransform container, Vector2 cellSize)
        {
            UIInventorySlot leftHandSlot =
                HarmonyUtil.GetFieldValue<UIInventorySlot>(
                    inventory,
                    "leftHandSlot"
                );

            if (leftHandSlot == null)
                return;

            RectTransform rect = leftHandSlot.GetComponent<RectTransform>();

            if (rect == null)
                return;

            Vector2 frameSize = cellSize + SlotFramePadding;
            Vector2 step = frameSize + SlotVisualSpacing;

            Vector2 leftHandPosition =
                GridOffset +
                new Vector2(
                    -step.x,
                    -step.y * 0.5f
                ) +
                LeftHandExtraOffset;

            rect.SetParent(container, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = leftHandPosition;

            AlignOffhandFrame(inventory, container, leftHandPosition, cellSize);

            rect.SetAsLastSibling();
        }

        private static void AlignOffhandFrame(UIInventory inventory, RectTransform container, Vector2 position, Vector2 cellSize)
        {
            Transform sourceFrame = FindDeepChild(inventory.transform, "OffhandItem");

            if (sourceFrame == null)
                return;

            RectTransform frameRect = sourceFrame as RectTransform;

            if (frameRect == null)
                return;

            frameRect.SetParent(container, false);
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.localScale = Vector3.one;
            frameRect.localRotation = Quaternion.identity;
            frameRect.sizeDelta = cellSize + SlotFramePadding;
            frameRect.anchoredPosition = position + SlotFrameOffset;

            sourceFrame.gameObject.SetActive(true);
            frameRect.SetAsFirstSibling();
        }

        private static void MoveSelectedItemName(UIInventory inventory, RectTransform container, Vector2 cellSize)
        {
            TextMeshProUGUI itemName =
                HarmonyUtil.GetFieldValue<TextMeshProUGUI>(
                    inventory,
                    "currentSelectedItemNameText"
                );

            if (itemName == null)
                return;

            RectTransform textRect =
                itemName.GetComponent<RectTransform>();

            if (textRect == null)
                return;

            textRect.SetParent(container, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 frameSize = cellSize + SlotFramePadding;
            Vector2 step = frameSize + SlotVisualSpacing;

            float gridWidth =
                Columns * frameSize.x +
                (Columns - 1) * SlotVisualSpacing.x;

            float leftX = GridOffset.x;
            float rightX = GridOffset.x + ((Columns - 1) * step.x);
            float centerX = (leftX + rightX) * 0.5f;

            textRect.anchoredPosition =
                new Vector2(centerX, GridOffset.y + NameOffset.y);

            textRect.sizeDelta =
                new Vector2(gridWidth + 40f, textRect.sizeDelta.y);

            itemName.alignment = TextAlignmentOptions.Center;
            itemName.textWrappingMode = TextWrappingModes.NoWrap;
            itemName.fontSize = SelectedItemNameFontSize;
            itemName.margin = Vector4.zero;

            textRect.SetAsLastSibling();
        }

        private static void DisableLayoutComponents(RectTransform container)
        {
            LayoutGroup[] layouts =
                container.GetComponents<LayoutGroup>();

            foreach (LayoutGroup layout in layouts)
            {
                if (layout != null)
                    layout.enabled = false;
            }

            ContentSizeFitter fitter =
                container.GetComponent<ContentSizeFitter>();

            if (fitter != null)
                fitter.enabled = false;
        }

        private static void HideInventoryBackground(UIInventory inventory, RectTransform container)
        {
            Transform root = inventory.transform;

            Image[] images =
                root.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image == null)
                    continue;

                if (IsSlotImage(image))
                    continue;

                string name =
                    image.gameObject.name.ToLowerInvariant();

                if (image.transform.name == "OffhandItem")
                    continue;

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
            UIInventorySlot slot =
                image.GetComponentInParent<UIInventorySlot>();

            return slot != null;
        }

        private static void CreateSlotFramesFromOffhandItem(UIInventory inventory, UIInventorySlot[] slots, Vector2 cellSize)
        {
            Transform sourceFrame = FindDeepChild(inventory.transform, "OffhandItem");

            if (sourceFrame == null)
            {
                Debug.LogWarning("[Artisan] OffhandItem frame not found.");
                return;
            }

            RectTransform sourceRect = sourceFrame as RectTransform;

            if (sourceRect == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                RectTransform slotRect = slots[i].GetComponent<RectTransform>();

                if (slotRect == null || slotRect.parent == null)
                    continue;

                string frameName = "ArtisanSlotFrame_" + i;

                Transform oldFrame = slotRect.parent.Find(frameName);

                if (oldFrame != null)
                    Object.Destroy(oldFrame.gameObject);

                GameObject frameObj = Object.Instantiate(sourceFrame.gameObject, slotRect.parent);
                frameObj.name = frameName;

                RectTransform frameRect = frameObj.GetComponent<RectTransform>();

                if (frameRect == null)
                    continue;

                frameRect.anchorMin = slotRect.anchorMin;
                frameRect.anchorMax = slotRect.anchorMax;
                frameRect.pivot = slotRect.pivot;
                frameRect.localScale = Vector3.one;
                frameRect.localRotation = Quaternion.identity;

                frameRect.sizeDelta = cellSize + SlotFramePadding;
                frameRect.anchoredPosition = slotRect.anchoredPosition + SlotFrameOffset;

                frameObj.SetActive(true);

                frameRect.SetSiblingIndex(slotRect.GetSiblingIndex());
                slotRect.SetSiblingIndex(frameRect.GetSiblingIndex() + 1);
            }
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