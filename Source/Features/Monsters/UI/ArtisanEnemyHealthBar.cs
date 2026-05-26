using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YAPYAP;

namespace Artisan.Features.Monsters
{
    public sealed class ArtisanEnemyHealthBar : MonoBehaviour
    {
        private sealed class Billboard : MonoBehaviour
        {
            private Camera mainCamera;

            private void LateUpdate()
            {
                if (mainCamera == null)
                    mainCamera = Camera.main;

                if (mainCamera == null)
                    return;

                transform.rotation = Quaternion.Euler(
                    mainCamera.transform.eulerAngles.x,
                    mainCamera.transform.eulerAngles.y,
                    0f
                );
            }
        }

        private static Sprite sharedWhiteSprite;

        private readonly Vector2 size = new Vector2(20f, 2.5f);
        private readonly Color backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        private readonly Color healthColor = Color.red;

        private float lastDamageTime;
        private bool isActive = true;
        private bool initialized;

        private Vector3 worldOffset = new Vector3(0f, 1.25f, 0f);

        private Canvas canvas;
        private Image foreground;
        private Image background;
        private TextMeshProUGUI healthText;

        private NpcHurtbox npcHurtbox;
        private NpcBehaviour npcBehaviour;
        private Transform followTransform;
        private Camera mainCamera;

        private float targetFillAmount = 1f;
        private int lastHealthCurrent = int.MinValue;
        private int lastHealthMax = int.MinValue;

        private float nextVisibilityUpdateTime;
        private float nextFollowResolveTime;
        private float nextOffsetResolveTime;

        public void EnsureInitialized()
        {
            if (initialized)
                return;

            npcHurtbox = GetComponent<NpcHurtbox>();
            if (npcHurtbox == null)
                return;

            npcBehaviour = GetComponent<NpcBehaviour>();
            followTransform = ResolveFollowTransform();
            mainCamera = Camera.main;

            InitializeCanvas();
            UpdateWorldOffsetFromBounds(true);
            UpdateCanvasWorldPosition();
            UpdateHealthDisplay();
            ResetTimer();

            initialized = true;
        }

        public void ResetTimer()
        {
            lastDamageTime = Time.time;

            if (!isActive && canvas != null)
                ReactivateHealthBar();
        }

        public void UpdateHealthDisplay()
        {
            if (npcHurtbox == null)
                return;

            int maxHealth = Mathf.Max(0, npcHurtbox.MaxHealth);
            int currentHealth = Mathf.Clamp(npcHurtbox.Health, 0, maxHealth);

            if (maxHealth > 0)
                targetFillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
            else
                targetFillAmount = 0f;

            UpdateHealthText();
        }

        private void InitializeCanvas()
        {
            GameObject canvasObject = new GameObject("ArtisanEnemyHealthCanvas");

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            canvas.transform.SetParent(followTransform != null ? followTransform : transform, false);
            canvas.transform.localPosition = worldOffset;

            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            rectTransform.localScale = new Vector3(0.03f, 0.03f, 0.03f);

            background = CreateBarElement(canvas.transform, backgroundColor, "Background");
            foreground = CreateBarElement(canvas.transform, healthColor, "Foreground");

            foreground.type = Image.Type.Filled;
            foreground.fillMethod = Image.FillMethod.Horizontal;
            foreground.fillOrigin = 0;
            foreground.fillAmount = targetFillAmount;

            canvasObject.AddComponent<Billboard>();

            CreateHealthText(canvas.transform);
        }

        private Transform ResolveFollowTransform()
        {
            if (npcBehaviour != null)
            {
                if (npcBehaviour.Rigidbody != null)
                    return npcBehaviour.Rigidbody.transform;

                if (npcBehaviour.CameraTargetTransform != null)
                    return npcBehaviour.CameraTargetTransform;
            }

            return transform;
        }

        private Transform ResolveBoundsRoot()
        {
            return npcBehaviour != null ? npcBehaviour.transform : transform;
        }

        private void Update()
        {
            EnsureInitialized();

            if (!initialized || !isActive)
                return;

            if (Time.time - lastDamageTime > 10f)
            {
                HideHealthBar();
                return;
            }

            if (npcHurtbox == null || foreground == null || background == null)
                return;

            RefreshFollowTransform();
            UpdateWorldOffsetFromBounds(false);
            UpdateCanvasWorldPosition();
            UpdateHealthValues();
            UpdateForegroundAnimation();

            if (Time.time >= nextVisibilityUpdateTime)
            {
                UpdateCanvasVisibility();
                nextVisibilityUpdateTime = Time.time + 0.1f;
            }
        }

        private void RefreshFollowTransform()
        {
            if (Time.time < nextFollowResolveTime)
                return;

            Transform resolvedTransform = ResolveFollowTransform();

            if (resolvedTransform != null && resolvedTransform != followTransform)
            {
                followTransform = resolvedTransform;

                if (canvas != null)
                {
                    canvas.transform.SetParent(followTransform, false);
                    canvas.transform.localPosition = worldOffset;
                }

                UpdateWorldOffsetFromBounds(true);
            }

            nextFollowResolveTime = Time.time + 0.5f;
        }

        private void UpdateHealthValues()
        {
            int health = npcHurtbox.Health;
            int maxHealth = npcHurtbox.MaxHealth;

            if (maxHealth <= 0)
                return;

            if (health == lastHealthCurrent && maxHealth == lastHealthMax)
                return;

            lastHealthCurrent = health;
            lastHealthMax = maxHealth;
            targetFillAmount = Mathf.Clamp01((float)health / maxHealth);

            UpdateHealthText();
        }

        private void UpdateForegroundAnimation()
        {
            foreground.fillAmount = Mathf.Lerp(
                foreground.fillAmount,
                targetFillAmount,
                Time.deltaTime * 10f
            );

            Color currentColor = foreground.color;

            if (targetFillAmount < 0.3f)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                foreground.color = new Color(1f, pulse, pulse, currentColor.a);
                return;
            }

            foreground.color = new Color(
                healthColor.r,
                healthColor.g,
                healthColor.b,
                currentColor.a
            );
        }

        private void UpdateWorldOffsetFromBounds(bool force)
        {
            if (!force && Time.time < nextOffsetResolveTime)
                return;

            nextOffsetResolveTime = Time.time + 1f;

            Transform rootTransform = ResolveBoundsRoot();

            Bounds bounds;
            if (rootTransform == null || followTransform == null || !TryGetWorldBounds(rootTransform, out bounds))
                return;

            float baseY = followTransform.position.y;
            float calculatedOffset = bounds.max.y - baseY + 0.15f;
            calculatedOffset = Mathf.Clamp(calculatedOffset, 0.35f, 12f);

            float extraOffset = ArtisanMod.MonsterHealthBarExtraHeightOffset != null
                ? ArtisanMod.MonsterHealthBarExtraHeightOffset.Value
                : 0.55f;

            float newOffsetY = Mathf.Clamp(calculatedOffset + extraOffset, 0.35f, 20f);

            if (Mathf.Abs(worldOffset.y - newOffsetY) <= 0.01f)
                return;

            worldOffset = new Vector3(worldOffset.x, newOffsetY, worldOffset.z);
            UpdateCanvasWorldPosition();
        }

        private void UpdateCanvasWorldPosition()
        {
            if (canvas == null || followTransform == null)
                return;

            canvas.transform.position =
                followTransform.position +
                new Vector3(worldOffset.x, 0f, worldOffset.z) +
                Vector3.up * worldOffset.y;
        }

        private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = default(Bounds);
            bool hasBounds = false;

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (hasBounds)
                return true;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void CreateHealthText(Transform parent)
        {
            GameObject textObject = new GameObject("HealthText");
            textObject.transform.SetParent(parent, false);

            healthText = textObject.AddComponent<TextMeshProUGUI>();
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.fontSize = 10f;
            healthText.color = Color.white;
            healthText.margin = new Vector4(0f, 0f, 0f, 10f);

            RectTransform rectTransform = healthText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 5f);
            rectTransform.sizeDelta = new Vector2(100f, 20f);
        }

        private void UpdateHealthText()
        {
            if (healthText == null || npcHurtbox == null)
                return;

            int maxHealth = Mathf.Max(0, npcHurtbox.MaxHealth);
            int currentHealth = Mathf.Clamp(npcHurtbox.Health, 0, maxHealth);

            healthText.text = currentHealth + "/" + maxHealth;

            if (maxHealth > 0 && (float)currentHealth / maxHealth < 0.3f)
                healthText.color = Color.yellow;
            else
                healthText.color = Color.white;
        }

        private Image CreateBarElement(Transform parent, Color color, string name)
        {
            GameObject elementObject = new GameObject(name);
            elementObject.transform.SetParent(parent, false);

            Image image = elementObject.AddComponent<Image>();
            image.sprite = GetSharedWhiteSprite();
            image.color = color;

            RectTransform rectTransform = elementObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return image;
        }

        private static Sprite GetSharedWhiteSprite()
        {
            if (sharedWhiteSprite != null)
                return sharedWhiteSprite;

            Texture2D whiteTexture = Texture2D.whiteTexture;

            sharedWhiteSprite = Sprite.Create(
                whiteTexture,
                new Rect(0f, 0f, whiteTexture.width, whiteTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            return sharedWhiteSprite;
        }

        private void UpdateCanvasVisibility()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || foreground == null || background == null)
                return;

            Vector3 position = canvas != null ? canvas.transform.position : transform.position;

            float distance = Vector3.Distance(mainCamera.transform.position, position);
            float alpha = Mathf.Clamp01(1f - distance / 50f);

            foreground.color = new Color(foreground.color.r, foreground.color.g, foreground.color.b, alpha);
            background.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, alpha * 0.8f);

            if (healthText != null)
                healthText.alpha = alpha;
        }

        private void ReactivateHealthBar()
        {
            isActive = true;

            if (canvas != null)
                canvas.gameObject.SetActive(true);

            UpdateHealthDisplay();
            UpdateCanvasVisibility();
        }

        private void HideHealthBar()
        {
            isActive = false;

            if (canvas != null)
                canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (canvas != null)
                UnityEngine.Object.Destroy(canvas.gameObject);

            canvas = null;
        }
    }
}