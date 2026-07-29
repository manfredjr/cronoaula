<#
.SYNOPSIS
    Gera o executavel unico do CronoAula em .\dist

.DESCRIPTION
    Publica o aplicativo como arquivo unico, autocontido e para Windows x64.
    O resultado roda em qualquer maquina com Windows 10/11 de 64 bits, mesmo
    sem o .NET instalado, e nao precisa de privilegio de administrador.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -SkipTests
#>

[CmdletBinding()]
param(
    # Pula a execucao dos testes (nao recomendado).
    [switch]$SkipTests,

    # Pasta de saida do executavel.
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host ""
Write-Host "=== CronoAula - geracao do executavel ===" -ForegroundColor Cyan
Write-Host ""

# --- Verifica se o SDK do .NET esta disponivel -------------------------------
try {
    $sdkVersion = (dotnet --version).Trim()
    Write-Host "SDK do .NET encontrado: $sdkVersion"
}
catch {
    Write-Host "ERRO: o SDK do .NET nao foi encontrado." -ForegroundColor Red
    Write-Host "Instale o .NET 8 SDK em https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# --- Testes ------------------------------------------------------------------
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "Executando os testes..." -ForegroundColor Cyan
    dotnet test --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "ERRO: os testes falharam. O executavel NAO foi gerado." -ForegroundColor Red
        exit 1
    }
    Write-Host "Testes aprovados." -ForegroundColor Green
}
else {
    Write-Host "Testes ignorados (-SkipTests)." -ForegroundColor Yellow
}

# --- Limpeza da saida anterior ----------------------------------------------
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

# --- Publicacao --------------------------------------------------------------
Write-Host ""
Write-Host "Publicando o executavel unico..." -ForegroundColor Cyan

# Observacao sobre desempenho: PublishReadyToRun foi testado e piorou a partida
# (1133 ms contra 905 ms). Com a compressao do arquivo unico ligada, o executavel
# maior custa mais para descompactar do que se ganha evitando a compilacao JIT.
dotnet publish .\CronoAula\CronoAula.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERRO: a publicacao falhou." -ForegroundColor Red
    exit 1
}

# --- Remove arquivos auxiliares, deixando so o .exe -------------------------
# O .pdb serve para depuracao e nao e necessario para usar o aplicativo.
Get-ChildItem $OutputDir -Filter *.pdb -ErrorAction SilentlyContinue | Remove-Item -Force

# --- Relatorio final ---------------------------------------------------------
$exe = Join-Path $OutputDir "CronoAula.exe"
if (-not (Test-Path $exe)) {
    Write-Host "ERRO: o executavel nao foi encontrado em $exe" -ForegroundColor Red
    exit 1
}

$item = Get-Item $exe
$mb = [Math]::Round($item.Length / 1MB, 1)

# Publica o SHA-256 junto. Quem baixar de um site pode conferir que o arquivo
# nao foi alterado no caminho, e o valor serve para acompanhar denuncias de
# falso positivo junto a Microsoft e aos antivirus.
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash
$arquivoHash = Join-Path $OutputDir "CronoAula.exe.sha256.txt"
Set-Content -Path $arquivoHash -Value "$hash  CronoAula.exe" -Encoding ASCII

# Aviso util: sem assinatura digital, o SmartScreen reclama em maquinas que
# baixarem o arquivo pela internet.
$assinatura = (Get-AuthenticodeSignature $exe).Status

Write-Host ""
Write-Host "=== Pronto ===" -ForegroundColor Green
Write-Host "Executavel : $($item.FullName)"
Write-Host "Tamanho    : $mb MB"
Write-Host "SHA-256    : $hash"
Write-Host "Assinatura : $assinatura"
Write-Host ""

if ($assinatura -ne "Valid") {
    Write-Host "AVISO: executavel sem assinatura digital." -ForegroundColor Yellow
    Write-Host "  Rodando a partir de um pen drive ou pasta de rede, funciona normalmente." -ForegroundColor Yellow
    Write-Host "  Baixado de um site, o Windows exibira aviso do SmartScreen." -ForegroundColor Yellow
    Write-Host "  Veja a secao 'Distribuicao' do README para as alternativas." -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Copie o CronoAula.exe para qualquer PC com Windows 10/11 (64 bits) e execute." -ForegroundColor Cyan
Write-Host ""
