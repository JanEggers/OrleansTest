using GrainInterfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grains
{
    public class DeviceRepository : Grain, IDeviceRepository
    {
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            RegisterTimer(UpdateDevices, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));            
        }


        private Dictionary<string, IDeviceGrainInternal> _devices = new Dictionary<string, IDeviceGrainInternal>();

        public async Task<IDevice> CreateDevice(string name)
        {
            if(!_devices.TryGetValue(name, out var device))
            {
                _devices[name] = device = this.GrainFactory.GetGrain<IDeviceGrainInternal>(name);
            }

            var downcasted = await device.AsDevice();

            return downcasted;
        }

        public async Task<IEnumerable<IDevice>> GetDevices()
        {
            return await Task.WhenAll(_devices.Values.Select(d => d.AsDevice()));
        }

        private async Task UpdateDevices(object state)
        {
            await Task.WhenAll(_devices.Values.Select(d => d.UpdateStatus()));
        }
    }
}
