param(
    [string]$ApkPath = 'Builds/MobilePrototype/GravityGame-Prototype.apk',
    [string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.5.9f1/Editor',
    [int]$RequiredTargetSdk = 36,
    [switch]$RequireRelease,
    [string]$ReportPath = 'Builds/MobilePrototype/android-apk-audit.json'
)

$ErrorActionPreference = 'Stop'
$apk = (Resolve-Path -LiteralPath $ApkPath).Path
$androidTools = Join-Path $UnityEditorPath 'Data/PlaybackEngines/AndroidPlayer'
$buildTools = Join-Path $androidTools 'SDK/build-tools/36.0.0'
$aapt = Join-Path $buildTools 'aapt.exe'
$badging = & $aapt dump badging $apk
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect the Android package.' }
$badgingText = $badging -join "`n"
$packageName = [regex]::Match($badgingText, "package: name='([^']+)'").Groups[1].Value
$targetSdk = [int][regex]::Match($badgingText, "targetSdkVersion:'(\d+)'").Groups[1].Value
$minSdk = [int][regex]::Match($badgingText, "sdkVersion:'(\d+)'").Groups[1].Value
$debuggable = $badgingText.Contains('application-debuggable')
if ($targetSdk -lt $RequiredTargetSdk) { throw "Target SDK $targetSdk is below $RequiredTargetSdk." }
if ($badgingText -notmatch "native-code:.*'arm64-v8a'") { throw 'ARM64 support is missing.' }
if ([string]::IsNullOrWhiteSpace($packageName) -or $packageName -match 'UnityTechnologies|unity\.template') {
    throw 'The package still uses an empty or Unity-template application identifier.'
}
if ($RequireRelease -and $debuggable) { throw 'This is a debuggable prototype, not a release build.' }

& (Join-Path $buildTools 'zipalign.exe') -c -P 16 4 $apk
if ($LASTEXITCODE -ne 0) { throw 'APK native-library ZIP alignment failed the 16 KB check.' }
& (Join-Path $androidTools 'OpenJDK/bin/java.exe') -jar (Join-Path $buildTools 'lib/apksigner.jar') verify $apk
if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed.' }

# Read ELF headers directly from ZIP streams; native libraries are never extracted or modified.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$libraries = @()
$archive = [System.IO.Compression.ZipFile]::OpenRead($apk)
try {
    foreach ($entry in $archive.Entries) {
        if ($entry.FullName -notmatch '^lib/arm64-v8a/.*\.so$') { continue }
        $stream = $entry.Open()
        $reader = [System.IO.BinaryReader]::new($stream)
        try {
            $header = $reader.ReadBytes(64)
            if ($header.Length -ne 64 -or $header[0] -ne 127 -or $header[1] -ne 69 -or
                $header[2] -ne 76 -or $header[3] -ne 70 -or $header[4] -ne 2 -or $header[5] -ne 1) {
                throw "Unsupported ARM64 ELF header: $($entry.FullName)"
            }
            $tableOffset = [BitConverter]::ToUInt64($header, 32)
            $entrySize = [BitConverter]::ToUInt16($header, 54)
            $entryCount = [BitConverter]::ToUInt16($header, 56)
            $tableEnd = $tableOffset + $entrySize * $entryCount
            if ($tableOffset -lt 64 -or $entrySize -lt 56 -or $tableEnd -gt 1048576) {
                throw "Invalid ELF program header table: $($entry.FullName)"
            }
            $tail = $reader.ReadBytes([int]$tableEnd - 64)
            if ($tail.Length -ne [int]$tableEnd - 64) { throw 'Truncated ELF program header table.' }
            $headers = [byte[]]($header + $tail)
            $alignments = @()
            for ($index = 0; $index -lt $entryCount; $index++) {
                $offset = [int]($tableOffset + $index * $entrySize)
                if ([BitConverter]::ToUInt32($headers, $offset) -ne 1) { continue }
                $fileOffset = [BitConverter]::ToUInt64($headers, $offset + 8)
                $virtualAddress = [BitConverter]::ToUInt64($headers, $offset + 16)
                $alignment = [BitConverter]::ToUInt64($headers, $offset + 48)
                if ($alignment -lt 16384 -or ($fileOffset % 16384) -ne ($virtualAddress % 16384)) {
                    throw "16 KB ELF segment alignment failed: $($entry.FullName)"
                }
                $alignments += $alignment
            }
            if ($alignments.Count -eq 0) { throw "No ELF LOAD segments: $($entry.FullName)" }
            $libraries += [pscustomobject]@{ Name = $entry.FullName; LoadAlignments = $alignments }
        }
        finally { $reader.Dispose(); $stream.Dispose() }
    }
}
finally { $archive.Dispose() }
if ($libraries.Count -eq 0) { throw 'No ARM64 native libraries were found.' }
$permissions = & $aapt dump permissions $apk
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect Android permissions.' }
$report = [pscustomobject]@{
    Apk = $apk
    Sha256 = (Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
    Package = $packageName
    MinimumSdk = $minSdk
    TargetSdk = $targetSdk
    Debuggable = $debuggable
    SignatureVerified = $true
    Zip16KBAligned = $true
    Arm64Libraries = $libraries
    Permissions = @($permissions | Where-Object { $_ -like 'uses-permission:*' })
    Scope = 'Package checks only. Device behavior and Google Play submission are not certified.'
}
$reportFile = [System.IO.Path]::GetFullPath($ReportPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($reportFile)) | Out-Null
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportFile -Encoding utf8
Write-Output "ANDROID_APK_AUDIT_PASSED: $packageName; SDK $minSdk-$targetSdk; $($libraries.Count) ARM64 libraries; signature and 16 KB alignment verified."
if ($debuggable) { Write-Output 'Development APK: use a signed, non-development AAB for Google Play.' }
