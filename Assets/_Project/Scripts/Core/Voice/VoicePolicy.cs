using System.Collections.Generic;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;

namespace CourtOfNi.Core.Voice
{
    /// <summary>플레이어가 들어가 있어야 하는 음성 채널과, 그 채널에서 말할 수 있는지.</summary>
    public readonly struct VoiceRoute
    {
        public readonly string Channel;
        public readonly bool CanSpeak;

        public VoiceRoute(string channel, bool canSpeak)
        {
            Channel = channel;
            CanSpeak = canSpeak;
        }

        public override string ToString() => $"{Channel}{(CanSpeak ? "" : " (muted)")}";
    }

    /// <summary>
    /// 역할과 페이즈에 따라 음성 채널을 배정한다.
    /// - 변론: 모두 법정 채널에 있되, 판사와 변론 중인 피고인만 말할 수 있다.
    /// - 배심원 회의: 배심원은 배심원 채널로 이동하고, 나머지는 법정 채널에 남는다.
    /// - 그 외: 모두 법정 채널에서 자유롭게 말한다.
    /// </summary>
    public static class VoicePolicy
    {
        public const string CourtChannel = "court";
        public const string JuryChannel = "jury";

        public static IReadOnlyList<VoiceRoute> RoutesFor(int playerId, Role role, PhaseState phase)
        {
            switch (phase.Phase)
            {
                case MatchPhase.Defense:
                    var speaking = role == Role.Judge || playerId == phase.ActivePlayerId;
                    return new[] { new VoiceRoute(CourtChannel, speaking) };

                case MatchPhase.Deliberation:
                    return role == Role.Juror
                        ? new[] { new VoiceRoute(JuryChannel, true) }
                        : new[] { new VoiceRoute(CourtChannel, true) };

                default:
                    return new[] { new VoiceRoute(CourtChannel, true) };
            }
        }
    }

    /// <summary>
    /// 음성 서비스(Vivox, Photon Voice, Steam Voice 등)를 감싸는 인터페이스.
    /// 클라이언트는 페이즈가 바뀔 때마다 VoicePolicy가 준 경로를 Apply로 넘기기만 한다.
    /// </summary>
    public interface IVoiceService
    {
        bool IsConnected { get; }
        void Connect(string playerName);
        void Disconnect();
        /// <summary>목록에 없는 채널은 나가고, 있는 채널은 들어가며, 말하기 권한을 맞춘다.</summary>
        void Apply(IReadOnlyList<VoiceRoute> routes);
    }

    /// <summary>음성 연동 전까지 쓰는 빈 구현. 현재 경로만 기억한다.</summary>
    public sealed class NullVoiceService : IVoiceService
    {
        public bool IsConnected { get; private set; }
        public IReadOnlyList<VoiceRoute> CurrentRoutes { get; private set; } = new VoiceRoute[0];

        public void Connect(string playerName) => IsConnected = true;
        public void Disconnect() => IsConnected = false;
        public void Apply(IReadOnlyList<VoiceRoute> routes) => CurrentRoutes = routes;
    }
}
