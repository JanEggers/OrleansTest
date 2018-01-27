using GrainInterfaces.Model;
using Orleans;
using System;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    public interface IDevice : IGrainWithStringKey
    {
        Task<Guid> GetStatusStreamId();
        
        Task<DeviceStatus> GetStatus();
    }
}
