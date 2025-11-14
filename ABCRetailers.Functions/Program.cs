using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ABCRetailers.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerOptions =>
    {
        workerOptions.EnableUserCodeException = true;
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        // Load configuration sources
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Register your services with access to configuration
        services.AddSingleton<IAzureStorageService>(sp =>
            new AzureStorageService(context.Configuration));

        services.AddSingleton<IBlobStorageService>(sp =>
            new BlobStorageService(context.Configuration));

        services.AddSingleton<IFileShareService>(sp =>
            new FileShareService(context.Configuration));
    })
    .Build();

Console.WriteLine("Starting Azure Functions...");
host.Run();
