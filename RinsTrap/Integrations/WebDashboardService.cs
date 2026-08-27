using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RinsTrap.Integrations
{
    public class WebDashboardService : IDisposable
    {
        private static WebDashboardService? _instance;
        public static WebDashboardService Instance => _instance ??= new WebDashboardService();

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _serverTask;
        private bool _disposed;

        public bool IsRunning => _listener != null;
        public int Port => App.Settings.Prop.WebDashboardPort;
        public string Url => $"http://localhost:{Port}";
        public event EventHandler? StatusChanged;

        private WebDashboardService() { }

        public async Task StartAsync()
        {
            if (IsRunning)
                return;

            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, Port);
                _listener.Start();

                _serverTask = Task.Run(() => AcceptConnectionsAsync(_cts.Token));

                StatusChanged?.Invoke(this, EventArgs.Empty);
                App.Logger.WriteLine("WebDashboard", $"Dashboard started at {Url}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WebDashboard", ex);
            }
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _serverTask?.Wait(3000);

                StatusChanged?.Invoke(this, EventArgs.Empty);
                App.Logger.WriteLine("WebDashboard", "Dashboard stopped");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WebDashboard", ex);
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, ct));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("WebDashboard", ex);
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Read HTTP request
                    string? requestLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(requestLine))
                        return;

                    string[] parts = requestLine.Split(' ');
                    string method = parts[0];
                    string path = parts.Length > 1 ? parts[1] : "/";

                    // Read headers
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null && line != "")
                    {
                        // Skip headers
                    }

                    // Generate response
                    string response = GenerateResponse(path);
                    string httpResponse = $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nConnection: close\r\n\r\n{response}";

                    byte[] buffer = Encoding.UTF8.GetBytes(httpResponse);
                    await stream.WriteAsync(buffer, 0, buffer.Length, ct);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WebDashboard", ex);
            }
        }

        private string GenerateResponse(string path)
        {
            if (path == "/api/instances")
            {
                return GetInstancesJson();
            }

            return GetDashboardHtml();
        }

        private string GetInstancesJson()
        {
            var instances = InstanceManager.Instance.RunningInstances.Select(i => new
            {
                i.ProcessId,
                i.Name,
                i.AccountName,
                i.Runtime
            });

            var accounts = App.Settings.Prop.MultiInstanceAccounts.Select(a => new
            {
                a.Name,
                a.AutoLaunch,
                a.ProcessId,
                IsRunning = a.ProcessId > 0
            });

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                instances,
                accounts,
                stats = new
                {
                    totalRunning = InstanceManager.Instance.RunningInstances.Count,
                    totalAccounts = App.Settings.Prop.MultiInstanceAccounts.Count,
                    antiAfk = AntiAfkService.Instance.IsRunning,
                    screenshots = ScreenshotService.Instance.IsRunning,
                    logging = App.Settings.Prop.InstanceLoggingEnabled
                }
            });
        }

        private string GetDashboardHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <title>RinsTrap Dashboard</title>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #1a1a2e; color: #eee; padding: 20px; }
        .container { max-width: 1200px; margin: 0 auto; }
        h1 { text-align: center; margin-bottom: 20px; color: #00d4ff; }
        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-bottom: 20px; }
        .stat-card { background: #16213e; padding: 20px; border-radius: 10px; text-align: center; }
        .stat-value { font-size: 2em; font-weight: bold; color: #00d4ff; }
        .stat-label { color: #888; margin-top: 5px; }
        .section { background: #16213e; padding: 20px; border-radius: 10px; margin-bottom: 20px; }
        .section h2 { margin-bottom: 15px; color: #00d4ff; }
        .instance-list { display: grid; gap: 10px; }
        .instance-item { background: #1a1a2e; padding: 15px; border-radius: 8px; display: flex; justify-content: space-between; align-items: center; }
        .instance-item.running { border-left: 4px solid #00ff88; }
        .instance-item.error { border-left: 4px solid #ff4444; }
        .instance-item.idle { border-left: 4px solid #888; }
        .status-badge { padding: 5px 10px; border-radius: 15px; font-size: 0.85em; }
        .status-badge.running { background: #00ff88; color: #000; }
        .status-badge.error { background: #ff4444; color: #fff; }
        .status-badge.idle { background: #888; color: #fff; }
        .actions { display: flex; gap: 10px; margin-top: 15px; }
        .btn { padding: 10px 20px; border: none; border-radius: 5px; cursor: pointer; font-weight: bold; }
        .btn-primary { background: #00d4ff; color: #000; }
        .btn-danger { background: #ff4444; color: #fff; }
        .btn-success { background: #00ff88; color: #000; }
        .btn:hover { opacity: 0.8; }
        .refresh-btn { float: right; }
        .log-container { max-height: 300px; overflow-y: auto; background: #0d1117; padding: 10px; border-radius: 5px; font-family: monospace; font-size: 0.9em; }
        .log-entry { padding: 3px 0; border-bottom: 1px solid #21262d; }
        .log-time { color: #8b949e; }
        .log-type { color: #58a6ff; margin: 0 10px; }
        .log-message { color: #c9d1d9; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>RinsTrap Dashboard</h1>
        
        <div class='stats' id='stats'></div>
        
        <div class='section'>
            <h2>Running Instances <button class='btn btn-primary refresh-btn' onclick='refresh()'>Refresh</button></h2>
            <div class='instance-list' id='instances'></div>
        </div>
        
        <div class='section'>
            <h2>Quick Actions</h2>
            <div class='actions'>
                <button class='btn btn-primary' onclick='launchAll()'>Launch All</button>
                <button class='btn btn-danger' onclick='killAll()'>Kill All</button>
                <button class='btn btn-success' onclick='restartAll()'>Restart All</button>
            </div>
        </div>
        
        <div class='section'>
            <h2>Recent Logs</h2>
            <div class='log-container' id='logs'></div>
        </div>
    </div>
    
    <script>
        async function refresh() {
            const res = await fetch('/api/instances');
            const data = await res.json();
            
            // Update stats
            document.getElementById('stats').innerHTML = `
                <div class='stat-card'><div class='stat-value'>${data.stats.totalRunning}</div><div class='stat-label'>Running Instances</div></div>
                <div class='stat-card'><div class='stat-value'>${data.stats.totalAccounts}</div><div class='stat-label'>Total Accounts</div></div>
                <div class='stat-card'><div class='stat-value'>${data.stats.antiAfk ? 'ON' : 'OFF'}</div><div class='stat-label'>Anti-AFK</div></div>
                <div class='stat-card'><div class='stat-value'>${data.stats.screenshots ? 'ON' : 'OFF'}</div><div class='stat-label'>Screenshots</div></div>
            `;
            
            // Update instances
            document.getElementById('instances').innerHTML = data.instances.map(i => `
                <div class='instance-item ${i.Status.includes('Running') ? 'running' : i.Status.includes('Error') ? 'error' : 'idle'}'>
                    <div>
                        <strong>${i.DisplayName || i.InstanceName || 'Instance ' + i.ProcessId}</strong>
                        <div style='color: #888; font-size: 0.9em;'>PID: ${i.ProcessId}</div>
                    </div>
                    <span class='status-badge ${i.Status.includes('Running') ? 'running' : i.Status.includes('Error') ? 'error' : 'idle'}'>${i.Status}</span>
                </div>
            `).join('');
        }
        
        function launchAll() { fetch('/api/launch-all', {method: 'POST'}); }
        function killAll() { fetch('/api/kill-all', {method: 'POST'}); }
        function restartAll() { fetch('/api/restart-all', {method: 'POST'}); }
        
        refresh();
        setInterval(refresh, 5000);
    </script>
</body>
</html>";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _cts?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
