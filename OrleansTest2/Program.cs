using System;
using System.Net;
using System.Threading.Tasks;
using Grains;
using Grains.Placement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using OrleansTest2.Placement;

namespace OrleansTest2
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            var ip = Dns.GetHostAddresses(Environment.MachineName);

            var silo = new SiloHostBuilder()
                .ConfigureAppConfiguration(builder => { builder.AddJsonFile("appsettings.json"); })
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
                .Configure<TypeManagementOptions>(options =>
                {
                    options.TypeMapRefreshInterval = TimeSpan.FromSeconds(1);
                })
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(typeof(GreeterGrain).Assembly)
                        .WithReferences();
                })
                .AddPlacementDirector<RolePlacementStrategy, RolePlacementDirector>()
                .ConfigureApplicationParts(p => p.AddApplicationPart(typeof(GreeterGrain).Assembly))
                .ConfigureEndpoints(siloPort: 11112, gatewayPort: 30001, advertisedIP: IPAddress.Loopback)
                .UseAdoNetClustering(options =>
                {
                    options.ConnectionString = @"Server=localhost\SQLEXPRESS;Initial Catalog=OrleansTest;Trusted_Connection=True;";
                    options.Invariant = "System.Data.SqlClient";
                })
                .AddSimpleMessageStreamProvider("SimpleStreamProvider")
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .Build();

            await silo.StartAsync();

            await Task.Delay(TimeSpan.FromDays(1));
        }
    }
}
