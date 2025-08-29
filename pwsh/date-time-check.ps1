param (
    [switch]$CheckService,
    [switch]$CheckConfig,
    [switch]$CheckStatus,
    [switch]$TestConnectivity,
    [switch]$CheckRegistry,
    [switch]$All,
    [switch]$SyncNow,
    [int]$PollInterval,
    [switch]$Query
)

function Log($message) {
    Write-Host $message
}

Log "=== Time Sync Health Check ==="
Log "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

Log "=== NTP Status ==="
w32tm /query /status | ForEach-Object { Log $_ }
Log ""

# Query NTP status/configuration if requested
if ($Query) {
    Log "=== NTP Status ==="
    w32tm /query /status | ForEach-Object { Log $_ }
    Log ""
    Log "=== NTP Configuration ==="
    w32tm /query /configuration | ForEach-Object { Log $_ }
    Log ""
    return
}

Log "=== NTP Configuration ==="
w32tm /query /configuration | ForEach-Object { Log $_ }
Log ""

# Update polling interval if specified
if ($PollInterval) {
    Log "🔄 Updating NTP polling interval to $PollInterval seconds..."
    w32tm /config /update /specialpollinterval:$PollInterval | ForEach-Object { Log $_ }
    Restart-Service w32time
    Log "Polling interval updated and service restarted."
    Log ""
}

# 1. Check Windows Time Service
if ($CheckService -or $All) {
    $service = Get-Service w32time
    $startMode = (Get-WmiObject -Class Win32_Service -Filter "Name='w32time'").StartMode
    Log "🛠️ Service Status: $($service.Status)"
    Log "🛠️ Startup Type: $startMode"
    Log ""
}

# 2. Check NTP Configuration
if ($CheckConfig -or $All) {
    Log "⚙️ NTP Configuration:"
    w32tm /query /configuration | ForEach-Object { Log $_ }
    Log ""
}

# 3. Check Sync Status
if ($CheckStatus -or $All) {
    Log "📊 Sync Status:"
    w32tm /query /status | ForEach-Object { Log $_ }
    Log ""
}

# 4. Test Connectivity to NTP Servers
if ($TestConnectivity -or $All) {
    $servers = @("time.windows.com", "pool.ntp.org")
    foreach ($server in $servers) {
        $test = Test-NetConnection -ComputerName $server -Port 123
        $result = if ($test.TcpTestSucceeded) { "✅ Reachable" } else { "❌ Unreachable" }
        Log "🌐 NTP Server '$server': $result"
    }
    Log ""
}

# 5. Check Registry Parameters
if ($CheckRegistry -or $All) {
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Parameters"
    $reg = Get-ItemProperty $regPath
    Log "🧬 Registry Parameters:"
    Log "Type: $($reg.Type)"
    Log "NtpServer: $($reg.NtpServer)"
    Log ""
}

# 6. Sync Time Now
if ($SyncNow) {
    Log "⏰ Syncing time now..."
    $service = Get-Service w32time
    if ($service.Status -ne 'Running') {
        Log "Windows Time service is not running. Starting service..."
        Start-Service w32time
        Log "Service started."
    }
    w32tm /resync | ForEach-Object { Log $_ }
    Log ""
}