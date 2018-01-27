using Orleans.Concurrency;

namespace GrainInterfaces.Model
{
    [Immutable]
    public class DeviceStatus
    {
        public OperationStatus OperationStatus { get; set; }
    }

    public enum OperationStatus
    {
        Unknown,
        Ready,
        Warning,
        Error
    }
}
