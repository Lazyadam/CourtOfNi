using System.Collections.Generic;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Players;
using CourtOfNi.Core.Roles;

namespace CourtOfNi.Core.Missions
{
    /// <summary>
    /// 배심원 개인 미션. 판결이 끝난 뒤 배심원별로 평가해서 점수 변화량을 돌려준다 (실패 시 음수 가능).
    /// 새 미션 종류는 이 인터페이스를 구현하고, 데이터 에셋(JuryMissionAsset)으로 감싸서 에디터에서 편집한다.
    /// </summary>
    public interface IJuryMission
    {
        string Id { get; }
        string Description { get; }
        MissionOutcome Evaluate(MissionContext context, int jurorId);
    }

    public readonly struct MissionOutcome
    {
        public readonly bool Success;
        public readonly int ScoreDelta;

        public MissionOutcome(bool success, int scoreDelta)
        {
            Success = success;
            ScoreDelta = scoreDelta;
        }
    }

    /// <summary>미션 평가에 필요한 매치 결과 전체.</summary>
    public sealed class MissionContext
    {
        public RoleAssignment Assignment { get; }
        public VerdictResult Verdict { get; }
        public IReadOnlyDictionary<int, PlayerInfo> Players { get; }

        public MissionContext(RoleAssignment assignment, VerdictResult verdict, IReadOnlyDictionary<int, PlayerInfo> players)
        {
            Assignment = assignment;
            Verdict = verdict;
            Players = players;
        }
    }
}
