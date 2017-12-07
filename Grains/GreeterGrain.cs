using System;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Streams;

namespace Grains
{
    /// <summary>
    /// Grain implementation class Grain1.
    /// </summary>
    [ImplicitStreamSubscription("MyStreamNamespace")]
    public class GreeterGrain : Grain, IGreeterGrain
    {
        private int count = 0;

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            //Create a GUID based on our GUID as a grain
            var guid = this.GetPrimaryKey();
            //Get one of the providers which we defined in config
            var streamProvider = GetStreamProvider("SimpleStreamProvider");
            //Get the reference to a stream
            var stream = streamProvider.GetStream<GreetRequest>(guid, "MyStreamNamespace");
            //Set our OnNext method to the lambda which simply prints the data, this doesn't make new subscriptions
            await stream.SubscribeAsync((data, token) => SayHello(data.Name));
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
