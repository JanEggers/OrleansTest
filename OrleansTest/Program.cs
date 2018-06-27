using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Grains.Placement;

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
            var silo = new SiloHostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.json");
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "TestSilo";
                    options.ServiceId = "TestSilo";
                })
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(typeof(DeviceGrain).Assembly)
                        .WithReferences();
                })
                .Configure<TypeManagementOptions>(options =>
                {
                    options.TypeMapRefreshInterval = TimeSpan.FromSeconds(1);
                })
                .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000, advertisedIP: IPAddress.Loopback)
                .UseAdoNetClustering(options =>
                {
                    options.ConnectionString = @"Server=localhost\SQLEXPRESS;Initial Catalog=OrleansTest;Trusted_Connection=True;";
                    options.Invariant = "System.Data.SqlClient";
                })
                .AddPlacementDirector<RolePlacementStrategy, RolePlacementDirector>()
                .AddSimpleMessageStreamProvider("SimpleStreamProvider")
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .Build();

            await silo.StartAsync();

            Console.WriteLine("Silo started.");
            
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            await silo.StopAsync();
        }
    }
}
