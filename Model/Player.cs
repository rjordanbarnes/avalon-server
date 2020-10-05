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
    }
}