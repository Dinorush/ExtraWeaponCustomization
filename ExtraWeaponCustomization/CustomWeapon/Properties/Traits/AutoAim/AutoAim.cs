﻿using Agents;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class AutoAim : 
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponOwnerSetContext>,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponEnableContext>,
        IWeaponProperty<WeaponDisableContext>,
        IWeaponProperty<WeaponUpdateContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponPreStartFireContext>,
        IWeaponProperty<WeaponFireCancelContext>,
        IWeaponProperty<WeaponPreRayContext>
    {
        public bool HipActive { get; private set; } = false;
        public bool AimActive { get; private set; } = true;
        public float Angle { get; private set; } = 0f;
        public float Range { get; private set; } = 0f;
        public float LockTime { get; private set; } = 0f;
        public float LockDecayTime { get; private set; } = 0f;
        public bool StayOnTarget { get; private set; } = false;
        public bool ResetOnNewTarget { get; private set; } = false;
        public bool RequireLock { get; private set; } = false;
        public bool LockWhileEmpty { get; private set; } = true;
        public bool TagOnly { get; private set; } = false;
        public bool IgnoreInvisibility { get; private set; } = false;
        public TargetingMode TargetMode { get; private set; } = TargetingMode.Normal;
        public TargetingPriority TargetPriority { get; private set; } = TargetingPriority.Angle;
        public bool FavorLookPoint { get; private set; } = false;
        public bool HomingOnly { get; private set; } = false;

        public CrosshairHitIndicator? _reticle;
        private GameObject? _reticleHolder;
        private FPSCamera? _camera;
        private float _detectionTick;
        private EnemyAgent? _target;
        private bool _hasTarget = false;
        private float _progress;
        private float _lastUpdateTime;

        private float _weakspotDetectionTick;
        private Dam_EnemyDamageLimb? _weakspotLimb;
        private Transform? _weakspotTarget;
        private readonly List<Collider> _bodyList = new();
        private readonly List<(Collider collider, Dam_EnemyDamageLimb limb)> _weakspotList = new();

        private static Ray s_ray;
        private static RaycastHit s_raycastHit;

        private readonly Color _targetedColor = new(1.2f, 0.3f, 0.1f, 1f);
        private readonly Color _passiveLocked = new(0.8f, 0.3f, 0.2f, 1f);
        private readonly Color _passiveDetection = new(0.5f, 0.5f, 0.5f, 1f);
        private readonly Vector3 _targetedAngles = new(0f, 0f, 45f);
        private Coroutine? targetLostAnimator = null;

        public void Invoke(WeaponPreStartFireContext context)
        {
            context.Allow &= !RequireLock || UseAutoAim;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            // We don't want to stop burst weapons from firing mid-burst, but we do want to stop fully automatic weapons.
            if (CWC.Gun!.ArchetypeData.FireMode == eWeaponFireMode.Burst && !CWC.Gun!.m_archeType.BurstIsDone())
                return;

            context.Allow &= !RequireLock || UseAutoAim;
        }

        public void Invoke(WeaponPreRayContext context)
        {
            if (_camera == null || HomingOnly) return;

            // Ignore pierced shots
            if (context.Data.maxRayDist < CWC.Gun!.MaxRayDist) return;

            if (!UseAutoAim) return;

            // Prioritize aim if looking at the locked enemy
            if (FavorLookPoint)
            {
                s_ray.origin = _camera.Position;
                s_ray.direction = context.Data.fireDir;
                if (Physics.Raycast(s_ray, out s_raycastHit, 100f, LayerUtil.MaskEntityAndWorld3P))
                {
                    Agent? agent = DamageableUtil.GetDamageableFromRayHit(s_raycastHit)?.GetBaseAgent();
                    if (agent != null && agent.Alive && (!RequireLock || agent == _target))
                        return; // Cancel auto aim (just shoot where user is aiming)
                }
            }

            context.Data.fireDir = (GetTargetPos() - _camera.Position).normalized;
        }

        public void Invoke(WeaponSetupContext context)
        {
            _reticle = AutoAimReticle.Reticle;
            _reticleHolder = AutoAimReticle.ReticleHolder;
            OnEnable();
        }

        public void Invoke(WeaponOwnerSetContext _) => OnEnable();
        public void Invoke(WeaponEnableContext _) => OnEnable();

        public void Invoke(WeaponDisableContext _) => OnDisable();
        public void Invoke(WeaponClearContext _) => OnDisable();

        public void Invoke(WeaponUpdateContext _) 
        {
            if (_camera == null && CWC.Weapon.Owner != null)
                _camera = CWC.Weapon.Owner.FPSCamera;

            bool hasTarget = _hasTarget;
            UpdateDetection();
            if (LockDecayTime > 0)
            {
                if (hasTarget && !_hasTarget)
                    targetLostAnimator = CoroutineManager.StartCoroutine(AnimateReticleToCenter().WrapToIl2Cpp());
                else if(!hasTarget && _hasTarget && targetLostAnimator != null)
                {
                    CoroutineManager.StopCoroutine(targetLostAnimator);
                    targetLostAnimator = null;
                }
            }
            UpdateAnimate();
        }

        public void OnDisable()
        {
            _progress = 0f;
            _target = null;
            if (_reticle != null)
            {
                _reticle.transform.localScale = Vector3.zero;
                _reticle.SetVisible(false);
            }
        }

        public void OnEnable()
        {
            _reticle?.SetVisible(true);
        }

        private void UpdateDetection()
        {
            // If LockTime/LockDecayTime is 0, _progress is set straight to 0 or 1 immediately after target changes to prevent div by 0.
            if (_target != null && _progress < 1f)
                _progress = Mathf.Min(1f, _progress + (Clock.Time - _lastUpdateTime) / LockTime);
            else if (_target == null && _progress > 0f)
                _progress = Mathf.Max(0f, _progress - (Clock.Time - _lastUpdateTime) / LockDecayTime);

            _lastUpdateTime = Clock.Time;

            if (!CanLock)
            {
                if (LockDecayTime <= 0)
                    _progress = 0;
                _target = null;
                _hasTarget = false;
                return;
            }

            // If the target died, immediately re-acquire another one
            if (_hasTarget && (_target == null || !_target.Alive))
            {
                _target = null;
                _detectionTick = 0f;
            }

            if (_detectionTick >= Time.time) return;

            if (StayOnTarget && _target != null && !CheckTargetValid())
                _target = null;

            // Acquire new target if applicable
            if (_target == null || !StayOnTarget)
            {
                EnemyAgent? _lastTarget = _target;
                _target = CheckForTarget();
                _hasTarget = _target != null;

                if (_hasTarget && _target != _lastTarget)
                {
                    if (TargetMode == TargetingMode.Weakspot)
                        ResetWeakspotLists();
                    if (ResetOnNewTarget)
                        _progress = 0;
                }
            }

            // Prevents statements at the top of the func from doing div by 0
            if (_target == null && LockDecayTime <= 0)
                _progress = 0f;
            else if (_target != null && LockTime <= 0)
                _progress = 1f;

            _detectionTick = Time.time + Configuration.AutoAimTickDelay;
        }

        private void UpdateAnimate()
        {
            if (_target != null)
                _reticleHolder!.transform.position = _camera!.m_camera.WorldToScreenPoint(GetTargetPos());

            if (UseAutoAim)
            {
                _reticle!.m_hitColor = _targetedColor;
                _reticle!.transform.localScale = Vector3.one;
                _reticle.transform.localEulerAngles = _targetedAngles;
            }
            else if(LockedTarget)
            {
                _reticle!.m_hitColor = _passiveLocked;
                _reticle!.transform.localScale = Vector3.one;
                _reticle.transform.localEulerAngles += new Vector3(0, 0, 1f);
            }
            else
            {
                // Color and rotation speed depend on square progress, size uses linear. Makes a more noticeable end to the lock.
                float sqProgress = _progress * _progress;
                if (_target != null)
                    _reticle!.transform.localEulerAngles += new Vector3(0, 0, Mathf.Lerp(4, 1, sqProgress));
                else
                    _reticle!.transform.localEulerAngles += new Vector3(0, 0, 5);
                
                _reticle!.m_hitColor = GetLockingColor(sqProgress);
                _reticle.transform.localScale = Vector3.one * Mathf.Lerp(0, 1f, _progress);
            }

            _reticle.transform.localScale *= AutoAimActive ? 1.6f : 1f;
            _reticle.UpdateColorsWithAlphaMul(AutoAimActive ? 1.0f : 0.5f);
        }

        private IEnumerator AnimateReticleToCenter()
        {
            Vector3 startPos = _reticleHolder!.transform.position;
            Vector3 endPos = _camera!.m_camera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
            float startProgress = _progress;
            while(_progress > 0)
            {
                _reticleHolder.transform.position = Vector3.Lerp(endPos, startPos, _progress / startProgress);
                yield return null;
            }
            targetLostAnimator = null;
        }

        private Color GetLockingColor(float frac)
        {
            return new Color(
                    Mathf.Lerp(_passiveDetection.r, _passiveLocked.r, frac),
                    Mathf.Lerp(_passiveDetection.g, _passiveLocked.g, frac),
                    Mathf.Lerp(_passiveDetection.b, _passiveLocked.b, frac),
                    1f
                );
        }

        private bool HasAmmo => CWC.Gun!.GetCurrentClip() > 0 && CWC.Gun!.IsReloading == false;
        private bool CanLock => LockWhileEmpty || HasAmmo;
        private bool LockedTarget => _target != null && _progress == 1f;
        private bool AutoAimActive => HasAmmo && (
               AimActive == InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)
            || HipActive != InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)
            );
        public bool UseAutoAim => LockedTarget && AutoAimActive;

        public (EnemyAgent?, Dam_EnemyDamageLimb?) GetTargets() => (_target, _weakspotLimb);

        private bool CheckTargetValid()
        {
            if (_target == null || !_target.Alive) return false;
            if (!IgnoreInvisibility && (_target.RequireTagForDetection || TagOnly) && !_target.IsTagged) return false;

            // Check if any part of the target is still valid
            Vector3 position = _camera!.Position;
            Vector3 targetPos = GetTargetPos();
            if (Physics.Linecast(position, targetPos, LayerUtil.MaskWorldExcProj)) return false;

            foreach (var limb in _target.Damage.DamageLimbs)
            {
                if (limb.IsDestroyed == true) continue;

                Vector3 diff = limb.GetComponent<Collider>().ClosestPoint(position) - position;
                float sqrDist = diff.sqrMagnitude;
                if (sqrDist < Range * Range && Vector3.Angle(_camera!.CameraRayDir, diff) < Angle)
                    return true;
            }
            return false;
        }

        private EnemyAgent? CheckForTarget()
        {
            // s_ray may get changed by other functions, so to ensure it remains the same, use a local one
            Ray ray = new(_camera!.Position, _camera!.CameraRayDir);

            List<EnemyAgent> enemies = SearchUtil.GetEnemiesInRange(ray, Range, Angle, CWC.Weapon.Owner.CourseNode);

            List<(EnemyAgent, float)>? angleList = null; // Needed for later comparisons with current target
            switch (TargetPriority)
            {
                case TargetingPriority.Angle:
                    angleList = enemies.ConvertAll(enemy => (enemy, Vector3.Angle(ray.direction, GetSearchTargetPos(enemy) - ray.origin)));
                    angleList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(angleList, enemies);
                    break;
                case TargetingPriority.Distance:
                    var distList = enemies.ConvertAll(enemy => (enemy, (GetSearchTargetPos(enemy) - ray.origin).sqrMagnitude));
                    distList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(distList, enemies);
                    break;
                case TargetingPriority.Health:
                    // Since we prefer higher HealthMax, need to invert angles so the reverse gets the right order
                    var healthList = enemies.ConvertAll(enemy => (enemy, enemy.Damage.HealthMax, 180f - Vector3.Angle(ray.direction, GetSearchTargetPos(enemy) - ray.origin)));
                    healthList.Sort(SortUtil.FloatTuple);
                    healthList.Reverse();
                    SortUtil.CopySortedList(healthList, enemies);
                    break;
                default:
                    return null;
            };

            // New targets scanned don't check weakspots for performance.
            // Still, we want to consider current target's weakspots (since it's already cached and feels better).
            if (TargetPriority == TargetingPriority.Angle && CheckTargetValid())
            {
                float targetAngle = Vector3.Angle(ray.direction, GetTargetPos() - ray.origin);
                foreach ((EnemyAgent enemy, float angle) in angleList!)
                {
                    if (angle >= targetAngle) return _target;
                    if ((IgnoreInvisibility || (!enemy.RequireTagForDetection && !TagOnly) || enemy.IsTagged)
                     && !Physics.Linecast(ray.origin, GetSearchTargetPos(enemy), LayerUtil.MaskWorldExcProj))
                        return enemy;
                }
            }
            else
            {
                foreach (var enemy in enemies)
                    if ((IgnoreInvisibility || (!enemy.RequireTagForDetection && !TagOnly) || enemy.IsTagged)
                     && !Physics.Linecast(ray.origin, GetSearchTargetPos(enemy), LayerUtil.MaskWorldExcProj))
                        return enemy;
            }

            return null;
        }

        private Vector3 GetSearchTargetPos(EnemyAgent enemy)
        {
            return TargetMode == TargetingMode.Body ? enemy.AimTargetBody.position : enemy.AimTarget.position;
        }

        private Vector3 GetTargetPos()
        {
            if (_target == null) return Vector3.zero;

            UpdateWeakspotLimb();

            return TargetMode switch
            {
                TargetingMode.Body => _target.AimTargetBody.position,
                TargetingMode.Weakspot => _weakspotTarget!.position,
                _ => _target.AimTarget.position,
            };
        }

        private void ResetWeakspotLists()
        {
            _weakspotList.Clear();
            _bodyList.Clear();
            _weakspotLimb = null;
            if (_target == null) return;

            _weakspotTarget = _target.AimTarget;
            foreach (var limb in _target.Damage.DamageLimbs)
            {
                if (limb.m_health > 0)
                {
                    Collider collider = limb.GetComponent<Collider>();
                    if (limb.m_type == eLimbDamageType.Weakspot)
                        _weakspotList.Add((collider, limb));
                    else
                        _bodyList.Add(collider);
                }
            }
        }

        private void UpdateWeakspotLimb()
        {
            if (TargetMode != TargetingMode.Weakspot) return;

            if (_weakspotList.Count == 0)
            {
                _weakspotTarget = _target!.AimTarget;
                return;
            }

            if (_weakspotLimb != null && _weakspotLimb.m_health > 0 && Clock.Time < _weakspotDetectionTick) return;
            _weakspotDetectionTick = Clock.Time + Configuration.AutoAimTickDelay;

            _weakspotList.Sort(WeakspotCompare);
            _weakspotList.Reverse();

            for (int i = _weakspotList.Count - 1; i >= 0; i--)
            {
                Dam_EnemyDamageLimb weakspot = _weakspotList[i].limb;
                if (weakspot == null || weakspot.m_health <= 0)
                {
                    _weakspotList.RemoveAt(i);
                    continue;
                }
                Collider collider = _weakspotList[i].collider;

                s_ray.direction = weakspot.transform.position - _camera!.Position;
                s_ray.origin = _camera.Position;
                collider.Raycast(s_ray, out s_raycastHit, (weakspot.transform.position - _camera.Position).magnitude);

                if (!_bodyList.Any(collider => collider.Raycast(s_ray, out _, s_raycastHit.distance)) && !Physics.Linecast(_camera.Position, weakspot.transform.position, LayerUtil.MaskWorldExcProj))
                {
                    _weakspotTarget = collider.transform;
                    _weakspotLimb = weakspot;
                    return;
                }
            }
        }

        private int WeakspotCompare((Collider? collider, Dam_EnemyDamageLimb) x, (Collider? collider, Dam_EnemyDamageLimb) y)
        {
            if (x.collider == null) return 1;
            if (y.collider == null) return -1;
            float angleX = Vector3.Angle(_camera!.CameraRayDir, x.collider.transform.position);
            float angleY = Vector3.Angle(_camera!.CameraRayDir, y.collider.transform.position);
            return angleX < angleY ? -1 : 1;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteBoolean(nameof(HipActive), HipActive);
            writer.WriteBoolean(nameof(AimActive), AimActive);
            writer.WriteNumber(nameof(Angle), Angle);
            writer.WriteNumber(nameof(Range), Range);
            writer.WriteNumber(nameof(LockTime), LockTime);
            writer.WriteNumber(nameof(LockDecayTime), LockDecayTime);
            writer.WriteBoolean(nameof(StayOnTarget), StayOnTarget);
            writer.WriteBoolean(nameof(ResetOnNewTarget), ResetOnNewTarget);
            writer.WriteBoolean(nameof(RequireLock), RequireLock);
            writer.WriteBoolean(nameof(LockWhileEmpty), LockWhileEmpty);
            writer.WriteBoolean(nameof(TagOnly), TagOnly);
            writer.WriteBoolean(nameof(IgnoreInvisibility), IgnoreInvisibility);
            writer.WriteString(nameof(TargetMode), TargetMode.ToString());
            writer.WriteString(nameof(TargetPriority), TargetPriority.ToString());
            writer.WriteBoolean(nameof(FavorLookPoint), FavorLookPoint);
            writer.WriteBoolean(nameof(HomingOnly), HomingOnly);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "hipactive":
                case "hip":
                    HipActive = reader.GetBoolean();
                    break;
                case "aimactive":
                case "aim":
                    AimActive = reader.GetBoolean();
                    break;
                case "angle":
                    Angle = reader.GetSingle();
                    break;
                case "range":
                    Range = reader.GetSingle();
                    break;
                case "locktime":
                    LockTime = reader.GetSingle();
                    break;
                case "lockdecaytime":
                    LockDecayTime = reader.GetSingle();
                    break;
                case "stayontarget":
                    StayOnTarget = reader.GetBoolean();
                    break;
                case "resetonnewtarget":
                    ResetOnNewTarget = reader.GetBoolean();
                    break;
                case "requirelock":
                    RequireLock = reader.GetBoolean();
                    break;
                case "lockwhileempty":
                    LockWhileEmpty = reader.GetBoolean();
                    break;
                case "tagonly":
                    TagOnly = reader.GetBoolean();
                    break;
                case "ignoreinvisibility":
                case "ignoreinvis":
                    IgnoreInvisibility = reader.GetBoolean();
                    break;
                case "targetbody":
                    if (reader.GetBoolean())
                        TargetMode = TargetingMode.Body;
                    break;
                case "targetingmode":
                case "targetmode":
                    TargetMode = reader.GetString().ToEnum(TargetingMode.Normal);
                    break;
                case "targetingpriority":
                case "targetpriority":
                    TargetPriority = reader.GetString().ToEnum(TargetingPriority.Angle);
                    break;
                case "favorlookpoint":
                case "favourlookpoint":
                    FavorLookPoint = reader.GetBoolean();
                    break;
                case "homingonly":
                    HomingOnly = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
    
    public enum TargetingMode
    {
        Normal,
        Body,
        Weakspot
    }

    public enum TargetingPriority
    {
        Angle,
        Distance,
        Health
    }
}
