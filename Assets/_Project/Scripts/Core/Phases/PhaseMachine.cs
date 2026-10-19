using System;
using System.Collections.Generic;
using CourtOfNi.Core.Match;

namespace CourtOfNi.Core.Phases
{
    /// <summary>
    /// 한 매치의 페이즈 순서를 관리한다.
    /// 역할 공개 → 사건 만들기 → [피고인A 변론 → 피고인B 변론 → 질문 → 배심원 회의 → 투표] × 라운드 → 판결 → 종료.
    /// 라운드마다 먼저 변론하는 피고인이 번갈아 바뀐다.
    /// </summary>
    public sealed class PhaseMachine
    {
        readonly List<PhaseState> _sequence = new List<PhaseState>();
        int _index = -1;

        public PhaseMachine(MatchRules rules, IReadOnlyList<int> defendantIds)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (rules.Rounds < 1) throw new ArgumentException("라운드는 최소 1회여야 합니다.", nameof(rules));

            _sequence.Add(new PhaseState(MatchPhase.RoleReveal, 0, -1, rules.RoleRevealSeconds));
            _sequence.Add(new PhaseState(MatchPhase.CaseBuilding, 0, -1, rules.CaseBuildingSeconds));

            for (var round = 1; round <= rules.Rounds; round++)
            {
                var first = defendantIds[(round - 1) % defendantIds.Count];
                var second = defendantIds[round % defendantIds.Count];
                _sequence.Add(new PhaseState(MatchPhase.Defense, round, first, rules.DefenseSeconds));
                _sequence.Add(new PhaseState(MatchPhase.Defense, round, second, rules.DefenseSeconds));
                _sequence.Add(new PhaseState(MatchPhase.Questioning, round, -1, rules.QuestioningSeconds));
                _sequence.Add(new PhaseState(MatchPhase.Deliberation, round, -1, rules.DeliberationSeconds));
                _sequence.Add(new PhaseState(MatchPhase.Voting, round, -1, rules.VotingSeconds));
            }

            _sequence.Add(new PhaseState(MatchPhase.Verdict, 0, -1, rules.VerdictSeconds));
            _sequence.Add(new PhaseState(MatchPhase.Finished, 0, -1, 0f));
        }

        /// <summary>페이즈가 바뀔 때 (이전, 현재)로 호출된다.</summary>
        public event Action<PhaseState, PhaseState> PhaseChanged;

        public PhaseState Current => _index < 0 ? new PhaseState(MatchPhase.NotStarted, 0, -1, 0f) : _sequence[_index];
        public float Remaining { get; private set; }
        public bool IsFinished => Current.Phase == MatchPhase.Finished;

        public void Start()
        {
            if (_index >= 0) throw new InvalidOperationException("이미 시작된 매치입니다.");
            MoveTo(0);
        }

        /// <summary>판사의 "다음" 버튼이나 조기 종료 조건(투표 완료 등)으로 다음 페이즈로 넘긴다.</summary>
        public void Advance()
        {
            if (_index < 0) throw new InvalidOperationException("매치가 시작되지 않았습니다.");
            if (IsFinished) return;
            MoveTo(_index + 1);
        }

        /// <summary>서버(호스트)가 매 프레임 호출한다. 타이머가 끝나면 자동으로 다음 페이즈로 넘어간다.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_index < 0 || IsFinished || !Current.HasTimer) return;
            Remaining -= deltaSeconds;
            if (Remaining <= 0f) Advance();
        }

        void MoveTo(int index)
        {
            var previous = Current;
            _index = index;
            Remaining = Current.Duration;
            PhaseChanged?.Invoke(previous, Current);
        }
    }
}
