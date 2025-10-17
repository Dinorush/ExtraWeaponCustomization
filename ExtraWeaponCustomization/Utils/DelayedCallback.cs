using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using UnityEngine;

namespace EWC.Utils
{
    public sealed class DelayedCallback
    {
        private readonly float _delay;
        private readonly Func<float>? _getDelay;
        private readonly Action? _onStart;
        private readonly Action? _onRefresh;
        private readonly Action? _onEnd;

        private float _endTime;
        private Coroutine? _routine;

        // Takes a function for the delay. Use if the delay is not known on creation or has custom behavior.
        public DelayedCallback(Func<float> getDelay, Action? onEnd) : this(getDelay, null, null, onEnd) { }
        public DelayedCallback(Func<float> getDelay, Action? onStart, Action? onEnd) : this(getDelay, onStart, null, onEnd) { }
        public DelayedCallback(Func<float> getDelay, Action? onStart, Action? onRefresh, Action? onEnd)
        {
            _getDelay = getDelay;
            _onStart = onStart;
            _onRefresh = onRefresh;
            _onEnd = onEnd;
        }

        // Takes a constant for the delay. Use if the delay is known at creation time.
        public DelayedCallback(float delay, Action? onEnd) : this(delay, null, null, onEnd) { }
        public DelayedCallback(float delay, Action? onStart, Action? onEnd) : this(delay, onStart, null, onEnd) { }
        public DelayedCallback(float delay, Action? onStart, Action? onRefresh, Action? onEnd)
        {
            _delay = delay;
            _onStart = onStart;
            _onRefresh = onRefresh;
            _onEnd = onEnd;
        }

        public bool Active => _endTime > Clock.Time && _routine != null;

        // Starts or refreshes the delayed callback.
        // Calls OnStart if the callback is currently inactive, otherwise calls OnRefresh.
        public void Start(bool checkEnd = false, bool refresh = true)
        {
            bool active = Active;
            if (!refresh && active) return;

            // Catch case where callback should have ended but coroutine hasn't ran yet.
            _endTime = Clock.Time + (_getDelay?.Invoke() ?? _delay);
            if (checkEnd && !active && _routine != null)
            {
                _onEnd?.Invoke();
                _onStart?.Invoke();
            }
            else if (_routine == null)
                _routine = CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
            else
                _onRefresh?.Invoke();
        }

        // Loop created after Start() until the delay passes.
        // If Start() is called while Update() is running, the delay is extended rather than starting another routine.
        public IEnumerator Update()
        {
            _onStart?.Invoke();
            while (_endTime > Clock.Time)
                yield return new WaitForSeconds(_endTime - Clock.Time);
            _routine = null;
            _onEnd?.Invoke();
        }

        // Forcibly ends Update() and calls OnEnd if it was active.
        public void Stop()
        {
            if (CoroutineUtil.Stop(ref _routine))
                _onEnd?.Invoke();
        }

        // Forcibly ends Update(). Does NOT call OnEnd.
        public void Cancel()
        {
            CoroutineUtil.Stop(ref _routine);
        }

        // Checks if the coroutine is active and should stop. Useful if timing is very important.
        public bool CheckEnd()
        {
            if (_routine == null) return true;

            if (!Active && CoroutineUtil.Stop(ref _routine))
            {
                _onEnd?.Invoke();
                return true;
            }
            return false;
        }
    }
}
