using SyncService;
using SyncService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<TangoInscripcionService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
