using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;

namespace CourtOfNi.Core.Chat
{
    public enum ChatChannel
    {
        /// <summary>모두가 보는 법정 채팅.</summary>
        Court = 0,
        /// <summary>배심원끼리만 보는 채팅.</summary>
        Jury = 1,
    }

    public sealed class ChatMessage
    {
        public int SenderId;
        public ChatChannel Channel;
        public string Text;
    }

    /// <summary>
    /// 역할과 페이즈에 따라 누가 어떤 채팅 채널에 쓰고 읽을 수 있는지 정한다.
    /// 서버(호스트)가 이 규칙으로 메시지를 받을 사람만 골라서 보낸다.
    /// </summary>
    public static class ChatPolicy
    {
        public const int MaxMessageLength = 200;

        public static bool CanRead(Role role, ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.Court: return role != Role.None;
                case ChatChannel.Jury: return role == Role.Juror;
                default: return false;
            }
        }

        public static bool CanSend(Role role, ChatChannel channel, PhaseState phase)
        {
            if (!CanRead(role, channel)) return false;
            if (phase.Phase == MatchPhase.Finished || phase.Phase == MatchPhase.NotStarted) return channel == ChatChannel.Court;
            return true;
        }

        public static IReadOnlyList<int> Recipients(RoleAssignment assignment, ChatChannel channel) =>
            assignment.AllPlayers.Where(p => CanRead(assignment.RoleOf(p), channel)).ToArray();

        public static string Sanitize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Trim();
            return text.Length > MaxMessageLength ? text.Substring(0, MaxMessageLength) : text;
        }
    }
}
