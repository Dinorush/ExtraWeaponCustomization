using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using UnityEngine;

namespace EWC.Utils
{
    public sealed class DelayedCallback
    {
        private readonly float _duration;
        private readonly Action? _onStart;
        private readonly Action? _onRefresh;
        private readonly Action? _onEnd;

        private float _endTime;
        private Coroutine? _routine;

        public DelayedCallback(float duration, Action? onEnd)
        {
            _duration = duration;
            _onEnd = onEnd;
        }

        public DelayedCallback(float duration, Action? onStart, Action? onEnd)
        {
            _duration = duration;
            _onStart = onStart;
            _onEnd = onEnd;
        }

        public DelayedCallback(float duration, Action? onStart, Action? onRefresh, Action? onEnd)
        {
            _duration = duration;
            _onStart = onStart;
            _onRefresh = onRefresh;
            _onEnd = onEnd;
        }

        public void Start()
        {
            _endTime = Clock.Time + _duration;
            _onRefresh?.Invoke();
            _routine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        public IEnumerator Update()
        {
            _onStart?.Invoke();
            while (_endTime > Clock.Time)
                yield return new WaitForSeconds(_endTime - Clock.Time);
            _routine = null;
            _onEnd?.Invoke();
        }

        public void Stop()
        {
            if (_routine != null)
            {
                CoroutineManager.StopCoroutine(_routine);
                _routine = null;
                _onEnd?.Invoke();
            }
        }

        public void Cancel()
        {
            if (_routine != null)
            {
                CoroutineManager.StopCoroutine(_routine);
                _routine = null;
            }
        }
    }
}
