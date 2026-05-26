using HarmonyLib;
using Mirror;
using Mirror.RemoteCalls;
using System.Reflection;
using UnityEngine;
using YAPYAP;

namespace Artisan.Features.Inventory
{
    public static class ArtisanInventoryUpgradeNetwork
    {
        private const string CommandSignature =
            "System.Void Artisan.Features.Inventory.ArtisanInventoryUpgradeNetwork::CmdPurchaseInventoryUpgrade()";

        private const string TargetRpcSignature =
            "System.Void Artisan.Features.Inventory.ArtisanInventoryUpgradeNetwork::TargetRpcInventoryUpgradePurchased()";

        private static readonly int CommandHash = CommandSignature.GetStableHashCode();
        private static readonly int TargetRpcHash = TargetRpcSignature.GetStableHashCode();

        private static bool registered;

        public static void Register()
        {
            if (registered)
                return;

            RemoteProcedureCalls.RegisterCommand(
                typeof(PawnInventory),
                CommandSignature,
                InvokeCmdPurchaseInventoryUpgrade,
                false
            );

            RemoteProcedureCalls.RegisterRpc(
                typeof(PawnInventory),
                TargetRpcSignature,
                InvokeTargetRpcInventoryUpgradePurchased
            );

            registered = true;

            ArtisanMod.Logger.LogInfo("Registered Artisan inventory upgrade command.");
        }

        public static void RequestPurchase(PawnInventory inventory)
        {
            if (inventory == null)
                return;

            if (NetworkServer.active)
            {
                bool purchased = ArtisanInventoryUpgradeService.TryPurchaseNextUpgrade(inventory);

                if (purchased)
                    SendPurchaseSuccess(inventory, inventory.connectionToClient);

                return;
            }

            NetworkWriterPooled writer = NetworkWriterPool.Get();

            try
            {
                MethodInfo sendCommandInternal = AccessTools.Method(
                    typeof(NetworkBehaviour),
                    "SendCommandInternal"
                );

                sendCommandInternal.Invoke(
                    inventory,
                    new object[]
                    {
                        CommandSignature,
                        CommandHash,
                        writer,
                        0,
                        true
                    }
                );
            }
            finally
            {
                NetworkWriterPool.Return(writer);
            }
        }

        private static void InvokeCmdPurchaseInventoryUpgrade(
            NetworkBehaviour obj,
            NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkServer.active)
                return;

            PawnInventory inventory = obj as PawnInventory;

            if (inventory == null)
                return;

            bool purchased = ArtisanInventoryUpgradeService.TryPurchaseNextUpgrade(inventory);

            if (purchased)
                SendPurchaseSuccess(inventory, senderConnection);
        }

        private static void SendPurchaseSuccess(
            PawnInventory inventory,
            NetworkConnectionToClient connection)
        {
            if (inventory == null)
                return;

            ArtisanInventoryUpgradeEffects.PlayPurchaseSuccess(inventory);

            if (connection == null)
                return;

            NetworkWriterPooled writer = NetworkWriterPool.Get();

            try
            {
                MethodInfo sendTargetRpcInternal = AccessTools.Method(
                    typeof(NetworkBehaviour),
                    "SendTargetRPCInternal"
                );

                sendTargetRpcInternal.Invoke(
                    inventory,
                    new object[]
                    {
                        connection,
                        TargetRpcSignature,
                        TargetRpcHash,
                        writer,
                        0
                    }
                );
            }
            finally
            {
                NetworkWriterPool.Return(writer);
            }
        }

        private static void InvokeTargetRpcInventoryUpgradePurchased(
            NetworkBehaviour obj,
            NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            ArtisanInventoryUpgradeEffects.PlayLocalPurchaseSfx();
            ArtisanLockedSlotOverlayPatch.RefreshCurrentInventory();
        }
    }
}