using System;
using System.Collections.Generic;

namespace CourtOfNi.Core.Events
{
    /// <summary>
    /// 게임 이벤트를 연출(애니메이션, 사운드, UI)에 전달하는 타입별 이벤트 버스.
    /// 규칙 코드는 이벤트를 발행만 하고, 연출 코드는 구독만 한다.
    /// </summary>
    public static class GameEventBus
    {
        static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        public static IDisposable Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Handlers.TryGetValue(typeof(T), out var existing);
            Handlers[typeof(T)] = Delegate.Combine(existing, handler);
            return new Subscription(() => Unsubscribe(handler));
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (!Handlers.TryGetValue(typeof(T), out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) Handlers.Remove(typeof(T));
            else Handlers[typeof(T)] = remaining;
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (Handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)(gameEvent);
        }

        /// <summary>정적 타입을 모르는 이벤트(네트워크로 받은 이벤트 등)를 발행한다.</summary>
        public static void PublishBoxed(IGameEvent gameEvent)
        {
            if (gameEvent == null) return;
            if (Handlers.TryGetValue(gameEvent.GetType(), out var handler))
                handler.DynamicInvoke(gameEvent);
        }

        public static void Clear() => Handlers.Clear();

        sealed class Subscription : IDisposable
        {
            Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
