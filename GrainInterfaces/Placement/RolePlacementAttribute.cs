using Orleans.Placement;
using System;

namespace Grains.Placement
{
    /// <summary>
    /// Directs Orleans to only place new activations on a Silo supporting the Role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RolePlacementAttribute : PlacementAttribute
    {
        public string Role { get; private set; }

        public RolePlacementAttribute(string role) :
            base(new RolePlacementStrategy(role))
        {
            Role = role;
        }
    }
}
