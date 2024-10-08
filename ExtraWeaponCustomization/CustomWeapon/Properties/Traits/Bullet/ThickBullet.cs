using EWC.CustomWeapon.WeaponContext.Contexts;
using UnityEngine;
using System.Text.Json;
using EWC.Utils;
using System.Collections.Generic;
using System;
using Gear;
using Agents;
using Enemies;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class ThickBullet : 
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPostRayContext>
    {
        public float HitSize { get; set; } = 0f;

        private int _pierceCount = 0;

        private static RaycastHit s_rayHit;
        private const float SightCheckMinSize = 0.5f;

        public void Invoke(WeaponPostRayContext context)
        {
            if (HitSize == 0) return;

            context.Result = false;

            _pierceCount = CWC.Weapon.ArchetypeData.PiercingBullets ? CWC.Weapon.ArchetypeData.PiercingDamageCountLimit : 1;

            Vector3 wallPos; // Used to determine bounds for thick bullets and line of sight checks
            if (Physics.Raycast(Weapon.s_ray, out s_rayHit, context.Data.maxRayDist, LayerUtil.MaskWorld))
                wallPos = s_rayHit.point;
            else
                wallPos = Weapon.s_ray.origin + context.Data.fireDir * context.Data.maxRayDist;

            CheckCollisionInitial(context, wallPos);

            if (_pierceCount == 0) return;

            RaycastHit[] results = Physics.SphereCastAll(Weapon.s_ray, HitSize, (Weapon.s_ray.origin - wallPos).magnitude, LayerManager.MASK_MELEE_ATTACK_TARGETS);
            if (results.Length == 0) return;
            Array.Sort(results, DistanceCompare);

            foreach (RaycastHit hit in results)
            {
                if (hit.distance == 0) continue;

                IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(hit);
                if (damageable == null) continue;
                if (AlreadyHit(damageable, CWC.Gun!.m_damageSearchID))  continue;
                if (!CheckLineOfSight(hit.collider, hit.point + hit.normal * HitSize, wallPos, true)) continue;

                s_rayHit = hit;
                CheckDirectHit(ref s_rayHit);

                context.Data.RayHit = s_rayHit;
                if (BulletWeapon.BulletHit(context.Data.ToWeaponHitData(), true, 0, CWC.Gun!.m_damageSearchID, true))
                    _pierceCount--;
                
                if (_pierceCount <= 0) return;
            }
        }

        // Check for enemies within the initial sphere (if hitsize is big enough)
        private void CheckCollisionInitial(WeaponPostRayContext context, Vector3 wallPos)
        {
            Vector3 origin = Weapon.s_ray.origin;
            List<(EnemyAgent, RaycastHit hit)> hits = SearchUtil.GetHitsInRange(Weapon.s_ray, HitSize, 180f, CWC.Weapon.Owner.CourseNode);
            hits.Sort(SortUtil.SearchDistance);

            foreach (var pair in hits)
            {
                RaycastHit hit = pair.hit;
                if (AlreadyHit(hit.collider, CWC.Gun!.m_damageSearchID)) continue;
                if (!CheckLineOfSight(hit.collider, origin, wallPos)) continue;

                CheckDirectHit(ref hit);

                context.Data.RayHit = hit;
                if (BulletWeapon.BulletHit(context.Data.ToWeaponHitData(), true, 0, CWC.Gun!.m_damageSearchID, true))
                    _pierceCount--;

                if (_pierceCount <= 0) break;
            }

            List<RaycastHit> lockHits = SearchUtil.GetLockHitsInRange(Weapon.s_ray, HitSize, 180f);
            lockHits.Sort(SortUtil.RaycastDistance);

            foreach (var hit in lockHits)
            {
                if (AlreadyHit(hit.collider, CWC.Gun!.m_damageSearchID)) continue;
                if (!CheckLineOfSight(hit.collider, origin, wallPos, true)) continue;

                context.Data.RayHit = hit;
                if (BulletWeapon.BulletHit(context.Data.ToWeaponHitData(), true, 0, CWC.Gun!.m_damageSearchID, true))
                    _pierceCount--;

                if (_pierceCount <= 0) break;
            }
        }

        // Naive LOS check to ensure that some point on the bullet line can see the enemy
        private bool CheckLineOfSight(Collider collider, Vector3 startPos, Vector3 endPos, bool checkLock = false)
        {
            if (HitSize < SightCheckMinSize) return true;

            float remainingDist = (endPos - startPos).magnitude;
            float increment = Math.Max(0.1f, Math.Min(HitSize, remainingDist) / 10f);

            Vector3 colliderPos = collider.transform.position;
            Vector3 origin = startPos;

            float checkDistSqr = (origin - colliderPos).sqrMagnitude;
            float maxCheckDistSqr = checkDistSqr;
            int count = 0;
            while (remainingDist >= 0.1f && checkDistSqr <= maxCheckDistSqr)
            {
                if (!Physics.Linecast(origin, collider.transform.position, out s_rayHit, LayerUtil.MaskWorld))
                    return true;
                else if (checkLock && collider.gameObject.GetInstanceID() == s_rayHit.collider.gameObject.GetInstanceID())
                    return true;

                origin += Weapon.s_ray.direction * increment;
                checkDistSqr = (origin - colliderPos).sqrMagnitude;
                remainingDist -= increment;
                count++;
            }

            return false;
        }

        // Scans through body parts to see if the player is aiming directly at one
        private void CheckDirectHit(ref RaycastHit hit)
        {
            RaycastHit bestHit = new() { distance = float.MaxValue };
            Agent? baseAgent = DamageableUtil.GetDamageableFromCollider(hit.collider)?.GetBaseAgent();
            if (baseAgent == null) return;
            
            foreach (Collider collider in baseAgent.GetComponentsInChildren<Collider>())
            {
                if (collider.Raycast(Weapon.s_ray, out var tempHit, (collider.transform.position - Weapon.s_ray.origin).magnitude + 1f) && tempHit.distance < bestHit.distance)
                    bestHit = tempHit;
            }

            if (bestHit.distance != float.MaxValue)
                hit = bestHit;
        }

        private static int DistanceCompare(RaycastHit a, RaycastHit b)
        {
            if (a.distance == b.distance) return 0;
            return a.distance < b.distance ? -1 : 1;
        }

        private static bool AlreadyHit(IDamageable damageable, uint searchID)
        {
            return searchID != 0 && damageable.GetBaseDamagable()?.TempSearchID == searchID;
        }

        private static bool AlreadyHit(Collider collider, uint searchID)
        {
            return searchID != 0 && DamageableUtil.GetDamageableFromCollider(collider)?.GetBaseDamagable()?.TempSearchID == searchID;
        }

        public override IWeaponProperty Clone()
        {
            ThickBullet copy = new()
            {
                HitSize = HitSize
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "hitsize":
                case "size":
                    HitSize = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
