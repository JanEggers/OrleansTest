using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class DeviceGrain : Grain<DeviceStatus>, IDeviceGrainInternal, IDevice
    {
        private IDevice _device;
        private IAsyncStream<DeviceStatus> _statusStream;

        public override Task OnActivateAsync()
        {
            _device = this.GrainFactory.GetGrain<IDevice>(this.GetPrimaryKeyString());

            var streamProvider = GetStreamProvider("SimpleStreamProvider");
            //Get the reference to a stream
            _statusStream = streamProvider.GetStream<DeviceStatus>(new Guid(), nameof(DeviceStatus));
            return base.OnActivateAsync();
        }
        public Task<IDevice> AsDevice()
        {
            return Task.FromResult(_device);
        }

        public Task<DeviceStatus> GetStatus()
        {
            return Task.FromResult(this.State);
        }

        public async Task UpdateStatus()
        {
            var opState = this.State.OperationStatus;
            switch (opState)
            {
                case OperationStatus.Unknown:
                    opState = OperationStatus.Ready;
                    break;
                case OperationStatus.Ready:
                    opState = OperationStatus.Warning;
                    break;
                case OperationStatus.Warning:
                    opState = OperationStatus.Error;
                    break;
                case OperationStatus.Error:
                    opState = OperationStatus.Ready;
                    break;
            }

            this.State.OperationStatus = opState;

            await WriteStateAsync();
            await _statusStream.OnNextAsync(this.State);
        }

        public Task<Guid> GetStatusStreamId()
        {
            return Task.FromResult(_statusStream.Guid);
        }
    }
}
