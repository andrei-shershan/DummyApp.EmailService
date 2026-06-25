using DummyApp.EmailService.Functions.Extensions;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config => config.AddKeyVaultFromConfiguration())
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) => services.AddEmailServices(context.Configuration))
    .Build();

host.Run();
