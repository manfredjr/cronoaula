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

<#
    Executa git ou gh sem que o PowerShell trate a saida deles como falha.

    Motivo, e a razao de este script ja ter quebrado no meio de uma publicacao:
    o git escreve o progresso na saida de ERRO, nao na saida normal. No
    PowerShell 5.1, com ErrorActionPreference = "Stop", cada linha vinda dali
    vira uma excecao e derruba o script, mesmo quando o comando terminou bem.

    A consequencia foi real: o script apagou a tag da versao, o que levou a
    release junto, e abortou antes de recriar. O repositorio ficou so com a
    versao antiga publicada.

    Toda chamada a programa externo neste script passa por aqui. Quem decide se
    houve falha e o codigo de saida, nunca o fato de ter escrito algo.
#>
function Invoke-Externo {
    param(
        [Parameter(Mandatory = $true)][string]$Programa,
        [Parameter(ValueFromRemainingArguments = $true)][string[]]$Argumentos
    )

    # Resolve o executavel de verdade, e nao pelo nome. Sem isto, uma funcao
    # deste script com o mesmo nome do programa seria chamada no lugar dele:
    # o PowerShell resolve funcao antes de aplicativo, e o resultado e uma
    # recursao infinita.
    $caminho = (Get-Command $Programa -CommandType Application -ErrorAction SilentlyContinue |
                Select-Object -First 1).Source
    if (-not $caminho) {
        return [PSCustomObject]@{ Codigo = 127; Saida = @("$Programa nao encontrado"); Ok = $false }
    }

    $anterior = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $saida = & $caminho @Argumentos 2>&1 | ForEach-Object { $_.ToString() }
        return [PSCustomObject]@{
            Codigo = $LASTEXITCODE
            Saida  = @($saida)
            Ok     = ($LASTEXITCODE -eq 0)
        }
    }
    finally {
        $ErrorActionPreference = $anterior
    }
}

# Nomes diferentes dos programas, de proposito, pelo motivo explicado acima.
function ExecGit { Invoke-Externo -Programa "git" @args }
function ExecGh  { Invoke-Externo -Programa "gh"  @args }

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
$autenticacao = ExecGh auth status
if (-not $autenticacao.Ok) {
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

$pendentes = (ExecGit status --porcelain).Saida | Where-Object { $_ }
if ($pendentes) {
    Write-Host "  Ha alteracoes nao registradas:" -ForegroundColor Red
    $pendentes | ForEach-Object { Write-Host "    $_" }
    Parar "Faca o commit antes de publicar."
}
Ok "Arvore de trabalho limpa"

ExecGit fetch origin --tags --prune --quiet | Out-Null

$naoEnviados = (ExecGit log origin/main..main --oneline).Saida | Where-Object { $_ }
if ($naoEnviados) {
    Aviso "Commits ainda nao enviados:"
    $naoEnviados | ForEach-Object { Write-Host "    $_" }
}

$soNoRemoto = (ExecGit log main..origin/main --oneline).Saida | Where-Object { $_ }
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

$consulta = ExecGh release list --limit 100 --json tagName,name
$releases = @()
if ($consulta.Ok -and $consulta.Saida) {
    try { $releases = @($consulta.Saida -join "" | ConvertFrom-Json) } catch { $releases = @() }
}

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
    # Ordem importa. A tag so e removida depois que o codigo novo ja esta no
    # GitHub, e a release e recriada logo em seguida. Assim, se algo falhar no
    # meio, o repositorio nunca fica sem nenhuma versao publicada por muito
    # tempo, que foi o que aconteceu quando este script quebrou.
    ExecGit push origin main --quiet | Out-Null

    $releaseExiste = (ExecGh release view $tag).Ok
    if ($releaseExiste) {
        Aviso "A release $tag ja existe. Sera refeita com o executavel atual."
        $r = ExecGh release delete $tag --yes --cleanup-tag
        if (-not $r.Ok) { Parar "Nao foi possivel remover a release anterior de $tag." }
    }

    # A tag pode existir sem release, apontando para o commit errado.
    if ((ExecGit ls-remote --tags origin $tag).Saida | Where-Object { $_ }) {
        ExecGit push origin ":refs/tags/$tag" | Out-Null
    }

    ExecGit tag -f -a $tag -m "CronoAula $Versao" | Out-Null
    $envio = ExecGit push origin $tag --force
    if (-not $envio.Ok) { Parar "Nao foi possivel enviar a tag $tag." }
    Ok "Codigo e tag enviados"

    $arquivoNotas = Join-Path $env:TEMP "cronoaula-notas-$Versao.md"
    Set-Content -Path $arquivoNotas -Value $notas -Encoding UTF8

    $criacao = ExecGh release create $tag $exe --title "CronoAula $Versao" --notes-file $arquivoNotas
    if (-not $criacao.Ok) {
        $criacao.Saida | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        Parar "Nao foi possivel criar a release."
    }
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
            $remocao = ExecGh release delete $r.tagName --yes --cleanup-tag
            if ($remocao.Ok) { Ok "removida: $($r.tagName)" }
            else { Aviso "nao foi possivel remover $($r.tagName)" }
        }
    }
}

# ---------------------------------------------------------------------------
Passo "Resultado"

if ($Simular) {
    Write-Host "  Nada foi alterado. Rode sem -Simular para publicar de verdade."
} else {
    (ExecGh release list --limit 20).Saida | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
    Write-Host "  https://github.com/manfredjr/cronoaula/releases/latest" -ForegroundColor Cyan
}
Write-Host ""
