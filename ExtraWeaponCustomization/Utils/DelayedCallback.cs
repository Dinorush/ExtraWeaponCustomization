using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using UnityEngine;

namespace EWC.Utils
{
    public sealed class DelayedCallback
    {
        private readonly Func<float>? _endTimeUpdate;
        private readonly Action? _onStart;
        private readonly Action? _onRefresh;
        private readonly Action? _onEnd;

        private float _endTime;
        private Coroutine? _routine;

        public DelayedCallback(float endTime, Action? onEnd)
        {
            _endTime = endTime;
            _onEnd = onEnd;
        }


        public DelayedCallback(Func<float> endTimeUpdate, Action? onStart, Action? onRefresh, Action? onEnd)
        {
            _endTimeUpdate = endTimeUpdate;
            _onStart = onStart;
            _onRefresh = onRefresh;
            _onEnd = onEnd;
        }

        public void Start()
        {
            _onRefresh?.Invoke();
            _routine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        public IEnumerator Update()
        {
            _onStart?.Invoke();
            if (_endTimeUpdate != null)
            {
                for (_endTime = _endTimeUpdate.Invoke(); _endTime > Clock.Time; _endTime = _endTimeUpdate.Invoke())
                    yield return new WaitForSeconds(_endTime - Clock.Time);
            }
            else
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
