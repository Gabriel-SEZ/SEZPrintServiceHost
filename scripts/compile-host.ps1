$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot 'src\SezPrintServiceHost.cs'
$dist = Join-Path $repoRoot 'dist'
$output = Join-Path $dist 'SEZPrintServiceHost.exe'

if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Código-fonte do serviço não encontrado: $source"
}

$candidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $csc) {
    throw 'Compilador C# do .NET Framework não encontrado. Use Windows com .NET Framework 4.x ou GitHub Actions (windows-latest).'
}

New-Item -ItemType Directory -Path $dist -Force | Out-Null
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue

$compilerArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/debug-',
    '/warn:4',
    "/out:$output",
    '/reference:System.dll',
    '/reference:System.ServiceProcess.dll',
    $source
)

Write-Host "Compilador: $csc"
Write-Host "Fonte: $source"
& $csc @compilerArgs
if ($LASTEXITCODE -ne 0) {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
    throw "Falha ao compilar SEZPrintServiceHost.exe. Código: $LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath $output -PathType Leaf)) {
    throw "Compilação terminou sem gerar: $output"
}

$file = Get-Item -LiteralPath $output
if ($file.Length -lt 4096) {
    throw "Executável gerado parece incompleto: $($file.Length) bytes"
}

try {
    [System.Reflection.AssemblyName]::GetAssemblyName($output) | Out-Null
}
catch {
    Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
    throw "O arquivo gerado não é um assembly .NET válido: $($_.Exception.Message)"
}

$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
Set-Content -LiteralPath (Join-Path $dist 'SEZPrintServiceHost.sha256') -Value "$hash  SEZPrintServiceHost.exe" -Encoding ASCII
Write-Host 'COMPILAÇÃO CONCLUÍDA'
Write-Host "Arquivo: $output"
Write-Host "Tamanho: $($file.Length) bytes"
Write-Host "SHA256: $hash"
