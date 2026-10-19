using System;
using System.Collections.Generic;

namespace CourtOfNi.Core.Cards
{
    /// <summary>
    /// 뽑을 더미와 버린 더미를 가진 범용 덱. 뽑을 더미가 비면 버린 더미를 섞어서 다시 쓴다.
    /// </summary>
    public sealed class Deck<T>
    {
        readonly List<T> _drawPile = new List<T>();
        readonly List<T> _discardPile = new List<T>();
        readonly Random _rng;

        public Deck(IEnumerable<T> cards, Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _drawPile.AddRange(cards);
            Shuffle();
        }

        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discardPile.Count;
        public bool IsEmpty => _drawPile.Count == 0 && _discardPile.Count == 0;

        public T Draw()
        {
            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0) throw new InvalidOperationException("덱에 남은 카드가 없습니다.");
                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle();
            }

            var last = _drawPile.Count - 1;
            var card = _drawPile[last];
            _drawPile.RemoveAt(last);
            return card;
        }

        public void Discard(T card) => _discardPile.Add(card);

        public void Shuffle()
        {
            for (var i = _drawPile.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
            }
        }
    }
}
