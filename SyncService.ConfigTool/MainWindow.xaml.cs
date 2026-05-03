using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace SyncService.ConfigTool;

public partial class MainWindow : Window
{
    private const string ServiceName = "SADSyncService";

    private readonly string _appSettingsPath;
    private readonly string _appSettingsTemplatePath;

    public MainWindow()
    {
        InitializeComponent();

        var baseDir = AppContext.BaseDirectory;
        _appSettingsPath = Path.Combine(baseDir, "appsettings.json");
        _appSettingsTemplatePath = Path.Combine(baseDir, "appsettings.template.json");

        Loaded += (_, _) =>
        {
            LoadSettings();
            RefreshServiceStatus();
        };
    }

    // ---------- Carga / guardado ----------

    private void LoadSettings()
    {
        try
        {
            string sourcePath = File.Exists(_appSettingsPath)
                ? _appSettingsPath
                : _appSettingsTemplatePath;

            if (!File.Exists(sourcePath))
            {
                Log("No se encontró appsettings.json ni el template. Cargando valores por defecto.");
                ApplyDefaults();
                return;
            }

            var json = File.ReadAllText(sourcePath);
            var node = JsonNode.Parse(json) ?? new JsonObject();

            var conn = node["SqlServerConnection"]?.GetValue<string>() ?? string.Empty;
            ParseSqlConnection(conn);

            ApiBaseUrlBox.Text = node["ApiSettings"]?["BaseUrl"]?.GetValue<string>() ?? "http://localhost:5161";
            SyncKeyBox.Text = node["ApiSettings"]?["SyncKey"]?.GetValue<string>() ?? string.Empty;

            // Soporta ambos esquemas (Minutes/Seconds) que aparecen en el repo.
            IntervalBox.Text = (node["SyncIntervalMinutes"]?.GetValue<int>() ?? 30).ToString();
        }
        catch (Exception ex)
        {
            Log($"Error leyendo configuración: {ex.Message}");
            ApplyDefaults();
        }
    }

    private void ApplyDefaults()
    {
        ApiBaseUrlBox.Text = "http://localhost:5161";
        IntervalBox.Text = "30";
        TrustServerCertBox.IsChecked = true;
    }

    private void ParseSqlConnection(string conn)
    {
        if (string.IsNullOrWhiteSpace(conn) || conn.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var b = new SqlConnectionStringBuilder(conn);
            SqlServerBox.Text = b.DataSource;
            SqlDatabaseBox.Text = b.InitialCatalog;
            SqlUserBox.Text = b.UserID;
            SqlPasswordBox.Password = b.Password;
            TrustServerCertBox.IsChecked = b.TrustServerCertificate;
        }
        catch (Exception ex)
        {
            Log($"Connection string inválida en archivo: {ex.Message}");
        }
    }

    private string BuildSqlConnection()
    {
        var b = new SqlConnectionStringBuilder
        {
            DataSource = SqlServerBox.Text.Trim(),
            InitialCatalog = SqlDatabaseBox.Text.Trim(),
            UserID = SqlUserBox.Text.Trim(),
            Password = SqlPasswordBox.Password,
            TrustServerCertificate = TrustServerCertBox.IsChecked == true,
        };
        return b.ConnectionString;
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(SqlServerBox.Text)) return Fail("Falta servidor SQL.");
        if (string.IsNullOrWhiteSpace(SqlDatabaseBox.Text)) return Fail("Falta base de datos.");
        if (string.IsNullOrWhiteSpace(SqlUserBox.Text)) return Fail("Falta usuario SQL.");
        if (string.IsNullOrWhiteSpace(ApiBaseUrlBox.Text)) return Fail("Falta URL del backend.");
        if (string.IsNullOrWhiteSpace(SyncKeyBox.Text)) return Fail("Falta SyncKey.");
        if (!int.TryParse(IntervalBox.Text, out var n) || n <= 0) return Fail("Intervalo inválido.");
        return true;
    }

    private bool Fail(string msg)
    {
        MessageBox.Show(this, msg, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private bool SaveSettings()
    {
        if (!Validate()) return false;

        try
        {
            JsonNode root;
            if (File.Exists(_appSettingsPath))
                root = JsonNode.Parse(File.ReadAllText(_appSettingsPath)) ?? new JsonObject();
            else if (File.Exists(_appSettingsTemplatePath))
                root = JsonNode.Parse(File.ReadAllText(_appSettingsTemplatePath)) ?? new JsonObject();
            else
                root = new JsonObject();

            root["SqlServerConnection"] = BuildSqlConnection();

            var api = root["ApiSettings"] as JsonObject ?? new JsonObject();
            api["BaseUrl"] = ApiBaseUrlBox.Text.Trim();
            api["SyncKey"] = SyncKeyBox.Text.Trim();
            root["ApiSettings"] = api;

            root["SyncIntervalMinutes"] = int.Parse(IntervalBox.Text);

            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_appSettingsPath, root.ToJsonString(opts));
            Log($"appsettings.json guardado en {_appSettingsPath}");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"No se pudo guardar appsettings.json:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    // ---------- Tests ----------

    private async void TestSql_Click(object sender, RoutedEventArgs e)
    {
        Log("Probando conexión SQL...");
        try
        {
            await using var conn = new SqlConnection(BuildSqlConnection());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @@VERSION";
            var v = (string?)await cmd.ExecuteScalarAsync();
            Log($"OK: {v?.Split('\n')[0]}");
        }
        catch (Exception ex)
        {
            Log($"FALLÓ: {ex.Message}");
        }
    }

    private async void TestApi_Click(object sender, RoutedEventArgs e)
    {
        Log("Probando API...");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("X-Sync-Key", SyncKeyBox.Text.Trim());
            var url = ApiBaseUrlBox.Text.TrimEnd('/') + "/api/sync/inscripciones";
            var resp = await http.GetAsync(url);
            Log($"GET {url} -> {(int)resp.StatusCode} {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            Log($"FALLÓ: {ex.Message}");
        }
    }

    // ---------- Botones ----------

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SaveSettings())
            MessageBox.Show(this, "Configuración guardada. Reiniciá el servicio para aplicar los cambios.",
                "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void SaveAndRestart_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSettings()) return;
        await RestartServiceAsync();
        RefreshServiceStatus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    // ---------- Servicio Windows ----------

    private void RefreshServiceStatus()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            ServiceStatusText.Text = $"{ServiceName}: {sc.Status}";
        }
        catch (InvalidOperationException)
        {
            ServiceStatusText.Text = $"{ServiceName}: NO INSTALADO";
        }
        catch (Exception ex)
        {
            ServiceStatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async Task RestartServiceAsync()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            if (sc.Status != ServiceControllerStatus.Stopped &&
                sc.Status != ServiceControllerStatus.StopPending)
            {
                Log("Deteniendo servicio...");
                sc.Stop();
                await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60)));
            }
            Log("Iniciando servicio...");
            sc.Start();
            await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60)));
            Log($"Servicio en estado: {sc.Status}");
        }
        catch (InvalidOperationException)
        {
            Log($"El servicio {ServiceName} no está instalado.");
        }
        catch (Exception ex)
        {
            Log($"Error controlando el servicio: {ex.Message}");
        }
    }

    // ---------- UI helpers ----------

    private void Log(string line)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        LogBox.AppendText($"[{ts}] {line}{Environment.NewLine}");
        LogBox.ScrollToEnd();
        Debug.WriteLine(line);
    }
}
