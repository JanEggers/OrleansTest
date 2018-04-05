using GrainInterfaces;
using Orleans;
using System.Threading.Tasks;

namespace Grains
{
    public interface IDeviceGrainInternal : IGrainWithStringKey
    {
        Task<IDevice> AsDevice();

        Task UpdateStatus();
    }
}
