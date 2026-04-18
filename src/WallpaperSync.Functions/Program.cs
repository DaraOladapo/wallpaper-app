using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton(_ =>
            new Azure.Storage.Blobs.BlobServiceClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")));
    })
    .Build();

await host.RunAsync();
