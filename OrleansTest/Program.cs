using System;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Providers;
using Orleans.Providers.Streams.SimpleMessageStream;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;
using Orleans.Storage;
using Orleans.Streams;

namespace OrleansTest
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            // First, configure and start a local silo
            var siloConfig = ClusterConfiguration.LocalhostPrimarySilo(siloPort:8080);
            siloConfig.Globals.RegisterStreamProvider<SimpleMessageStreamProvider>("SimpleStreamProvider");
            siloConfig.Globals.RegisterStorageProvider<MemoryStorage>("Default");
            siloConfig.Globals.RegisterStorageProvider<MemoryStorage>("PubSubStore");
            var silo = new SiloHost("TestSilo", siloConfig);
            silo.InitializeOrleansSilo();
            silo.StartOrleansSilo();

            Console.WriteLine("Silo started.");

            // Then configure and connect a client.
            var clientConfig = ClientConfiguration.LocalhostSilo();
            clientConfig.RegisterStreamProvider<SimpleMessageStreamProvider>("SimpleStreamProvider");
            var client = new ClientBuilder()
                .UseConfiguration(clientConfig)
                .Build();

            await client.Connect();

            Console.WriteLine("Client connected.");

            //
            // This is the place for your test code.
            //

            var id = Guid.NewGuid();
            var friend = client.GetGrain<IGreeterGrain>(id);
            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");
            var stream = streamProvider.GetStream<Request>(id, "MyStreamNamespace");
            var request = new Request() { Msg = "Hello" };
            var requests = Enumerable.Range(0, 1000).Select(i => new Request() { Msg = "Hello" }).ToList();
            
            var send = 0;

            var t1 = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    var recv = await friend.GetStatistics();
                    Console.WriteLine($"{send}/{recv}");
                    send = 0;
                }
            });
            
            var t2 = Task.Run(async () =>
            {
                //while (true)
                //{
                //    for (int i = 0; i < 10000; i++)
                //    {
                //        friend.SayHello("peter");
                //        send++;
                //    }
                //    await Task.Delay(TimeSpan.FromMilliseconds(1));
                //}

                //while (true)
                //{
                //    await stream.OnNextBatchAsync(requests);
                //    send += requests.Count;
                //}

                while (true)
                {
                    await stream.OnNextAsync(request);
                    send ++;
                }
            });

            var t3 = Task.Run(() =>
            {
                Console.WriteLine("\nPress Enter to terminate...");
                Console.ReadLine();
            });

            await await Task.WhenAny(t1, t2, t3);

            // Shut down
            await client.Close();
            silo.ShutdownOrleansSilo();
        }
    }
}
