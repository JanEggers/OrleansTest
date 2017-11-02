using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    /// <summary>
    /// Grain implementation class Grain1.
    /// </summary>
    public class GreeterGrain : Grain, IGreeterGrain
    {
        private int count = 0;

        public Task<string> SayHello(string name)
        {
            count++;
            return Task.FromResult($"Hello {name}");
        }

        public Task<int> GetStatistics()
        {
            var temp = count;
            count = 0;
            return Task.FromResult(temp);
        }
    }
}
