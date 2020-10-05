namespace Avalon.Server.Model
{
    public enum Role
    {
        ServantOfArthur,
        MinionOfMordred
    }

    public enum Team
    {
        Good,
        Evil
    }

    static class RoleMethods
    {
        public static Team GetTeam(this Role role)
        {
            switch (role)
            {
                case Role.ServantOfArthur:
                    return Team.Good;
                case Role.MinionOfMordred:
                    return Team.Evil;
            }

            throw new System.ArgumentException("Unknown role");
        }
    }
}