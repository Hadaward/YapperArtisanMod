using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using YAPYAP;

using Random = UnityEngine.Random;

namespace Artisan.Features.Monsters
{
    [HarmonyPatch]
    public static class NpcHurtboxClientHitFxPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(NpcHurtbox),
                "UserCode_RpcOnHit__Int32__Vector3"
            );
        }

        [HarmonyPostfix]
        private static void Postfix(NpcHurtbox __instance, int damage, Vector3 forceDir)
        {
            if (__instance == null)
                return;

            if (!ArtisanMod.EnableShowMonstersHealthBar.Value && !ArtisanMod.EnableShowMonsterDamageIndicator.Value)
                return;

            try
            {
                if (ArtisanMod.EnableShowMonstersHealthBar.Value)
                {
                    ArtisanEnemyHealthBar healthBar = __instance.GetComponent<ArtisanEnemyHealthBar>();

                    if (healthBar == null)
                        healthBar = __instance.gameObject.AddComponent<ArtisanEnemyHealthBar>();

                    healthBar.EnsureInitialized();
                    healthBar.ResetTimer();
                    healthBar.UpdateHealthDisplay();
                }

                if (ArtisanMod.EnableShowMonsterDamageIndicator.Value)
                    ArtisanDamageTextFactory.Create(GetDamageIndicatorPosition(__instance), Mathf.Clamp(damage, 1, 9999));

            }
            catch (Exception ex)
            {
                ArtisanMod.Logger.LogWarning("[MonsterHealthBar] Failed to render hit UI: " + ex);
            }
        }

        private static Vector3 GetDamageIndicatorPosition(NpcHurtbox __instance)
        {
            Vector3 position = __instance.transform.position;

            NpcBehaviour npc;
            if (__instance.TryGetComponent<NpcBehaviour>(out npc))
            {
                if (npc.Rigidbody != null)
                {
                    position = npc.Rigidbody.transform.position;
                } else if (npc.CameraTargetTransform != null)
                {
                    position = npc.CameraTargetTransform.position;
                }
            }

            position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1f, 1.5f), Random.Range(-0.5f, 0.5f));

            return position;
        }
    }
}