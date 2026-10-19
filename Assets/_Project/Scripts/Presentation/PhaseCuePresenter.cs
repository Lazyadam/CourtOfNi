using System;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Phases;
using UnityEngine;

namespace CourtOfNi.Presentation
{
    /// <summary>
    /// 연출 예시: 페이즈가 바뀌면 페이즈별로 지정한 애니메이터 트리거와 효과음을 재생한다.
    /// (예: 변론 시작 시 판사봉 애니메이션, 투표 시작 시 북소리)
    /// </summary>
    public sealed class PhaseCuePresenter : GameEventPresenter<PhaseChangedEvent>
    {
        [Serializable]
        public struct Cue
        {
            public MatchPhase phase;
            public string animatorTrigger;
            public AudioClip sound;
        }

        [SerializeField] Animator animator;
        [SerializeField] AudioSource audioSource;
        [SerializeField] Cue[] cues = Array.Empty<Cue>();

        protected override void Play(PhaseChangedEvent gameEvent)
        {
            foreach (var cue in cues)
            {
                if (cue.phase != gameEvent.Current.Phase) continue;
                if (animator != null && !string.IsNullOrEmpty(cue.animatorTrigger)) animator.SetTrigger(cue.animatorTrigger);
                if (audioSource != null && cue.sound != null) audioSource.PlayOneShot(cue.sound);
            }
        }
    }
}
