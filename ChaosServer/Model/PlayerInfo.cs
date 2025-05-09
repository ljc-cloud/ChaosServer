
namespace ChaosServer.Model
{
    public class PlayerInfo : IEquatable<PlayerInfo>
    {
        public int id;
        public string username;
        public string nickname;
        // public string password;
        public bool Equals(PlayerInfo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PlayerInfo)obj);
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
