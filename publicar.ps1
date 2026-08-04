<#
.SYNOPSIS
    Publica uma versao do CronoAula no GitHub, deixando apenas ela disponivel.

.DESCRIPTION
    Faz o caminho completo de uma versao nova:

      1. confere que a arvore de trabalho esta limpa e sincronizada;
      2. roda os testes e gera o executavel unico;
      3. cria a tag da versao e envia para o GitHub;
      4. publica a Release com o executavel anexado;
      5. REMOVE as releases e tags anteriores, deixando so a versao atual.

    O passo 5 e o motivo deste script existir: sem ele, versoes antigas
    continuam disponiveis para download, e um professor pode baixar uma versao
    com defeito ja corrigido.

    O que NAO e apagado: os commits. O historico do codigo permanece intacto,
    e qualquer versao antiga pode ser reconstruida a partir dele. O que sai de
    circulacao e apenas o executavel pronto.

.PARAMETER Versao
    Numero da versao, sem o "v". Exemplo: 1.1.0

.PARAMETER ManterAnteriores
    Publica a versao nova sem remover as anteriores.

.PARAMETER Simular
    Mostra tudo o que seria feito, sem alterar nada no GitHub.

.EXAMPLE
    .\publicar.ps1 -Versao 1.2.0 -Simular
    .\publicar.ps1 -Versao 1.2.0

.NOTES
    Exige o GitHub CLI autenticado uma unica vez:
        winget install GitHub.cli
        gh auth login
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Versao,

    [switch]$ManterAnteriores,
    [switch]$Simular
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$tag = "v$Versao"
$exe = Join-Path $PSScriptRoot "dist\CronoAula.exe"

function Passo($texto) { Write-Host "`n=== $texto ===" -ForegroundColor Cyan }
function Ok($texto)    { Write-Host "  $texto" -ForegroundColor Green }
function Aviso($texto) { Write-Host "  $texto" -ForegroundColor Yellow }
function Parar($texto) { Write-Host "`nERRO: $texto" -ForegroundColor Red; exit 1 }

# ---------------------------------------------------------------------------
Passo "Ferramentas"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "  O GitHub CLI nao esta instalado." -ForegroundColor Red
    Write-Host "  Instale e autentique uma unica vez:" -ForegroundColor Yellow
    Write-Host "      winget install GitHub.cli"
    Write-Host "      gh auth login"
    exit 1
}

# gh auth status devolve 1 quando nao ha login, e escreve na saida de erro.
#
# Cuidado com o PowerShell 5.1: redirecionar a saida de erro de um programa
# externo com ErrorActionPreference = Stop transforma cada linha em excecao,
# mesmo quando o programa terminou bem. Por isso baixamos a preferencia so
# nesta chamada e olhamos apenas o codigo de saida.
$preferenciaAnterior = $ErrorActionPreference
$ErrorActionPreference = "Continue"
gh auth status *> $null
$semLogin = ($LASTEXITCODE -ne 0)
$ErrorActionPreference = $preferenciaAnterior

if ($semLogin) {
    Write-Host "  O GitHub CLI esta instalado, mas sem login." -ForegroundColor Red
    Write-Host "  Rode uma unica vez, no seu terminal:" -ForegroundColor Yellow
    Write-Host "      gh auth login"
    Write-Host "  Ele abre o navegador para autorizar. Depois disso este script"
    Write-Host "  funciona sozinho nas proximas versoes."
    exit 1
}
Ok "GitHub CLI pronto e autenticado"

# ---------------------------------------------------------------------------
Passo "Estado do repositorio"

$pendentes = git status --porcelain
if ($pendentes) {
    Write-Host "  Ha alteracoes nao registradas:" -ForegroundColor Red
    $pendentes | ForEach-Object { Write-Host "    $_" }
    Parar "Faca o commit antes de publicar."
}
Ok "Arvore de trabalho limpa"

git fetch origin --tags --prune --quiet
$naoEnviados = git log origin/main..main --oneline
if ($naoEnviados) {
    Aviso "Commits ainda nao enviados:"
    $naoEnviados | ForEach-Object { Write-Host "    $_" }
}

$soNoRemoto = git log main..origin/main --oneline
if ($soNoRemoto) {
    Write-Host "  O GitHub tem commits que voce nao possui:" -ForegroundColor Red
    $soNoRemoto | ForEach-Object { Write-Host "    $_" }
    Parar "Rode 'git pull --rebase' antes de publicar."
}

# ---------------------------------------------------------------------------
Passo "Versao declarada no codigo"

$csproj = Get-Content ".\CronoAula\CronoAula.csproj" -Raw
if ($csproj -notmatch "<Version>$([regex]::Escape($Versao))</Version>") {
    Parar "O CronoAula.csproj nao declara a versao $Versao. Atualize <Version>, <FileVersion> e <AssemblyVersion>."
}
Ok "csproj declara $Versao"

# ---------------------------------------------------------------------------
Passo "Testes e geracao do executavel"

if ($Simular) {
    Aviso "simulacao: pularia a geracao"
} else {
    & "$PSScriptRoot\build.ps1"
    if ($LASTEXITCODE -ne 0) { Parar "A geracao falhou." }

    if (-not (Test-Path $exe)) { Parar "Executavel nao encontrado em $exe" }

    $versaoDoExe = (Get-Item $exe).VersionInfo.FileVersion
    if ($versaoDoExe -notlike "$Versao*") {
        Parar "O executavel gerado informa versao $versaoDoExe, e nao $Versao."
    }
    Ok "Executavel confere: $versaoDoExe"
}

# ---------------------------------------------------------------------------
Passo "Versoes atualmente publicadas"

$releasesJson = gh release list --limit 100 --json tagName,name 2>$null
$releases = if ($releasesJson) { $releasesJson | ConvertFrom-Json } else { @() }

if ($releases.Count -eq 0) { Ok "nenhuma" }
else { $releases | ForEach-Object { Write-Host "    $($_.tagName)  $($_.name)" } }

$anteriores = $releases | Where-Object { $_.tagName -ne $tag }

# ---------------------------------------------------------------------------
Passo "Publicando $tag"

$notas = @"
## CronoAula $Versao

Baixe o **CronoAula.exe** abaixo. Nao precisa instalar nada: e um arquivo unico
que ja traz tudo dentro dele.

Requisitos: Windows 10 ou 11, 64 bits.

Na primeira execucao o Windows pode exibir um aviso, porque o programa nao tem
assinatura digital. Clique em **Mais informacoes** e depois em **Executar assim
mesmo**. O README explica em detalhe, na secao Distribuicao.
"@

if ($Simular) {
    Aviso "simulacao: criaria a tag $tag e a release com o executavel"
} else {
    # A tag pode ja existir apontando para o commit errado.
    $tagRemota = git ls-remote --tags origin $tag
    if ($tagRemota) {
        Aviso "A tag $tag ja existe no GitHub. Sera reposicionada no commit atual."
        gh release delete $tag --yes --cleanup-tag 2>$null | Out-Null
        git push origin ":refs/tags/$tag" 2>$null | Out-Null
    }

    git tag -f -a $tag -m "CronoAula $Versao" | Out-Null
    git push origin main --quiet
    git push origin $tag --force --quiet
    Ok "Codigo e tag enviados"

    $arquivoNotas = Join-Path $env:TEMP "cronoaula-notas-$Versao.md"
    Set-Content -Path $arquivoNotas -Value $notas -Encoding UTF8

    gh release create $tag $exe --title "CronoAula $Versao" --notes-file $arquivoNotas
    if ($LASTEXITCODE -ne 0) { Parar "Nao foi possivel criar a release." }
    Remove-Item $arquivoNotas -ErrorAction SilentlyContinue
    Ok "Release $tag publicada com o executavel"
}

# ---------------------------------------------------------------------------
Passo "Versoes anteriores"

if ($ManterAnteriores) {
    Aviso "mantidas por opcao (-ManterAnteriores)"
}
elseif ($anteriores.Count -eq 0) {
    Ok "nenhuma para remover"
}
else {
    foreach ($r in $anteriores) {
        if ($Simular) {
            Aviso "simulacao: removeria a release e a tag $($r.tagName)"
        } else {
            # --cleanup-tag remove a tag junto com a release.
            gh release delete $r.tagName --yes --cleanup-tag
            if ($LASTEXITCODE -eq 0) { Ok "removida: $($r.tagName)" }
            else { Aviso "nao foi possivel remover $($r.tagName)" }
        }
    }
}

# ---------------------------------------------------------------------------
Passo "Resultado"

if ($Simular) {
    Write-Host "  Nada foi alterado. Rode sem -Simular para publicar de verdade."
} else {
    gh release list --limit 20
    Write-Host ""
    Write-Host "  https://github.com/manfredjr/cronoaula/releases/latest" -ForegroundColor Cyan
}
Write-Host ""
