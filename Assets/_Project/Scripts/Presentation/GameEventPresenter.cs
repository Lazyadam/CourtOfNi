using System;
using CourtOfNi.Core.Events;
using UnityEngine;

namespace CourtOfNi.Presentation
{
    /// <summary>
    /// 게임 이벤트 하나에 연출을 붙이는 기반 컴포넌트. 상속해서 Play만 구현하면
    /// 활성화되어 있는 동안 해당 이벤트가 올 때마다 호출된다.
    /// </summary>
    public abstract class GameEventPresenter<T> : MonoBehaviour where T : IGameEvent
    {
        IDisposable _subscription;

        protected virtual void OnEnable() => _subscription = GameEventBus.Subscribe<T>(Play);

        protected virtual void OnDisable() => _subscription?.Dispose();

        protected abstract void Play(T gameEvent);
    }
}
