using Artisan.Shared.Reflection;

namespace Artisan.Features.Spells
{
    using HarmonyLib;
    using Mirror;
    using System.Collections.Generic;
    using UnityEngine;
    using YAPYAP;

    [HarmonyPatch(typeof(ProjectilePush), "OnTargetHit")]
    public class ProjectilePushDamagePatch
    {
        private static readonly Dictionary<int, HashSet<uint>> DamagedTargetsByProjectile = new Dictionary<int, HashSet<uint>>();

        public static void Prefix(ProjectilePush __instance, Collider target, Rigidbody targetRigidbody, NetworkIdentity targetIdentity)
        {
            if (!NetworkServer.active || __instance == null || target == null || targetRigidbody == null || targetIdentity == null)
                return;

            bool isAeroProjectile = IsAeroProjectile(__instance);
            bool isLevitationBlastProjectile = IsLevitationBlastProjectile(__instance);

            if (!isAeroProjectile && !isLevitationBlastProjectile)
                return;

            if (isAeroProjectile && !ArtisanMod.EnableAeroDamagePatch.Value)
                return;

            if (isLevitationBlastProjectile && !ArtisanMod.EnableTeleBlastDamagePatch.Value)
                return;

            int damageAmount = isAeroProjectile
                ? ArtisanMod.AeroSpellDamage.Value
                : isLevitationBlastProjectile
                    ? ArtisanMod.TeleBlastSpellDamage.Value
                    : 0;

            bool affectPlayers = isAeroProjectile
                ? ArtisanMod.MakeAeroDamageOtherPlayers.Value
                : isLevitationBlastProjectile
                    ? ArtisanMod.MakeTeleBlastDamageOtherPlayers.Value
                    : false;

            LayerMask pushableLayer = HarmonyUtil.GetFieldValue<LayerMask>(__instance, "pushableLayer");

            if (((1 << target.gameObject.layer) & pushableLayer.value) == 0)
                return;

            HashSet<Rigidbody> affectedRigidbodies =
                HarmonyUtil.GetFieldValue<HashSet<Rigidbody>>(__instance, "affectedRigidbodies");

            if (affectedRigidbodies != null && affectedRigidbodies.Contains(targetRigidbody))
                return;

            if (!__instance.isServer)
                return;

            NetworkIdentity caster = HarmonyUtil.GetFieldValue<NetworkIdentity>(__instance, "Caster");
            Vector3 forward = __instance.transform.forward;

            if (!TryMarkTargetDamaged(__instance, targetIdentity))
                return;

            NpcBehaviour npc;

            if (targetIdentity.TryGetComponent(out npc))
                npc.OnHit(damageAmount, forward, false, caster);

            if (affectPlayers)
            {
                Pawn player;

                if (targetIdentity.TryGetComponent(out player) && player.Hurtbox != null)
                {
                    uint casterNetId = caster != null ? caster.netId : 0u;

                    player.Hurtbox.OnHit(
                        damageAmount,
                        0f,
                        forward,
                        null,
                        false,
                        false,
                        false,
                        casterNetId
                    );
                }
            }
        }

        public static void ClearProjectile(ProjectilePush projectile)
        {
            if (projectile == null)
                return;

            DamagedTargetsByProjectile.Remove(projectile.GetInstanceID());
        }

        private static bool TryMarkTargetDamaged(
            ProjectilePush projectile,
            NetworkIdentity targetIdentity
        )
        {
            if (projectile == null || targetIdentity == null)
                return false;

            int projectileId = projectile.GetInstanceID();
            uint targetId = targetIdentity.netId;

            HashSet<uint> damagedTargets;

            if (!DamagedTargetsByProjectile.TryGetValue(projectileId, out damagedTargets))
            {
                damagedTargets = new HashSet<uint>();
                DamagedTargetsByProjectile[projectileId] = damagedTargets;
            }

            if (damagedTargets.Contains(targetId))
                return false;

            damagedTargets.Add(targetId);
            return true;
        }

        private static bool IsAeroProjectile(ProjectilePush projectile)
        {
            Spell ownerSpell =
                HarmonyUtil.GetFieldValue<Spell>(
                    projectile,
                    "OwnerSpell"
                );

            if (ownerSpell == null)
                return false;

            return ownerSpell.GetType() == typeof(PushSpell);
        }

        private static bool IsLevitationBlastProjectile(ProjectilePush projectile)
        {
            Spell ownerSpell =
                HarmonyUtil.GetFieldValue<Spell>(
                    projectile,
                    "OwnerSpell"
                );

            if (ownerSpell == null)
                return false;

            return ownerSpell.GetType() == typeof(LevitationBlastSpell);
        }
    }

    [HarmonyPatch(typeof(ProjectilePush), "OnDestroy")]
    public class ProjectilePushDamageCleanupPatch
    {
        public static void Prefix(ProjectilePush __instance)
        {
            if (__instance == null)
                return;

            ProjectilePushDamagePatch.ClearProjectile(__instance);
        }
    }
}