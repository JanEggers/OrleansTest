﻿using System.Threading.Tasks;
using Grains.Placement;
using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace OrleansTest2.Placement
{

    public class RolePlacementDirector : IPlacementDirector
    {
        public RolePlacementDirector()
        {
        }

        public virtual Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var allSilos = context.GetCompatibleSilos(target);
            var rolePlacementStrategy = (RolePlacementStrategy)strategy;

            return Task.FromResult(allSilos[0]);
        }
    }
}
