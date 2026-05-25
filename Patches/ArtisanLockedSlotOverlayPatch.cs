using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using YAPYAP;

namespace Artisan.Patches
{
    [HarmonyPatch]
    public static class ArtisanLockedSlotOverlayPatch
    {
        [HarmonyPatch(typeof(UIInventory), "RefreshInventoryDisplay")]
        [HarmonyPostfix]
        private static void RefreshInventoryDisplayPostfix(UIInventory __instance)
        {
            Refresh(__instance);
        }

        public static void RefreshCurrentInventory()
        {
            if (UIManager.Instance == null ||
                UIManager.Instance.uiGame == null ||
                UIManager.Instance.uiGame.uiPlayer == null ||
                UIManager.Instance.uiGame.uiPlayer.uiInventory == null)
                return;

            Refresh(UIManager.Instance.uiGame.uiPlayer.uiInventory);
        }

        private static void Refresh(UIInventory inventoryUi)
        {
            if (inventoryUi == null)
                return;

            PawnInventory inventory = HarmonyUtil.GetFieldValue<PawnInventory>(
                inventoryUi,
                "_playerInventory"
            );

            if (inventory == null)
                return;

            UIInventorySlot[] slots = HarmonyUtil.GetFieldValue<UIInventorySlot[]>(
                inventoryUi,
                "inventorySlots"
            );

            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                UIInventorySlot slot = slots[i];

                if (slot == null)
                    continue;

                ArtisanLockedSlotOverlay overlay =
                    slot.GetComponent<ArtisanLockedSlotOverlay>();

                if (overlay == null)
                    overlay = slot.gameObject.AddComponent<ArtisanLockedSlotOverlay>();

                bool isLocked =
                    ArtisanInventoryUpgradeService.IsEnabled() &&
                    !ArtisanInventoryUpgradeService.IsSlotUnlocked(inventory, slot.SlotIndex);

                overlay.SetLocked(isLocked);
            }
        }
    }

    public sealed class ArtisanLockedSlotOverlay : MonoBehaviour
    {
        private const string OverlayName = "Artisan_LockedSlotOverlay";
        private const string LockIconName = "Artisan_LockedSlotIcon";

        private static readonly Vector2 OverlayInset = new Vector2(2f, 2f);
        private static readonly Vector2 LockIconSize = new Vector2(24f, 24f);
        private static readonly Vector2 LockIconOffset = new Vector2(0f, 4f);
        private static readonly Color OverlayColor = new Color(0.35f, 0.02f, 0.02f, 0.45f);
        private static readonly Color LockIconColor = new Color(0.86f, 0.90f, 0.92f, 0.96f);

        private GameObject overlayObject;
        private Image lockImage;

        public bool IsLocked { get; private set; }

        public void SetLocked(bool isLocked)
        {
            EnsureCreated();
            SyncOverlayRect();

            IsLocked = isLocked;

            if (overlayObject != null)
                overlayObject.SetActive(isLocked);
        }

        private void EnsureCreated()
        {
            if (overlayObject != null)
                return;

            overlayObject = new GameObject(
                OverlayName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image)
            );

            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.SetAsLastSibling();

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = OverlayColor;
            overlayImage.raycastTarget = false;

            CanvasGroup canvasGroup = overlayObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Sprite lockSprite = ArtisanTeleLockSpriteFactory.GetOrCreate();

            if (lockSprite == null)
            {
                ArtisanMod.Logger.LogWarning("Tele-lock mesh sprite could not be created. Locked slot icon will be hidden.");
                overlayObject.SetActive(false);
                return;
            }

            GameObject iconObject = new GameObject(
                LockIconName,
                typeof(RectTransform),
                typeof(Image)
            );

            iconObject.transform.SetParent(overlayObject.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = LockIconSize;
            iconRect.anchoredPosition = LockIconOffset;

            lockImage = iconObject.GetComponent<Image>();
            lockImage.sprite = lockSprite;
            lockImage.color = LockIconColor;
            lockImage.raycastTarget = false;
            lockImage.preserveAspect = true;

            overlayObject.SetActive(false);
        }

        private void SyncOverlayRect()
        {
            if (overlayObject == null)
                return;

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();

            if (overlayRect == null)
                return;

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.offsetMin = OverlayInset;
            overlayRect.offsetMax = -OverlayInset;
            overlayRect.localScale = Vector3.one;
            overlayRect.localRotation = Quaternion.identity;
            overlayObject.transform.SetAsLastSibling();
        }
    }

    public static class ArtisanTeleLockSpriteFactory
    {
        private const string MeshName = "SM_VFX_Tele_Lock_03";

        private static Sprite cachedSprite;

        public static Sprite GetOrCreate()
        {
            if (cachedSprite != null)
                return cachedSprite;

            Mesh mesh = FindTeleLockMesh();

            if (mesh == null)
                return null;

            cachedSprite = RenderMeshToSprite(mesh);

            if (cachedSprite != null)
                cachedSprite.name = "Artisan_TeleLockSprite";

            return cachedSprite;
        }

        private static Mesh FindTeleLockMesh()
        {
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();

            for (int i = 0; i < meshes.Length; i++)
            {
                Mesh mesh = meshes[i];

                if (mesh == null)
                    continue;

                if (mesh.name == MeshName || mesh.name.Contains(MeshName))
                    return mesh;
            }

            ArtisanMod.Logger.LogWarning("Could not find mesh asset: " + MeshName);
            return null;
        }

        private static void RemoveBlackBackground(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];

                if (pixel.r < 0.03f && pixel.g < 0.03f && pixel.b < 0.03f)
                    pixels[i] = new Color(0f, 0f, 0f, 0f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private static Sprite RenderMeshToSprite(Mesh mesh)
        {
            RenderTexture renderTexture = null;
            GameObject cameraObject = null;
            GameObject meshObject = null;
            Material material = null;
            RenderTexture previous = RenderTexture.active;

            try
            {
                renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
                renderTexture.name = "Artisan_TeleLockRenderTexture";
                renderTexture.antiAliasing = 4;
                renderTexture.Create();

                cameraObject = new GameObject("Artisan_TeleLockRenderCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = 0.7f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.transform.position = new Vector3(0f, 0f, -3f);
                camera.transform.rotation = Quaternion.identity;
                camera.targetTexture = renderTexture;
                camera.enabled = false;

                meshObject = new GameObject("Artisan_TeleLockRenderMesh");

                MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

                meshFilter.sharedMesh = mesh;

                Shader shader = Shader.Find("Unlit/Color");

                if (shader == null)
                    shader = Shader.Find("UI/Default");

                if (shader == null)
                    shader = Shader.Find("Sprites/Default");

                material = new Material(shader);
                material.color = Color.white;

                meshRenderer.sharedMaterial = material;

                meshObject.transform.rotation = Quaternion.Euler(-90f, 0f, 180f);

                Bounds bounds = mesh.bounds;
                float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

                if (maxSize > 0f)
                    meshObject.transform.localScale = Vector3.one * (1f / maxSize);

                meshObject.transform.position = -bounds.center * meshObject.transform.localScale.x;

                camera.Render();

                RenderTexture.active = renderTexture;

                Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                texture.name = "Artisan_TeleLockTexture";
                texture.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
                texture.Apply();

                RemoveBlackBackground(texture);

                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    128f
                );
            }
            finally
            {
                RenderTexture.active = previous;

                if (cameraObject != null)
                    Object.Destroy(cameraObject);

                if (meshObject != null)
                    Object.Destroy(meshObject);

                if (material != null)
                    Object.Destroy(material);

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.Destroy(renderTexture);
                }
            }
        }
    }
}