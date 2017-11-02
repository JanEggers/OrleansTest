using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    /// <summary>
    /// Grain interface IGrain1
    /// </summary>
    public interface IGreeterGrain : IGrainWithGuidKey
    {
        Task<int> GetStatistics();
        Task<string> SayHello(string name);
    }
}
