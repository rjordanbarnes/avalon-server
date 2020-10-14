namespace Avalon.Server.Model
{
    public class Player
    {
        public string connectionId;
        public string name;
        public Role role;

        public Player(string connectionId, string name)
        {
            this.connectionId = connectionId;
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Player other = (Player)obj;

            return other.connectionId == this.connectionId;
        }

        public override int GetHashCode()
        {
            return connectionId.GetHashCode();
        }
    }
}