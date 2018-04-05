using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface IDeviceRepository : IGrainWithStringKey
    {
        Task<IDevice> CreateDevice(string name);

        Task<IEnumerable<IDevice>> GetDevices();
    }
}
