using Orleans.Concurrency;

namespace GrainInterfaces.Model
{
    [Immutable]
    public class Request
    {
        public string Msg { get; set; }
    }
}
