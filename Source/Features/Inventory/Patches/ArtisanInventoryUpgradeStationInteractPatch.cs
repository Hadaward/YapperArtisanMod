using HarmonyLib;
using YAPYAP;

namespace Artisan.Features.Inventory
{
    [HarmonyPatch(typeof(Interactable), "Interact")]
    public static class ArtisanInventoryUpgradeStationInteractPatch
    {
        private static bool Prefix(Interactable __instance)
        {
            return !(__instance is ArtisanInventoryUpgradeStationInteractable);
        }
    }
}