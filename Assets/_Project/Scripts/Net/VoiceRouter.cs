using System;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Voice;
using UnityEngine;

namespace CourtOfNi.Net
{
    /// <summary>
    /// 페이즈가 바뀔 때마다 VoicePolicy로 로컬 플레이어의 음성 채널을 다시 계산해서 IVoiceService에 넘긴다.
    /// Vivox 연동 시 VivoxVoiceService(IVoiceService 구현)를 만들어 Service에 넣으면 된다.
    /// </summary>
    public sealed class VoiceRouter : MonoBehaviour
    {
        public IVoiceService Service { get; set; } = new NullVoiceService();

        IDisposable _phaseSubscription;

        void OnEnable() => _phaseSubscription = GameEventBus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);

        void OnDisable() => _phaseSubscription?.Dispose();

        void OnPhaseChanged(PhaseChangedEvent e)
        {
            var game = CourtNetworkGame.Instance;
            if (game == null || Service == null) return;

            var routes = VoicePolicy.RoutesFor(game.LocalPlayerId, game.LocalRole, e.Current);
            Service.Apply(routes);
            Debug.Log($"[Voice] {e.Current} → {string.Join(", ", routes)}");
        }
    }
}
