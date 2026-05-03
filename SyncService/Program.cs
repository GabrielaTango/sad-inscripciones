using Serilog;
using SyncService;
using SyncService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SADSyncService";
});

var logsDir = OperatingSystem.IsWindows()
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SAD", "SyncService", "logs")
    : Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDir);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logsDir, "syncservice-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Services.AddSerilog();

builder.Services.AddSingleton<TangoClienteService>();
builder.Services.AddSingleton<TangoInscripcionService>();
builder.Services.AddSingleton<TangoPagoService>();
builder.Services.AddSingleton<TangoImputacionService>();
builder.Services.AddSingleton<TangoResumenCuentaService>();
builder.Services.AddSingleton<ChangeTrackingService>();
builder.Services.AddHostedService<Worker>();

try
{
    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SyncService terminó inesperadamente");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
