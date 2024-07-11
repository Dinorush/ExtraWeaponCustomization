using Agents;
using Enemies;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using System;
using System.Collections;
using System.Text.Json;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class AutoAim : 
        Trait,
        IWeaponProperty<WeaponPostSetupContext>,
        IWeaponProperty<WeaponPreStartFireContext>,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponPreRayContext>
    {
        public bool HipActive { get; set; } = false;
        public bool AimActive { get; set; } = true;
        public float Angle { get; set; } = 0f;
        public float Range { get; set; } = 0f;
        public float LockTime { get; set; } = 0f;
        public float LockDecayTime { get; set; } = 0f;
        public bool StayOnTarget { get; set; } = false;
        public bool ResetOnNewTarget { get; set; } = false;
        public bool RequireLock { get; set; } = false;
        public bool LockWhileEmpty { get; set; } = true;
        public bool TagOnly { get; set; } = false;
        public bool IgnoreInvisibility { get; set; } = false;
        public bool TargetBody { get; set; } = false;
        public bool FavorLookPoint { get; set; } = false;

        public CrosshairHitIndicator? _reticle;
        private GameObject? _reticleHolder;
        private BulletWeapon? _weapon;
        private Camera? _camera;
        private float _detectionTick;
        private EnemyAgent? _target;
        private bool _hasTarget = false;
        private float _progress;
        private float _lastUpdateTime;

        private static readonly ColliderComparer _colliderComparer = new();

        private static Ray _ray;
        private static RaycastHit _raycastHit;

        private readonly Color _targetedColor = new(1.2f, 0.3f, 0.1f, 1f);
        private readonly Color _passiveLocked = new(0.8f, 0.3f, 0.2f, 1f);
        private readonly Color _passiveDetection = new(0.5f, 0.5f, 0.5f, 1f);
        private readonly Vector3 _targetedAngles = new(0f, 0f, 45f);
        private Coroutine? targetLostAnimator = null;

        public void Invoke(WeaponPreStartFireContext context)
        {
            context.Allow &= !RequireLock || UseAutoAim;
        }

        public void Invoke(WeaponPreFireContext context)
        {
            // We don't want to stop burst weapons from firing mid-burst, but we do want to stop fully automatic weapons.
            if (context.Weapon.ArchetypeData.FireMode == eWeaponFireMode.Burst && !context.Weapon.m_archeType.BurstIsDone())
                return;

            context.Allow &= !RequireLock || UseAutoAim;
        }

        public void Invoke(WeaponPreRayContext context)
        {
            // Prioritize aim if looking at the locked enemy
            if (FavorLookPoint && _camera != null)
            {
                _ray.origin = _camera.transform.position;
                _ray.direction = context.Data.fireDir;
                if (Physics.Raycast(_ray, out _raycastHit, 100f, LayerManager.MASK_BULLETWEAPON_RAY))
                {
                    Agent? agent = WeaponTriggerContext.GetDamageableFromRayHit(_raycastHit)?.GetBaseAgent();
                    if (agent != null && agent.Alive && agent == _target)
                        return; // Cancel auto aim (just shoot where user is aiming)
                }
            }

            if (UseAutoAim)
            {
                Vector3 trgtPos = TargetBody ? _target!.AimTargetBody.position : _target!.AimTarget.position;
                context.Data.fireDir = (trgtPos - context.Data.owner.FPSCamera.Position).normalized;
            }
        }

        public void Invoke(WeaponPostSetupContext context)
        {
            _weapon = context.Weapon;

            _reticle = AutoAimReticle.Reticle;
            _reticle.SetVisible(true);
            _reticleHolder = AutoAimReticle.ReticleHolder;
        }

        public void Update() 
        {
            if (_weapon == null) return;

            if (_camera == null)
                _camera = _weapon.Owner?.FPSCamera?.m_camera;

            bool hasTarget = _hasTarget;
            UpdateDetection();
            if (LockDecayTime > 0)
            {
                if (hasTarget && !_hasTarget)
                    targetLostAnimator = CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(AnimateReticleToCenter()));
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

                if (ResetOnNewTarget && _hasTarget && _target != _lastTarget)
                    _progress = 0;
            }

            // Prevents statements at the top of the func from doing div by 0
            if (_target == null && LockDecayTime <= 0)
                _progress = 0f;
            else if (_target != null && LockTime <= 0)
                _progress = 1f;

            _detectionTick = Time.time + 0.1f;
        }

        private void UpdateAnimate()
        {
            if (_target != null)
            {
                Vector3 trgtPos = TargetBody ? _target!.AimTargetBody.position : _target!.AimTarget.position;
                _reticleHolder!.transform.position = _camera!.WorldToScreenPoint(trgtPos);
            }

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
            Vector3 endPos = _camera!.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
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

        private bool HasAmmo => _weapon != null && _weapon.GetCurrentClip() > 0 && _weapon.IsReloading == false;
        private bool CanLock => LockWhileEmpty || HasAmmo;
        private bool LockedTarget => _target != null && _progress == 1f;
        private bool AutoAimActive => HasAmmo && (
               AimActive == InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)
            || HipActive != InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)
            );
        public bool UseAutoAim => LockedTarget && AutoAimActive;

        private bool CheckTargetValid()
        {
            if (_target == null || !_target.Alive) return false;
            if (!IgnoreInvisibility && (_target.RequireTagForDetection || TagOnly) && !_target.IsTagged) return false;

            // Check if any part of the target is still valid
            Vector3 position = _weapon!.transform.position;
            Vector3 forward = _weapon!.transform.forward;
            foreach (Collider collider in _target.GetComponentsInChildren<Collider>())
            {
                Vector3 targetPos = collider.transform.position;
                Vector3 up = targetPos + Vector3.up * 0.4f;
                Vector3 left = targetPos + Vector3.left * 0.4f;
                Vector3 to = targetPos - position;
                if (Vector3.Angle(forward, to) < Angle && !Physics.Linecast(position, targetPos, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS) && !Physics.Linecast(position, up, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS) && !Physics.Linecast(position, left, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS))
                    return true;
            }
            return false;
        }

        private EnemyAgent? CheckForTarget()
        {
            Vector3 forward = _weapon!.transform.forward;
            Vector3 position = _weapon!.transform.position;

            Collider[] colliders = Physics.OverlapSphere(position, Range, LayerManager.MASK_SENTRYGUN_DETECTION_TARGETS);
            _colliderComparer.Set(forward, position);
            Array.Sort(colliders, 0, colliders.Length, _colliderComparer);

            // Go through colliders by order of minimum angle (enemies closest to aim point are prioritized)
            foreach (Collider collider in colliders)
            {
                Vector3 pos = collider.transform.position;
                Vector3 to = pos - position;
                // Sorted by this, so if any are bigger, no valid colliders remain
                if (Vector3.Angle(forward, to) >= Angle)
                    return null;

                Vector3 up = pos + Vector3.up * 0.4f;
                Vector3 left = pos + Vector3.left * 0.4f;
                if (Physics.Linecast(position, pos, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS) || Physics.Linecast(position, up, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS) || Physics.Linecast(position, left, LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS))
                    continue;

                EnemyAgent? enemyAgent = collider.GetComponent<IDamageable>()?.GetBaseAgent()?.TryCast<EnemyAgent>();
                if (enemyAgent == null) continue;

                if (((!enemyAgent.RequireTagForDetection && !TagOnly) || enemyAgent.IsTagged || IgnoreInvisibility) && enemyAgent.Alive)
                    return enemyAgent;
            }
            return null;
        }

        public IWeaponProperty Clone()
        {
            AutoAim copy = new()
            {
                AimActive = AimActive,
                HipActive = HipActive,
                Angle = Angle,
                Range = Range,
                LockTime = LockTime,
                LockDecayTime = LockDecayTime,
                StayOnTarget = StayOnTarget,
                ResetOnNewTarget = ResetOnNewTarget,
                RequireLock = RequireLock,
                LockWhileEmpty = LockWhileEmpty,
                TagOnly = TagOnly,
                IgnoreInvisibility = IgnoreInvisibility,
                TargetBody = TargetBody,
                FavorLookPoint = FavorLookPoint
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
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
            writer.WriteBoolean(nameof(TargetBody), TargetBody);
            writer.WriteBoolean(nameof(FavorLookPoint), FavorLookPoint);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
                    TargetBody = reader.GetBoolean();
                    break;
                case "favorlookpoint":
                case "favourlookpoint":
                    FavorLookPoint = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }

        sealed class ColliderComparer : IComparer
        {
            Vector3 _forward;
            Vector3 _position;
            public void Set(Vector3 forward, Vector3 position)
            {
                _forward = forward;
                _position = position;
            }

            public int Compare(object? x, object? y)
            {
                // OverlapSphere never returns null colliders, so assuming non-null
                Vector3 xPos = ((Collider)x!).transform.position;
                Vector3 yPos = ((Collider)y!).transform.position;
                return Vector3.Angle(_forward, xPos - _position) < Vector3.Angle(_forward, yPos - _position) ? -1 : 1;
            }
        }
    }
}
