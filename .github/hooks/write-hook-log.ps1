param(
    [string]$LogPath = "C:\Faks\.NET\ASP.NET---World-of-Warcraft-builds\lab-1\agent_log.txt"
)

$payload = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($payload)) {
    exit 0
}

$logDir = Split-Path -Parent $LogPath
if ($logDir -and -not (Test-Path -LiteralPath $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

Add-Content -Path $LogPath -Value $payload -Encoding utf8
