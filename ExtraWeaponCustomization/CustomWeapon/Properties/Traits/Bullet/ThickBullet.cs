using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using UnityEngine;
using System.Text.Json;
using ExtraWeaponCustomization.Utils;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using System.Collections.Generic;
using System;
using Gear;
using Agents;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class ThickBullet : 
        Trait,
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

            _pierceCount = context.Weapon.ArchetypeData.PiercingBullets ? context.Weapon.ArchetypeData.PiercingDamageCountLimit : 1;

            Vector3 wallPos; // Used to determine bounds for thick bullets and line of sight checks
            if (Physics.Raycast(Weapon.s_ray, out s_rayHit, context.Data.maxRayDist, EWCProjectileManager.MaskWorld))
                wallPos = s_rayHit.point;
            else
                wallPos = Weapon.s_ray.origin + context.Data.fireDir * context.Data.maxRayDist;

            CheckCollisionInitial(context, wallPos);

            if (_pierceCount == 0) return;

            RaycastHit[] results = Physics.SphereCastAll(Weapon.s_ray, HitSize, (Weapon.s_ray.origin - wallPos).magnitude, LayerManager.MASK_ENEMY_DAMAGABLE);
            if (results.Length == 0) return;
            Array.Sort(results, DistanceCompare);

            foreach (RaycastHit hit in results)
            {
                if (hit.distance == 0) continue;
                if (AlreadyHit(hit.collider, context.Weapon.m_damageSearchID)) continue;
                if (!CheckLineOfSight(hit.collider, hit.point + hit.normal * HitSize, wallPos)) continue;

                s_rayHit = hit;
                CheckDirectHit(ref s_rayHit);

                context.Data.rayHit = s_rayHit;
                if (BulletWeapon.BulletHit(context.Data, true, 0, context.Weapon.m_damageSearchID, true))
                    _pierceCount--;
                
                if (_pierceCount <= 0) return;
            }
        }

        // Check for enemies within the initial sphere (if hitsize is big enough)
        private void CheckCollisionInitial(WeaponPostRayContext context, Vector3 wallPos)
        {
            Ray ray = default;
            ray.origin = Weapon.s_ray.origin;
            Collider[] colliders = Physics.OverlapSphere(ray.origin, HitSize, LayerManager.MASK_ENEMY_DAMAGABLE);
            PriorityQueue<RaycastHit, float> hitQueue = new();

            foreach (var collider in colliders)
            {
                ray.direction = collider.transform.position - ray.origin;
                if (!collider.Raycast(ray, out RaycastHit hit, HitSize)) continue;

                hitQueue.Enqueue(hit, hit.distance);
            }

            while (hitQueue.TryDequeue(out s_rayHit, out _))
            {
                if (AlreadyHit(s_rayHit.collider, context.Weapon.m_damageSearchID)) continue;
                if (!CheckLineOfSight(s_rayHit.collider, ray.origin, wallPos)) continue;

                CheckDirectHit(ref s_rayHit);

                context.Data.rayHit = s_rayHit;
                if (BulletWeapon.BulletHit(context.Data, true, 0, context.Weapon.m_damageSearchID, true))
                    _pierceCount--;

                if (_pierceCount <= 0) break;
            }
        }

        // Naive LOS check to ensure that some point on the bullet line can see the enemy
        private bool CheckLineOfSight(Collider collider, Vector3 startPos, Vector3 endPos)
        {
            if (HitSize < SightCheckMinSize) return true;

            float remainingDist = (endPos - startPos).magnitude;
            float increment = Math.Max(0.1f, Math.Min(HitSize, remainingDist) / 10f);

            Vector3 colliderPos = collider.transform.position;
            Ray ray = Weapon.s_ray;
            ray.origin = startPos;

            float checkDist = (ray.origin - colliderPos).magnitude;
            float maxCheckDist = checkDist;
            int count = 0;
            while (remainingDist >= 0.1f && checkDist <= maxCheckDist)
            {
                ray.direction = collider.transform.position - ray.origin;
                if (!Physics.Raycast(ray, out var temp, checkDist, EWCProjectileManager.MaskWorld))
                    return true;

                ray.origin += Weapon.s_ray.direction * increment;
                checkDist = (ray.origin - colliderPos).magnitude;
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

        private static bool AlreadyHit(Collider collider, uint searchID)
        {
            return DamageableUtil.GetDamageableFromCollider(collider)?.GetBaseDamagable()?.TempSearchID == searchID;
        }

        public override IWeaponProperty Clone()
        {
            ThickBullet copy = new()
            {
                HitSize = HitSize
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(HitSize), HitSize);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
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
