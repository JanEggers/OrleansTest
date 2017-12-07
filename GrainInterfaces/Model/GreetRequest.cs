using Orleans.Concurrency;

namespace GrainInterfaces.Model
{
    [Immutable]
    public class GreetRequest
    {
        public string Name { get; set; }
    }
}
