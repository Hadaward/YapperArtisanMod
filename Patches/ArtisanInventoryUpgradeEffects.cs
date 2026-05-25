using FMODUnity;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using YAPYAP;
using YAPYAP.Npc.Shopkeeper;

namespace Artisan.Patches
{
    public static class ArtisanInventoryUpgradeEffects
    {
        private const string ShopkeeperPurchaseRpcName = "RpcOnPurchaseMade";
        private const string ChestName = "Artisan_InventoryUpgradeChest";

        public static void PlayPurchaseSuccess(PawnInventory inventory)
        {
            if (inventory == null)
                return;

            PlayShopkeeperPurchaseReaction();
        }

        public static void PlayLocalPurchaseSfx()
        {
            EventReference purchaseSfx = FindShopPurchaseSfx();

            if (purchaseSfx.IsNull)
                return;

            AudioManager audioManager;

            if (!Service.Get<AudioManager>(out audioManager))
                return;

            Vector3 position = FindPurchaseSfxPosition();

            audioManager.PlayEvent(
                purchaseSfx,
                position,
                1f,
                true,
                null,
                true
            );
        }

        private static void PlayShopkeeperPurchaseReaction()
        {
            ShopkeeperStateMachine shopkeeper =
                Object.FindFirstObjectByType<ShopkeeperStateMachine>();

            if (shopkeeper == null)
                return;

            MethodInfo rpcMethod = AccessTools.Method(
                typeof(ShopkeeperStateMachine),
                ShopkeeperPurchaseRpcName
            );

            if (rpcMethod == null)
            {
                ArtisanMod.Logger.LogWarning("Could not find ShopkeeperStateMachine.RpcOnPurchaseMade.");
                return;
            }

            rpcMethod.Invoke(
                shopkeeper,
                new object[]
                {
                    null
                }
            );
        }

        private static EventReference FindShopPurchaseSfx()
        {
            Shop shop = Object.FindFirstObjectByType<Shop>();

            if (shop == null)
                return default(EventReference);

            return HarmonyUtil.GetFieldValue<EventReference>(
                shop,
                "purchaseSfx"
            );
        }

        private static Vector3 FindPurchaseSfxPosition()
        {
            GameObject chest = GameObject.Find(ChestName);

            if (chest != null)
                return chest.transform.position;

            Shop shop = Object.FindFirstObjectByType<Shop>();

            if (shop != null)
                return shop.transform.position;

            return Vector3.zero;
        }
    }
}