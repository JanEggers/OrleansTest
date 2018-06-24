using Orleans.Runtime;
using System;

namespace Grains.Placement
{
    [Serializable]
    public class RolePlacementStrategy : PlacementStrategy
    {
        public string RoleName { get; private set; }

        internal RolePlacementStrategy(string roleName)
        {
            RoleName = roleName;
        }

        public override string ToString()
        {
            return String.Format($"RolePlacementStrategy(role={RoleName})");
        }

        public override bool Equals(object obj)
        {
            if (obj is RolePlacementStrategy other)
                return other.RoleName == RoleName;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ RoleName.GetHashCode();
        }
    }
}
