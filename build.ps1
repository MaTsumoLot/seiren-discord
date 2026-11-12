$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $framework 'WPF'
& "$framework\csc.exe" /nologo /platform:x64 /target:winexe "/out:$PSScriptRoot\SeirenDiscord.exe" "/r:$wpf\UIAutomationClient.dll" "/r:$wpf\UIAutomationTypes.dll" "/r:$wpf\WindowsBase.dll" "$PSScriptRoot\SeirenDiscord.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
