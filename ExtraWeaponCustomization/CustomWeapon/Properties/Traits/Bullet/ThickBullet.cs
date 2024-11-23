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
        public float HitSize { get; private set; } = 0f;

        private int _pierceCount = 0;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private const float SightCheckMinSize = 0.5f;

        public void Invoke(WeaponPostRayContext context)
        {
            if (HitSize == 0) return;

            context.Result = false;

            s_ray = Weapon.s_ray;
            _pierceCount = CWC.Weapon.ArchetypeData.PiercingBullets ? CWC.Weapon.ArchetypeData.PiercingDamageCountLimit : 1;
            var wallPierce = CWC.GetTrait<WallPierce>();
            bool hitWall;
            Vector3 wallPos; // Used to determine bounds for thick bullets and line of sight checks
            if ((hitWall = Physics.Raycast(s_ray, out RaycastHit wallRayHit, context.Data.maxRayDist, LayerUtil.MaskWorld)) && wallPierce == null)
                wallPos = wallRayHit.point;
            else
                wallPos = s_ray.origin + context.Data.fireDir * context.Data.maxRayDist;
            float maxDist = (s_ray.origin - wallPos).magnitude;

            CheckCollisionInitial(context, wallPos, wallPierce);

            if (_pierceCount == 0) return;

            RaycastHit[] results = Physics.SphereCastAll(s_ray, HitSize, maxDist, LayerUtil.MaskEnemyDynamic);
            if (results.Length != 0)
            {
                SortUtil.SortWithWeakspotBuffer(results);

                foreach (RaycastHit hit in results)
                {
                    if (hit.distance == 0) continue;

                    IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(hit);
                    if (damageable == null) continue;
                    if (AlreadyHit(damageable, CWC.Gun!.m_damageSearchID)) continue;

                    if (wallPierce?.IsTargetReachable(CWC.Weapon.Owner.CourseNode, damageable.GetBaseAgent()?.CourseNode) == false) continue;
                    if (wallPierce == null && !CheckLineOfSight(hit.collider, hit.point + hit.normal * HitSize, wallPos, true)) continue;

                    s_rayHit = hit;
                    CheckDirectHit(ref s_rayHit);

                    context.Data.RayHit = s_rayHit;
                    if (BulletHit(context.Data))
                        _pierceCount--;

                    if (_pierceCount <= 0) return;
                }
            }

            results = Physics.RaycastAll(s_ray, maxDist, LayerUtil.MaskFriendly);
            if (results.Length != 0)
            {
                Array.Sort(results, SortUtil.Rayhit);

                foreach (RaycastHit hit in results)
                {
                    IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(hit);
                    if (damageable == null) continue;
                    if (AlreadyHit(damageable, CWC.Gun!.m_damageSearchID)) continue;
                    if (wallPierce?.IsTargetReachable(CWC.Weapon.Owner.CourseNode, damageable.GetBaseAgent().CourseNode) == false) continue;

                    context.Data.RayHit = hit;
                    if (BulletHit(context.Data))
                        _pierceCount--;

                    if (_pierceCount <= 0) return;
                }
            }

            if (!hitWall) return;

            context.Data.RayHit = wallRayHit;
            BulletHit(context.Data);
        }

        // Check for enemies within the initial sphere (if hitsize is big enough)
        private void CheckCollisionInitial(WeaponPostRayContext context, Vector3 wallPos, WallPierce? wallPierce)
        {
            Vector3 origin = s_ray.origin;
            List<(EnemyAgent enemy, RaycastHit hit)> hits = SearchUtil.GetEnemyHitsInRange(s_ray, HitSize, 180f, CWC.Weapon.Owner.CourseNode);
            hits.Sort(SortUtil.EnemyRayhit);

            foreach (var pair in hits)
            {
                RaycastHit hit = pair.hit;
                if (AlreadyHit(hit.collider, CWC.Gun!.m_damageSearchID)) continue;
                if (wallPierce?.IsTargetReachable(CWC.Weapon.Owner.CourseNode, pair.enemy.CourseNode) == false) continue;
                if (wallPierce == null && !CheckLineOfSight(hit.collider, origin, wallPos)) continue;

                CheckDirectHit(ref hit);

                context.Data.RayHit = hit;
                if (BulletHit(context.Data))
                    _pierceCount--;

                if (_pierceCount <= 0) break;
            }

            List<RaycastHit> lockHits = SearchUtil.GetLockHitsInRange(s_ray, HitSize, 180f);
            lockHits.Sort(SortUtil.Rayhit);

            foreach (var hit in lockHits)
            {
                if (AlreadyHit(hit.collider, CWC.Gun!.m_damageSearchID)) continue;
                if (wallPierce == null && !CheckLineOfSight(hit.collider, origin, wallPos, true)) continue;

                context.Data.RayHit = hit;
                if (BulletHit(context.Data))
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
                else if (checkLock && collider.gameObject.Pointer == s_rayHit.collider.gameObject.Pointer)
                    return true;

                origin += s_ray.direction * increment;
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
                if (collider.Raycast(s_ray, out var tempHit, (collider.transform.position - s_ray.origin).magnitude + 1f) && tempHit.distance < bestHit.distance)
                    bestHit = tempHit;
            }

            if (bestHit.distance != float.MaxValue)
                hit = bestHit;
        }

        private static bool AlreadyHit(IDamageable damageable, uint searchID)
        {
            return searchID != 0 && damageable.GetBaseDamagable()?.TempSearchID == searchID;
        }

        private static bool AlreadyHit(Collider collider, uint searchID)
        {
            return searchID != 0 && DamageableUtil.GetDamageableFromCollider(collider)?.GetBaseDamagable()?.TempSearchID == searchID;
        }

        private bool BulletHit(HitData data) => BulletWeapon.BulletHit(data.Apply(Weapon.s_weaponRayData), true, 0, CWC.Gun!.m_damageSearchID, true);

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
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
