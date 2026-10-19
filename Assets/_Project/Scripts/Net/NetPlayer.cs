using System;
using Unity.Collections;
using Unity.Netcode;

namespace CourtOfNi.Net
{
    /// <summary>모든 클라이언트에 동기화되는 플레이어 목록 항목. Id는 NGO의 clientId와 같다.</summary>
    public struct NetPlayer : INetworkSerializable, IEquatable<NetPlayer>
    {
        public int Id;
        public FixedString64Bytes Name;
        public float Age;
        public int Score;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Age);
            serializer.SerializeValue(ref Score);
        }

        public bool Equals(NetPlayer other) =>
            Id == other.Id && Name.Equals(other.Name) && Age.Equals(other.Age) && Score == other.Score;

        public override bool Equals(object obj) => obj is NetPlayer other && Equals(other);
        public override int GetHashCode() => Id;
    }
}
