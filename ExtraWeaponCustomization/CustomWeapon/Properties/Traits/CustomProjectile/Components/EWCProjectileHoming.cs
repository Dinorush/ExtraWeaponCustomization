using AIGraph;
using Enemies;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.Utils;
using EWC.Utils.Extensions;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileHoming
    {
        private readonly EWCProjectileComponentBase _base;

        private ProjectileHomingSettings _settings;
        private PlayerAgent? _owner;
        private eDimensionIndex _dimensionIndex;
        private WallPierce? _wallPierce;
        private bool _enabled = false;

        private bool _homingEnabled;
        private float _homingStartTime;
        private float _initialHomingEndTime;
        private EnemyAgent? _homingAgent;
        public EnemyAgent? HomingAgent
        {
            get => _homingAgent;
            private set
            {
                _homingAgent = value;
                if (_homingAgent == null)
                {
                    _homingLimb = null;
                    _homingTarget = null;
                }

                // Only local homing cares about finding new limbs to target
                if (_base.IsLocal && value != null)
                {
                    ResetWeakspotList();
                    UpdateHomingTarget();
                    EWCProjectileManager.DoProjectileTarget(_base.PlayerIndex, _base.SyncID, HomingAgent, (byte)(_homingLimb != null ? _homingLimb.m_limbID : 0));
                }
            }
        }

        private Dam_EnemyDamageLimb? _homingLimb;
        private Transform? _homingTarget;
        private readonly List<Dam_EnemyDamageLimb> _weakspotList = new();
        private bool _hadTarget;
        private float _nextSearchTime;

        private static Vector3 s_position;
        private static Vector3 s_dir;
        private readonly static Queue<AIG_CourseNode> s_nodeQueue = new();

#pragma warning disable CS8618 // Settings is set on Init call which will always run before it is used
        public EWCProjectileHoming(EWCProjectileComponentBase comp)
        {
            _base = comp;
        }
#pragma warning restore CS8618

        public void Init(Projectile projBase, Vector3 position, Vector3 dir)
        {
            if (_enabled) return;

            _enabled = true;
            _settings = projBase.HomingSettings;
            _wallPierce = null;
            _owner = null;

            _homingStartTime = Time.time + _settings.HomingDelay;
            _initialHomingEndTime = _homingStartTime + _settings.InitialHomingDuration;
            _homingEnabled = _settings.HomingEnabled;
            _nextSearchTime = 0f;
            HomingAgent = null;

            if (!_base.IsLocal) return;
            _owner = projBase.CWC.Weapon.Owner;
            _dimensionIndex = _owner.DimensionIndex;
            _wallPierce = projBase.CWC.GetTrait<WallPierce>();

            if (_homingEnabled)
            {
                s_position = position;
                s_dir = dir;
                if (_settings.SearchInitialMode == SearchMode.AutoAim)
                {
                    _homingEnabled = false;
                    AutoAim? autoAim = projBase.CWC.GetTrait<AutoAim>();
                    if (autoAim == null || !autoAim.UseAutoAim) return;

                    (EnemyAgent? target, Dam_EnemyDamageLimb? limb) = autoAim.GetTargets();
                    if (limb != null)
                    {
                        _homingLimb = limb;
                        _homingTarget = limb.transform;
                    }
                    HomingAgent = target;
                    _homingEnabled = true;
                }
                else
                {
                    if (_settings.SearchInitialMode == SearchMode.AimDir)
                        s_dir = _owner!.FPSCamera.Forward;
                    FindHomingAgent();
                    if (HomingAgent == null && _settings.SearchStopMode.HasFlag(StopSearchMode.Invalid))
                        _homingEnabled = false;
                }
            }
        }

        public void Update(Vector3 position, float deltaTime, ref Vector3 dir)
        {
            s_position = position;
            s_dir = dir;
            if (!_homingEnabled || Time.time < _homingStartTime || !UpdateHomingAgent()) return;

            UpdateDir(position, deltaTime, ref dir);          
        }

        public void UpdateDir(Vector3 position, float deltaTime, ref Vector3 dir)
        {
            if (!_homingEnabled || Time.time < _homingStartTime || HomingAgent == null) return;

            float strength = _settings.InitialHomingStrength;
            Vector3 diff = _homingTarget!.position - position;
            if (Time.time >= _initialHomingEndTime)
            {
                float distMod = diff.magnitude.Map(_settings.HomingMinDist, _settings.HomingMaxDist, 1f, 0f);
                strength = (float)(_settings.HomingStrength * Math.Pow(distMod, _settings.HomingDistExponent));
            }

            dir = Vector3.Slerp(dir, diff.normalized, Math.Min(strength * deltaTime, 1f));
        }

        public void Die()
        {
            _enabled = false;
            _weakspotList.Clear();
        }

        private bool UpdateHomingAgent()
        {
            if (!_base.IsLocal)
            {
                if (HomingAgent == null || !HomingAgent.Alive || HomingAgent.Damage.Health <= 0) return false;
                if (_homingLimb != null && !_homingLimb.IsDestroyed) return true;

                _homingTarget = GetHomingTarget(HomingAgent);
                _homingLimb = null;
                return true;
            }

            if (HomingAgent == null || !HomingAgent.Alive || HomingAgent.Damage.Health <= 0)
            {
                if (_hadTarget && _settings.SearchStopMode.HasFlag(StopSearchMode.Dead))
                {
                    _homingEnabled = false;
                    return false;
                }

                FindHomingAgent();
                if (HomingAgent == null && _settings.SearchStopMode.HasFlag(StopSearchMode.Invalid))
                {
                    _homingEnabled = false;
                    return false;
                }

                return HomingAgent != null;
            }

            if (HomingAgent != null)
            {
                UpdateHomingTarget();
                return true;
            }
            return false;
        }

        public void SetHomingAgent(EnemyAgent? agent, Dam_EnemyDamageLimb? limb)
        {
            HomingAgent = agent;
            if (agent != null)
            {
                _homingLimb = limb;
                _homingTarget = limb != null ? limb.transform : GetHomingTarget(agent);
            }
        }

        public void UpdateOnPierce()
        {
            if (_settings.SearchStayOnTarget) return;

            if (_settings.SearchStopMode.HasFlag(StopSearchMode.Pierce))
            {
                _homingEnabled = false;
                EWCProjectileManager.DoProjectileTarget(_base.PlayerIndex, _base.SyncID, null, 0);
                return;
            }

            FindHomingAgent();
            if (HomingAgent == null)
                EWCProjectileManager.DoProjectileTarget(_base.PlayerIndex, _base.SyncID, null, 0);
        }

        public void FindHomingAgent()
        {
            if (!_hadTarget && _nextSearchTime > Time.time)
            {
                HomingAgent = null;
                return;
            }

            _nextSearchTime = Time.time + Math.Max(Configuration.HomingTickDelay, _settings.SearchCooldown);
            _hadTarget = false;
            HomingAgent = null;
            _weakspotList.Clear();
            Ray ray = new(s_position, s_dir);
            SearchUtil.DupeCheckSet = _base.Hitbox.HitEnts;
            List<EnemyAgent> enemies = SearchUtil.GetEnemiesInRange(ray, _settings.SearchRange, _settings.SearchAngle, CourseNodeUtil.GetCourseNode(s_position, _dimensionIndex), SearchSetting.IgnoreDupes);

            switch (_settings.TargetPriority)
            {
                case TargetingPriority.Angle:
                    var angleList = enemies.ConvertAll(enemy => (enemy, Vector3.Angle(ray.direction, GetHomingTargetPos(enemy) - ray.origin)));
                    angleList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(angleList, enemies);
                    break;
                case TargetingPriority.Distance:
                    var distList = enemies.ConvertAll(enemy => (enemy, (GetHomingTargetPos(enemy) - ray.origin).sqrMagnitude));
                    distList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(distList, enemies);
                    break;
                case TargetingPriority.Health:
                    // Since we prefer higher HealthMax, need to invert angles so the reverse gets the right order
                    var healthList = enemies.ConvertAll(enemy => (enemy, enemy.Damage.HealthMax, 180f - Vector3.Angle(ray.direction, GetHomingTargetPos(enemy) - ray.origin)));
                    healthList.Sort(SortUtil.FloatTuple);
                    healthList.Reverse();
                    SortUtil.CopySortedList(healthList, enemies);
                    break;
                default:
                    return;
            };

            EnemyAgent? target = null;
            foreach (var enemy in enemies)
            {
                if (!_settings.SearchIgnoreInvisibility && (enemy.RequireTagForDetection || _settings.SearchTagOnly) && !enemy.IsTagged) continue;
                if (!_settings.SearchIgnoreWalls && Physics.Linecast(ray.origin, GetHomingTargetPos(enemy), LayerUtil.MaskWorldExcProj)) continue;
                if (_settings.SearchIgnoreWalls && !IsTargetReachable(_owner!.CourseNode, enemy.CourseNode)) continue;

                target = enemy;
                break;
            }

            if (target == null) return;

            _hadTarget = true;
            HomingAgent = target;
            byte limbID = (byte)(_homingLimb != null ? (byte)_homingLimb.m_limbID : 0);
            EWCProjectileManager.DoProjectileTarget(_base.PlayerIndex, _base.SyncID, _homingAgent, limbID);
        }

        private void ResetWeakspotList()
        {
            if (_settings.TargetMode != TargetingMode.Weakspot) return;

            _weakspotList.Clear();
            foreach (Dam_EnemyDamageLimb limb in _homingAgent!.Damage.DamageLimbs)
                if (!limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot && limb.GetComponent<Collider>() != null)
                    _weakspotList.Add(limb);
        }

        private void UpdateHomingTarget()
        {
            if (_homingLimb != null && !_homingLimb.IsDestroyed) return;

            if (_settings.TargetMode != TargetingMode.Weakspot || _weakspotList.Count == 0)
            {
                _homingLimb = null;
                _homingTarget = GetHomingTarget(_homingAgent!);
                return;
            }

            _weakspotList.Sort(WeakspotCompare);
            _weakspotList.Reverse();
            for (int i = _weakspotList.Count - 1; i >= 0; i--)
            {
                Dam_EnemyDamageLimb weakspot = _weakspotList[i];
                if (weakspot == null || !weakspot.IsDestroyed)
                {
                    _weakspotList.RemoveAt(i);
                    continue;
                }

                _homingLimb = _weakspotList[i];
                _homingTarget = _homingLimb.transform;
                return;
            }

            if (_weakspotList.Count > 0)
            {
                _homingLimb = _weakspotList[0];
                _homingTarget = _homingLimb.transform;
            }
            else
            {
                _homingTarget = _homingAgent!.AimTarget;
                _homingLimb = null;
            }
        }

        private int WeakspotCompare(Dam_EnemyDamageLimb x, Dam_EnemyDamageLimb y)
        {
            float angleX = Vector3.Angle(s_dir, x.DamageTargetPos - s_position);
            float angleY = Vector3.Angle(s_dir, y.DamageTargetPos - s_position);
            if (angleX == angleY) return 0;
            return angleX < angleY ? -1 : 1;
        }
        private Transform GetHomingTarget(EnemyAgent enemy)
        {
            return _settings.TargetMode == TargetingMode.Body ? enemy.AimTargetBody : enemy.AimTarget;
        }
        private Vector3 GetHomingTargetPos(EnemyAgent enemy)
        {
            return GetHomingTarget(enemy).position;
        }

        private bool IsTargetReachable(AIG_CourseNode? source, AIG_CourseNode? target)
        {
            if (source == null || target == null) return false;
            if (source.NodeID == target.NodeID) return true;

            AIG_SearchID.IncrementSearchID();
            ushort searchID = AIG_SearchID.SearchID;
            s_nodeQueue.Enqueue(source);

            while (s_nodeQueue.Count > 0)
            {
                AIG_CourseNode current = s_nodeQueue.Dequeue();
                current.m_searchID = searchID;
                foreach (AIG_CoursePortal portal in current.m_portals)
                {
                    iLG_Door_Core? door = portal.m_door;
                    if (_wallPierce?.RequireOpenPath == true && door != null && door.DoorType != eLG_DoorType.Security && door.DoorType != eLG_DoorType.Apex)
                        door = null;
                    if (door != null && door.LastStatus != eDoorStatus.Open && door.LastStatus != eDoorStatus.Opening && door.LastStatus != eDoorStatus.Destroyed)
                        continue;

                    AIG_CourseNode nextNode = portal.GetOppositeNode(current);
                    if (nextNode.NodeID == target.NodeID)
                    {
                        s_nodeQueue.Clear();
                        return true;
                    }
                    if (nextNode.m_searchID == searchID) continue;
                    s_nodeQueue.Enqueue(nextNode);
                }
            }
            s_nodeQueue.Clear();
            return false;
        }
    }
}
