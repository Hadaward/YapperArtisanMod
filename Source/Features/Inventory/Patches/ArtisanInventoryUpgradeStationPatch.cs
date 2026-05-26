using Artisan.Localization;
using HarmonyLib;
using Mirror;
using UnityEngine;
using YAPYAP;
using FMODUnity;

namespace Artisan.Features.Inventory
{
    [HarmonyPatch(typeof(Shop), "OnStartClient")]
    public static class ArtisanInventoryUpgradeStationPatch
    {
        private const string PrefabName = "Chest_Small_interactive";
        private const string ObjectName = "Artisan_InventoryUpgradeChest";

        private static void Postfix(Shop __instance)
        {
            if (__instance == null)
                return;

            if (GameObject.Find(ObjectName) != null)
                return;

            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return;

            GameObject prefab = FindPrefab(PrefabName);

            if (prefab == null)
            {
                ArtisanMod.Logger.LogWarning($"Could not find prefab: {PrefabName}");
                return;
            }

            Vector3 position = __instance.transform.position + (__instance.transform.right * -2f);
            position.y += 0.25f;

            GameObject chest = Object.Instantiate(
                prefab,
                position,
                Quaternion.Euler(0f, __instance.transform.eulerAngles.y, 0f)
            );

            chest.name = ObjectName;
            chest.SetActive(true);

            StripGameplayComponents(chest);
            EnsureCollider(chest);

            ArtisanInventoryUpgradeStationInteractable station =
                chest.AddComponent<ArtisanInventoryUpgradeStationInteractable>();

            station.HoldInteractDuration = 1.25f;
            station.RefreshLocalInteractable();

            ArtisanMod.Logger.LogInfo("Spawned inventory upgrade chest in lobby.");
        }

        private static GameObject FindPrefab(string prefabName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];

                if (obj == null)
                    continue;

                if (obj.name == prefabName || obj.name == prefabName + ".prefab")
                    return obj;
            }

            return null;
        }

        private static void StripGameplayComponents(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);

            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];

                if (component == null ||
                    component is Transform ||
                    component is MeshRenderer ||
                    component is SkinnedMeshRenderer ||
                    component is MeshFilter ||
                    component is Animator ||
                    component is Collider)
                    continue;

                Object.Destroy(component);
            }

            SetActiveRecursively(root.transform);
        }

        private static void SetActiveRecursively(Transform transform)
        {
            transform.gameObject.SetActive(true);

            for (int i = 0; i < transform.childCount; i++)
                SetActiveRecursively(transform.GetChild(i));
        }

        private static void EnsureCollider(GameObject root)
        {
            if (root.GetComponentInChildren<Collider>() != null)
                return;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.2f, 0.8f, 0.8f);
            collider.center = new Vector3(0f, 0.4f, 0f);
        }
    }

    public sealed class ArtisanInventoryUpgradeStationInteractable : Interactable
    {
        protected override void Awake()
        {
            base.Awake();

            CanInteractAction += CannotInteract;
            CanHoldInteractAction += CanHoldInteract;
            CustomTooltipAction += ShowTooltip;
        }

        protected override void OnDestroy()
        {
            CanInteractAction -= CannotInteract;
            CanHoldInteractAction -= CanHoldInteract;
            CustomTooltipAction -= ShowTooltip;

            base.OnDestroy();
        }

        public void ShowUpgradeTooltip(NetworkIdentity identity)
        {
            ShowTooltip(this, identity);
        }

        private bool CannotInteract(NetworkIdentity identity, Interactable interactable)
        {
            return false;
        }

        private bool CanHoldInteract(NetworkIdentity identity, Interactable interactable)
        {
            PawnInventory inventory = GetInventory(identity);

            return inventory != null &&
                   ArtisanInventoryUpgradeService.CanPurchaseNextUpgrade(inventory);
        }

        private void ShowTooltip(Interactable interactable, NetworkIdentity identity)
        {
            PawnInventory inventory = GetInventory(identity);

            if (inventory == null)
                return;

            int unlocked = ArtisanInventoryUpgradeService.GetUnlockedExtraSlots(inventory);
            int max = ArtisanInventoryUpgradeService.GetMaxExtraSlots();

            if (unlocked >= max)
            {
                UIManager.Instance.uiTooltip.ShowTooltip(
                    ArtisanText.Format("inventory_upgrade_full"),
                    InputTarget.Interact,
                    false,
                    false,
                    null,
                    InputTarget.Fire,
                    true,
                    false,
                    null,
                    InputTarget.Use,
                    true,
                    false
                );

                return;
            }

            int price = ArtisanInventoryUpgradeService.GetNextUpgradePrice(inventory);
            bool canPurchase = ArtisanInventoryUpgradeService.CanPurchaseNextUpgrade(inventory);

            UIManager.Instance.uiTooltip.ShowTooltip(
                ArtisanText.Format("inventory_upgrade_buy", price),
                InputTarget.Interact,
                canPurchase,
                true,
                null,
                InputTarget.Fire,
                true,
                false,
                null,
                InputTarget.Use,
                true,
                false
            );
        }

        private static PawnInventory GetInventory(NetworkIdentity identity)
        {
            return identity == null ? null : identity.GetComponent<PawnInventory>();
        }
    }

    [HarmonyPatch(typeof(Interactable), "HoldInteract")]
    public static class ArtisanInventoryUpgradeStationHoldPatch
    {
        private static bool Prefix(Interactable __instance, NetworkIdentity identity)
        {
            if (!(__instance is ArtisanInventoryUpgradeStationInteractable))
                return true;

            PawnInventory inventory =
                identity == null ? null : identity.GetComponent<PawnInventory>();

            UIManager instance = UIManager.Instance;

            if (instance != null && instance.uiTooltip != null)
                instance.uiTooltip.HideTooltip();

            ArtisanInventoryUpgradeNetwork.RequestPurchase(inventory);

            ArtisanInventoryUpgradeStationInteractable station =
                __instance as ArtisanInventoryUpgradeStationInteractable;

            if (station != null)
            {
                station.StartCoroutine(RefreshTooltipAfterPurchase(station, identity));
            }

            return false;
        }

        private static System.Collections.IEnumerator RefreshTooltipAfterPurchase(ArtisanInventoryUpgradeStationInteractable station, NetworkIdentity identity)
        {
            yield return new UnityEngine.WaitForSeconds(0.15f);

            if (station == null)
                yield break;

            station.RefreshLocalInteractable();
            ArtisanLockedSlotOverlayPatch.RefreshCurrentInventory();

            UIManager instance = UIManager.Instance;

            if (instance == null || instance.uiTooltip == null)
                yield break;

            station.ShowUpgradeTooltip(identity);
        }
    }
}