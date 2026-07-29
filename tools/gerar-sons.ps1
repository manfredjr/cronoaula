<#
.SYNOPSIS
    Gera os dois sons do CronoAula (Assets\aviso.wav e Assets\alerta.wav).

.DESCRIPTION
    aviso.wav  - aviso antecipado. Um toque unico, curto e discreto: serve para
                 dizer "esta acabando" sem interromper a aula.

    alerta.wav - fim do tempo. Padrao de duas notas alternadas, tipo sirene
                 suave, que se destaca do aviso e e reconhecivel a distancia.
                 O aplicativo repete este arquivo varias vezes, com pausa entre
                 as repeticoes, para o professor perceber mesmo se estiver
                 atendendo um aluno longe do computador.

    Ambos sao ondas senoidais com envelope suave (sem cliques nas bordas),
    PCM 16 bits, 44,1 kHz, mono.
#>

[CmdletBinding()]
param(
    [string]$PastaSaida = (Join-Path $PSScriptRoot "..\CronoAula\Assets")
)

$ErrorActionPreference = "Stop"
$script:rate = 44100

function New-Amostras { return New-Object System.Collections.Generic.List[Int16] }

<#
  Acrescenta uma nota. O envelope evita o "clique" que uma senoide cortada
  abruptamente produz: sobe rapido, sustenta e desce suave.
#>
function Add-Nota {
    param(
        [System.Collections.Generic.List[Int16]]$Lista,
        [double]$Freq,
        [double]$Dur,
        [double]$Amp,
        [double]$SubidaPct = 0.12,
        [double]$DescidaPct = 0.35
    )

    $n = [int]($script:rate * $Dur)
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / $script:rate
        $p = $i / $n

        $env = 1.0
        if ($p -lt $SubidaPct) {
            $env = $p / $SubidaPct
        }
        elseif ($p -gt (1.0 - $DescidaPct)) {
            $env = (1.0 - $p) / $DescidaPct
        }

        # Uma pitada do primeiro harmonico deixa o timbre menos "apitado"
        # e mais agradavel numa sala de aula.
        $onda = [Math]::Sin(2 * [Math]::PI * $Freq * $t) +
                0.18 * [Math]::Sin(4 * [Math]::PI * $Freq * $t)

        $v = $onda * $Amp * $env / 1.18
        $Lista.Add([Int16]([Math]::Round([Math]::Max(-1.0, [Math]::Min(1.0, $v)) * 32767)))
    }
}

function Add-Silencio {
    param([System.Collections.Generic.List[Int16]]$Lista, [double]$Dur)
    $n = [int]($script:rate * $Dur)
    for ($i = 0; $i -lt $n; $i++) { $Lista.Add([Int16]0) }
}

function Save-Wav {
    param([System.Collections.Generic.List[Int16]]$Lista, [string]$Caminho)

    $bytes = New-Object byte[] ($Lista.Count * 2)
    for ($i = 0; $i -lt $Lista.Count; $i++) {
        [BitConverter]::GetBytes($Lista[$i]).CopyTo($bytes, $i * 2)
    }

    $fs = [System.IO.File]::Create($Caminho)
    $bw = New-Object System.IO.BinaryWriter($fs)
    $ch = 1; $bits = 16
    $bw.Write([char[]]'RIFF');  $bw.Write([int](36 + $bytes.Length))
    $bw.Write([char[]]'WAVE');  $bw.Write([char[]]'fmt ')
    $bw.Write([int]16);         $bw.Write([int16]1)
    $bw.Write([int16]$ch);      $bw.Write([int]$script:rate)
    $bw.Write([int]($script:rate * $ch * ($bits / 8)))
    $bw.Write([int16]($ch * ($bits / 8)))
    $bw.Write([int16]$bits)
    $bw.Write([char[]]'data');  $bw.Write([int]$bytes.Length)
    $bw.Write($bytes)
    $bw.Close(); $fs.Close()

    $info = Get-Item $Caminho
    $seg = [Math]::Round($Lista.Count / $script:rate, 2)
    Write-Host ("  {0,-12} {1,6:N2}s  {2,8:N0} bytes" -f $info.Name, $seg, $info.Length)
}

[void][System.IO.Directory]::CreateDirectory($PastaSaida)
Write-Host "Gerando sons em $PastaSaida"

# --- aviso.wav: um toque so, discreto -----------------------------------------
# Nota unica em La5 (880 Hz), amplitude baixa. Passa despercebido pela turma,
# mas o professor que esta esperando por ele identifica na hora.
$aviso = New-Amostras
Add-Nota -Lista $aviso -Freq 880 -Dur 0.22 -Amp 0.22
Save-Wav -Lista $aviso -Caminho (Join-Path $PastaSaida "aviso.wav")

# --- alerta.wav: fim do tempo -------------------------------------------------
# Quatro notas alternando Sol5 (784 Hz) e Si5 (988 Hz), no ritmo de uma sirene
# branda. Duas notas distintas se destacam muito mais do ruido de fundo de uma
# sala do que um bip unico, e o intervalo entre elas e consonante, entao chama
# atencao sem soar como alarme de incendio.
$alerta = New-Amostras
foreach ($par in 1..2) {
    Add-Nota -Lista $alerta -Freq 784.00 -Dur 0.20 -Amp 0.34
    Add-Silencio -Lista $alerta -Dur 0.04
    Add-Nota -Lista $alerta -Freq 987.77 -Dur 0.20 -Amp 0.34
    Add-Silencio -Lista $alerta -Dur 0.04
}
# Nota final mais longa: fecha o padrao e deixa claro que a sequencia terminou.
Add-Nota -Lista $alerta -Freq 1174.66 -Dur 0.42 -Amp 0.32 -DescidaPct 0.55
Save-Wav -Lista $alerta -Caminho (Join-Path $PastaSaida "alerta.wav")

Write-Host "Pronto."
