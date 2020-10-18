namespace Avalon.Server.Model
{
    public enum Role
    {
        ServantOfArthur,
        MinionOfMordred
    }

    public enum Loyalty
    {
        Good,
        Evil
    }

    static class RoleMethods
    {
        public static Loyalty GetTeam(this Role role)
        {
            switch (role)
            {
                case Role.ServantOfArthur:
                    return Loyalty.Good;
                case Role.MinionOfMordred:
                    return Loyalty.Evil;
            }

            throw new System.ArgumentException("Unknown role");
        }
    }
}