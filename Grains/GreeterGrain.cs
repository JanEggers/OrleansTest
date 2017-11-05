using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using GrainInterfaces.Model;
using Orleans.Streams;
using System;

namespace Grains
{
    /// <summary>
    /// Grain implementation class Grain1.
    /// </summary>
    [ImplicitStreamSubscription("MyStreamNamespace")]
    public class GreeterGrain : Grain, IGreeterGrain, IAsyncObserver<Request>
    {
        private int count = 0;

        public override async Task OnActivateAsync()
        {
            var streamProvider = base.GetStreamProvider("SimpleStreamProvider");
            var stream = streamProvider.GetStream<Request>(this.GetPrimaryKey(), "MyStreamNamespace");
            var subscription = await stream.SubscribeAsync(this);
            
            await base.OnActivateAsync();
        }

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

        public Task OnNextAsync(Request item, StreamSequenceToken token = null)
        {
            count++;
            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}
