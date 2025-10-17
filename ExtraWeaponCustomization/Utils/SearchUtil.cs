using Agents;
using AIGraph;
using Enemies;
using EWC.Utils.Structs;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using SVector3 = System.Numerics.Vector3;

namespace EWC.Utils
{
    [Flags]
    internal enum SearchSetting
    {
        None = 0,
        Alloc = 1,
        ClosestHit = 1 << 1,
        CheckLOS = 1 << 2,
        CheckDoors = 1 << 3,
        CheckOwner = 1 << 4,
        CheckFriendly = 1 << 5,
        IgnoreDupes = 1 << 6
    }

    internal static class SearchUtil
    {
        private static readonly List<EnemyAgent> s_enemyCache = new();
        private static readonly List<(EnemyAgent, RaycastHit)> s_combinedCache = new();
        private static readonly List<(PlayerAgent, RaycastHit)> s_combinedCachePlayer = new();
        private static readonly Queue<AIG_CourseNode> s_nodeQueue = new();

        private static readonly List<RaycastHit> s_rayHitCache = new();

        public static HashSet<IntPtr>? DupeCheckSet;
        public static int SightBlockLayer = 0;
        public const float WeakspotBufferDist = 0.1f;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private const float Epsilon = 1e-5f;

        // Bounds.ClosestPoint doesn't actually return the closest point
        private static Vector3 ClosestPointOnBounds(Bounds bounds, Vector3 point)
        {
            return new(
                Math.Clamp(point.x, bounds.min.x, bounds.max.x),
                Math.Clamp(point.y, bounds.min.y, bounds.max.y),
                Math.Clamp(point.z, bounds.min.z, bounds.max.z)
                );
        }

        private static int GetMaxSteps(float dist) => dist <= 0.5f ? 1 : (int)Math.Ceiling(Math.Max(3d, 2d * Math.Log2(dist)));
        private static float GetStepDist(float dist, int step, int maxSteps) => maxSteps == 1 ? dist / 2f : dist * step / (maxSteps - 1);

        private static bool BoundsInRange(Ray il2Ray, float range, float angle, Bounds il2Bounds)
        {
            // Caching il2cpp structs to system structs for efficiency (hopefully).
            float sqrRange = range * range;
            SVector3 origin = new(il2Ray.origin.x, il2Ray.origin.y, il2Ray.origin.z);
            SVector3 dir = new(il2Ray.direction.x, il2Ray.direction.y, il2Ray.direction.z);
            SBounds bounds = il2Bounds;

            // Early exit: Box is not in range.
            SVector3 diff = bounds.ClosestPoint(origin) - origin;
            if (diff.LengthSquared() >= sqrRange) return false;

            // Early pass: No angle check is necessary or the cone is inside the box.
            if (angle >= 180f || bounds.Contains(origin)) return true;

            // Early exit: Box is behind the cone with angle <= 90.
            if (angle <= 90f)
            {
                diff = bounds.center - origin;
                float distToBox = SVector3.Dot(dir, diff);
                if (distToBox < 0)
                {
                    float lenInDir = bounds.extents.X * Math.Abs(dir.X) + bounds.extents.Y * Math.Abs(dir.Y) + bounds.extents.Z * Math.Abs(dir.Z);
                    if (distToBox + lenInDir < 0) return false;
                }
            }

            // Early pass: Cone ray directly hits the box.
            if (il2Bounds.IntersectRay(il2Ray, out float dist) && dist < range) return true;

            // Final computation: Iterate over points along the box's edges and compare angle/dist.
            float minDot = (float)Math.Cos(angle * Math.PI / 180f);

            Span<float> mins = stackalloc[] { bounds.min.X, bounds.min.Y, bounds.min.Z };
            Span<float> sizes = stackalloc[] { bounds.size.X, bounds.size.Y, bounds.size.Z };
            Span<int> steps = stackalloc[] { GetMaxSteps(sizes[0]), GetMaxSteps(sizes[1]), GetMaxSteps(sizes[2]) };
            Span<int> minSteps = stackalloc[] { Math.Min(steps[0], 2), Math.Min(steps[1], 2), Math.Min(steps[2], 2) };
            Span<float> point = stackalloc float[3];
            // Corners
            for (int i = 0; i < minSteps[0]; i++)
            {
                point[0] = mins[0] + GetStepDist(sizes[0], i, minSteps[0]);
                for (int j = 0; j < minSteps[1]; j++)
                {
                    point[1] = mins[1] + GetStepDist(sizes[1], j, minSteps[1]);
                    for (int k = 0; k < minSteps[2]; k++)
                    {
                        point[2] = mins[2] + GetStepDist(sizes[2], k, minSteps[2]);
                        diff.X = point[0] - origin.X;
                        diff.Y = point[1] - origin.Y;
                        diff.Z = point[2] - origin.Z;
                        if (diff.LengthSquared() < sqrRange && SVector3.Dot(dir, SVector3.Normalize(diff)) >= minDot)
                            return true;
                    }
                }
            }

            // Edges - Fix two axes, then iterate over the remaining axis.
            for (int axis = 0; axis < 3; axis++)
            {
                int fixedAxis1 = (axis + 1) % 3, fixedAxis2 = (axis + 2) % 3;
                for (int i = 1; i < steps[axis] - 1; i++)
                {
                    point[axis] = mins[axis] + GetStepDist(sizes[axis], i, steps[axis]);
                    for (int j = 0; j < minSteps[fixedAxis1]; j++)
                    {
                        point[fixedAxis1] = mins[fixedAxis1] + GetStepDist(sizes[fixedAxis1], j, minSteps[fixedAxis1]);
                        for (int k = 0; k < minSteps[fixedAxis2]; k++)
                        {
                            point[fixedAxis2] = mins[fixedAxis2] + GetStepDist(sizes[fixedAxis2], k, minSteps[fixedAxis2]);
                            diff.X = point[0] - origin.X;
                            diff.Y = point[1] - origin.Y;
                            diff.Z = point[2] - origin.Z;
                            if (diff.LengthSquared() < sqrRange && SVector3.Dot(dir, SVector3.Normalize(diff)) >= minDot)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool RaycastEnsured(Collider collider, float range, out RaycastHit hit)
        {
            // This should rarely ever fail
            if (collider.Raycast(s_ray, out hit, range)) return true;

            // It's probably inside the collider but I don't really know.
            s_ray.origin = s_ray.GetPoint(-5f);
            if (collider.Raycast(s_ray, out hit, 5f)) return true;

            // Shouldn't be here ever, give up hope on the original direction.
            Vector3 oldDir = s_ray.direction;
            s_ray.direction = collider.bounds.center - s_ray.origin;
            if (collider.Raycast(s_ray, out hit, 10f)) return true;

            // All hope is lost.
            EWCLogger.Warning($"Attempted to get a raycast to {collider.name} but failed! Pos: {s_ray.origin}, Dir: {oldDir.ToDetailedString()}, Collider center: {collider.bounds.center}, Diff to collider center: {s_ray.direction.ToDetailedString()}");
            return false;
        }

        private static List<Collider> GetValidColliders(Ray ray, float range, float angle, Agent agent)
        {
            var collidersIL = agent.GetComponentsInChildren<Collider>();
            List<Collider> colliders = new(collidersIL.Length);
            AgentType type = agent.Type;
            foreach (var collider in collidersIL)
            {
                if (type == AgentType.Enemy)
                {
                    Dam_EnemyDamageLimb? limb = collider.GetComponent<Dam_EnemyDamageLimb>();
                    if (limb == null || limb.IsDestroyed) continue;
                }
                else if (type == AgentType.Player && collider.GetComponent<IDamageable>() == null)
                    continue;

                if (BoundsInRange(ray, range, angle, collider.bounds))
                    colliders.Add(collider);
            }

            return colliders;
        }

        public static bool IsAgentInCone(Ray ray, float range, float angle, Agent agent, out RaycastHit hit, SearchSetting settings = SearchSetting.None)
        {
            hit = default;
            if (agent == null || !agent.Alive) return false;

            if (settings.HasFlag(SearchSetting.IgnoreDupes))
            {
                if (agent.Type == AgentType.Enemy && DupeCheckSet?.Contains(agent.Cast<EnemyAgent>().Damage.Pointer) == true)
                    return false;
                else if (agent.Type == AgentType.Player && DupeCheckSet?.Contains(agent.Cast<PlayerAgent>().Damage.Pointer) == true)
                    return false;
            }

            var colliders = GetValidColliders(ray, range, angle, agent);
            if (colliders.Count == 0) return false;

            s_ray.origin = ray.origin;
            float sqrRange = range * range;
            float minDist = sqrRange;
            Collider? minCollider = null;
            bool casted = false;
            AgentType type = agent.Type;

            foreach (var collider in colliders)
            {
                Vector3 trgtPos = collider.ClosestPoint(ray.origin);
                Vector3 diff = trgtPos - ray.origin;
                float sqrDist = diff.sqrMagnitude;
                float origDist = sqrDist;
                if (settings.HasFlag(SearchSetting.ClosestHit) && type == AgentType.Enemy && sqrDist < sqrRange && collider.GetComponent<Dam_EnemyDamageLimb>().m_type == eLimbDamageType.Weakspot)
                {
                    float newDist = Math.Max(diff.magnitude - WeakspotBufferDist, 0);
                    sqrDist = newDist * newDist;
                }

                if (sqrDist < minDist) // Skipping angle check - bounds does it well enough
                {
                    if (settings.HasFlag(SearchSetting.CheckLOS) && Physics.Linecast(ray.origin, trgtPos, SightBlockLayer)) continue;

                    minDist = sqrDist;
                    minCollider = collider;
                    // If not caching a hit to the closest collider, can just add the enemy as soon as one is valid
                    if (!settings.HasFlag(SearchSetting.ClosestHit)) break;

                    s_ray.direction = diff;

                    // If the distance is close to 0, need different logic since raycast will break
                    if (minDist < Epsilon)
                    {
                        if (origDist > Epsilon) break; // Raycast won't break, but we can stop searching for the closest collider
                        s_ray.origin -= ray.direction * Math.Min(0.1f, range / 2);
                        s_ray.direction = trgtPos - s_ray.origin;
                        if (RaycastEnsured(collider, range, out hit))
                        {
                            hit.point = trgtPos;
                            hit.distance = 0;
                            casted = true;
                        }
                        else
                            minCollider = null;

                        break; // Can't get lower than 0 distance
                    }
                }
            }
            if (minCollider == null) return false;
            if (settings.HasFlag(SearchSetting.ClosestHit) && !casted && !RaycastEnsured(minCollider, range, out hit))
                return false;
            return true;
        }

        private static void CacheEnemiesInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings)
        {
            AIG_SearchID.IncrementSearchID();
            ushort searchID = AIG_SearchID.SearchID;
            s_nodeQueue.Enqueue(origin);
            origin.m_searchID = searchID;
            CacheEnemiesInRangeFromQueue(ray, range, angle, settings);
        }

        private static void CacheEnemiesInRangeFromQueue(Ray ray, float range, float angle, SearchSetting settings)
        {
            ushort searchID = AIG_SearchID.SearchID;
            float sqrRange = range * range;
            s_combinedCache.Clear();
            while (s_nodeQueue.TryDequeue(out AIG_CourseNode? node))
            {
                foreach (AIG_CoursePortal portal in node.m_portals)
                {
                    AIG_CourseNode oppositeNode = portal.GetOppositeNode(node);
                    if (settings.HasFlag(SearchSetting.CheckDoors) && !portal.IsTraversable)
                        continue;

                    if (oppositeNode.m_searchID != searchID && BoundsInRange(ray, range, angle, portal.m_cullPortal.m_portalBounds))
                    {
                        oppositeNode.m_searchID = searchID;
                        s_nodeQueue.Enqueue(oppositeNode);
                    }
                }

                foreach (EnemyAgent enemy in node.m_enemiesInNode)
                {
                    if (enemy == null || !enemy.Alive || enemy.Damage.Health <= 0) continue;
                    if ((ClosestPointOnBounds(enemy.MovingCuller.Culler.Bounds, ray.origin) - ray.origin).sqrMagnitude > sqrRange) continue;
                    if (!IsAgentInCone(ray, range, angle, enemy, out s_rayHit, settings)) continue;
                    s_combinedCache.Add((enemy, s_rayHit));
                }
            }
        }

        public static List<EnemyAgent> GetEnemiesInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings = SearchSetting.None)
        {
            s_enemyCache.Clear();
            if (range == 0 || angle == 0)
                return settings.HasFlag(SearchSetting.Alloc) ? new List<EnemyAgent>() : s_enemyCache;

            CacheEnemiesInRange(ray, range, angle, origin, settings);
            if (settings.HasFlag(SearchSetting.Alloc))
                return s_combinedCache.ConvertAll(pair => pair.Item1);

            foreach ((EnemyAgent enemy, _) in s_combinedCache)
                s_enemyCache.Add(enemy);
            return s_enemyCache;
        }

        public static List<(EnemyAgent enemy, RaycastHit hit)> GetEnemyHitsInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings = SearchSetting.ClosestHit)
        {
            if (range == 0 || angle == 0)
            {
                s_combinedCache.Clear();
                return settings.HasFlag(SearchSetting.Alloc) ? new List<(EnemyAgent, RaycastHit)>() : s_combinedCache;
            }

            settings |= SearchSetting.ClosestHit;
            CacheEnemiesInRange(ray, range, angle, origin, settings);
            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_combinedCache);

            return s_combinedCache;
        }

        public static List<RaycastHit> GetLockHitsInRange(Ray ray, float range, float angle, SearchSetting settings = SearchSetting.None)
        {
            s_rayHitCache.Clear();
            if (range == 0 || angle == 0)
                return settings.HasFlag(SearchSetting.Alloc) ? new List<RaycastHit>() : s_rayHitCache;

            Collider[] colliders = Physics.OverlapSphere(ray.origin, range, LayerUtil.MaskDynamic);
            Vector3 origDir = ray.direction;
            foreach (Collider collider in colliders)
            {
                IDamageable? damageable = DamageableUtil.GetDamageableFromCollider(collider);
                if (damageable == null) continue;

                if (settings.HasFlag(SearchSetting.IgnoreDupes) && DupeCheckSet?.Contains(damageable.GetBaseDamagable().Pointer) == true) continue;

                if (settings.HasFlag(SearchSetting.CheckLOS)
                    && Physics.Linecast(ray.origin, damageable.DamageTargetPos, out s_rayHit, SightBlockLayer)
                    && s_rayHit.collider.gameObject.Pointer != collider.gameObject.Pointer)
                        continue;

                ray.direction = damageable.DamageTargetPos - ray.origin;
                if (collider.Raycast(ray, out s_rayHit, range) && Vector3.Angle(ray.direction, origDir) < angle)
                    s_rayHitCache.Add(s_rayHit);
            }

            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_rayHitCache);

            return s_rayHitCache;
        }

        public static List<(PlayerAgent player, RaycastHit hit)> GetPlayerHitsInRange(Ray ray, float range, float angle, SearchSetting settings = SearchSetting.CheckFriendly | SearchSetting.CheckOwner)
        {
            s_combinedCachePlayer.Clear();
            if (range == 0 || angle == 0)
                return settings.HasFlag(SearchSetting.Alloc) ? new List<(PlayerAgent, RaycastHit)>() : s_combinedCachePlayer;

            float sqrRange = range * range;
            foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
            {
                if (player == null || !player.Alive) continue;
                if ((ClosestPointOnBounds(player.m_movingCuller.Culler.Bounds, ray.origin) - ray.origin).sqrMagnitude > sqrRange) continue;
                if (player.IsLocallyOwned)
                {
                    // Local players have one collider; checking LoS can easily put the floor as the closest point, so this uses a custom position
                    if (!settings.HasFlag(SearchSetting.CheckOwner)) continue;
                    s_ray.origin = ray.origin;
                    s_ray.direction = player.Damage.DamageTargetPos - ray.origin;
                    if (!player.GetComponent<Collider>().Raycast(s_ray, out s_rayHit, range)) continue;
                    if (settings.HasFlag(SearchSetting.CheckLOS) && Physics.Linecast(ray.origin, s_rayHit.point, SightBlockLayer)) continue;
                }
                else if (!settings.HasFlag(SearchSetting.CheckFriendly) || !IsAgentInCone(ray, range, angle, player, out s_rayHit, settings))
                    continue;

                s_combinedCachePlayer.Add((player, s_rayHit));
            }

            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_combinedCachePlayer);

            return s_combinedCachePlayer;
        }

        public static List<RaycastHit> RaycastAll(Ray ray, float maxDist, int layerMask, SearchSetting settings = SearchSetting.None)
        {
            if (settings.HasFlag(SearchSetting.CheckLOS) && Physics.Raycast(ray, out s_rayHit, maxDist, SightBlockLayer))
                maxDist = s_rayHit.distance;

            s_rayHitCache.Clear();
            DamageUtil.IncrementSearchID();
            var searchID = DamageUtil.SearchID;
            float totalMoved = 0;
            while (Physics.Raycast(ray, out s_rayHit, maxDist, layerMask))
            {
                float move = s_rayHit.distance + 0.1f;
                IDamageable? damageable = s_rayHit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    var baseDam = damageable.GetBaseDamagable();
                    if (baseDam.TempSearchID != searchID && (!settings.HasFlag(SearchSetting.IgnoreDupes) || DupeCheckSet?.Contains(baseDam.Pointer) != true))
                    {
                        s_rayHit.distance += totalMoved; // Reflect the true distance of the hit
                        s_rayHitCache.Add(s_rayHit);
                        baseDam.TempSearchID = searchID;
                    }
                }
                totalMoved += move;
                ray.origin += ray.direction * move;
                maxDist -= move;
            }

            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_rayHitCache);

            return s_rayHitCache;
        }
    }
}
