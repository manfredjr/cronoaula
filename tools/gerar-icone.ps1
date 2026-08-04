<#
.SYNOPSIS
    Gera o icone do CronoAula (Assets\cronoaula.ico).

.DESCRIPTION
    Desenha um mostrador de relogio e empacota varios tamanhos em um unico .ico.
    Rode este script apenas se quiser alterar o desenho do icone; o arquivo
    gerado ja fica versionado junto com o projeto.
#>

[CmdletBinding()]
param(
    [string]$Saida = (Join-Path $PSScriptRoot "..\CronoAula\Assets\cronoaula.ico")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

# Tamanhos que o Windows usa: barra de tarefas, Alt+Tab, Explorer, telas 4K.
$tamanhos = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

function New-IconeRelogio {
    param([int]$Tam)

    $bmp = New-Object System.Drawing.Bitmap($Tam, $Tam, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Escala relativa: tudo e proporcional ao tamanho, para o desenho
    # continuar legivel tanto em 16 px quanto em 256 px.
    $m = $Tam / 256.0
    $margem = 8 * $m
    $d = $Tam - (2 * $margem)

    # Corpo do relogio no gradiente da marca MT - Manfred Tecnologia:
    # verde claro (#6AAF21) descendo para verde escuro (#04511F), a 135 graus,
    # reproduzindo o gradiente oficial do logo.
    $verdeClaro = [System.Drawing.Color]::FromArgb(255, 106, 175, 33)   # 6AAF21
    $verdeEscuro = [System.Drawing.Color]::FromArgb(255, 4, 81, 31)     # 04511F
    $branco = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)

    $rect = New-Object System.Drawing.RectangleF($margem, $margem, $d, $d)

    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect, $verdeClaro, $verdeEscuro, 135.0)
    $g.FillEllipse($grad, $rect)
    $grad.Dispose()

    # Anel externo branco fino: garante contraste sobre fundos claros ou escuros
    # sem engordar a silhueta nos tamanhos pequenos.
    $penAnel = New-Object System.Drawing.Pen($branco, [float](10 * $m))
    $g.DrawEllipse($penAnel, $rect)
    $penAnel.Dispose()

    $cx = $Tam / 2.0
    $cy = $Tam / 2.0

    # Marcas das horas so a partir de 64 px. Abaixo disso viram sujeira e
    # empastelam o desenho.
    if ($Tam -ge 64) {
        $penMarca = New-Object System.Drawing.Pen($branco, [float](10 * $m))
        $rInt = ($d / 2.0) - (28 * $m)
        $rExt = ($d / 2.0) - (44 * $m)
        foreach ($ang in 0, 90, 180, 270) {
            $rad = $ang * [Math]::PI / 180.0
            $x1 = $cx + $rInt * [Math]::Cos($rad); $y1 = $cy + $rInt * [Math]::Sin($rad)
            $x2 = $cx + $rExt * [Math]::Cos($rad); $y2 = $cy + $rExt * [Math]::Sin($rad)
            $g.DrawLine($penMarca, [float]$x1, [float]$y1, [float]$x2, [float]$y2)
        }
        $penMarca.Dispose()
    }

    # Ponteiros marcando 3:00, formando um angulo reto.
    #
    # A primeira versao usava a pose classica de 10h10, mas com tracos grossos
    # os dois ponteiros formavam um "V" que, em 16 px, era lido como um sinal
    # de confirmacao em vez de um relogio. O angulo reto nao tem essa ambiguidade.
    $penHora = New-Object System.Drawing.Pen($branco, [float](22 * $m))
    $penHora.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penHora.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penMin = New-Object System.Drawing.Pen($branco, [float](18 * $m))
    $penMin.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penMin.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    # Ponteiro das horas -> 3h (para a direita), mais curto
    $lenH = ($d / 2.0) - (86 * $m)
    $g.DrawLine($penHora, [float]$cx, [float]$cy, [float]($cx + $lenH), [float]$cy)

    # Ponteiro dos minutos -> 12h (para cima), mais longo
    $lenM = ($d / 2.0) - (56 * $m)
    $g.DrawLine($penMin, [float]$cx, [float]$cy, [float]$cx, [float]($cy - $lenM))

    $penHora.Dispose(); $penMin.Dispose()

    # Pino central
    if ($Tam -ge 32) {
        $rPino = 13 * $m
        $brushPino = New-Object System.Drawing.SolidBrush($branco)
        $g.FillEllipse($brushPino, [float]($cx - $rPino), [float]($cy - $rPino),
            [float]($rPino * 2), [float]($rPino * 2))
        $brushPino.Dispose()
    }

    $g.Dispose()
    return $bmp
}

# --- Monta o arquivo .ico ----------------------------------------------------
# Formato: ICONDIR (6 bytes) + uma ICONDIRENTRY (16 bytes) por imagem +
# os dados das imagens.
#
# Sobre o formato de cada imagem: o .ico aceita PNG (Vista em diante) ou o DIB
# classico. Aqui usamos DIB em TODAS as resolucoes, inclusive 256 px, por duas
# razoes concretas descobertas testando:
#
#  1. A classe System.Drawing.Icon do .NET nao le entradas PNG e falha ao abrir
#     um .ico feito so de PNG.
#
#  2. Mais grave: com uma entrada PNG dentro do icone, o Smart App Control do
#     Windows 11 passa a BLOQUEAR o executavel e a DLL do projeto
#     ("Uma politica de Controle de Aplicativo bloqueou este arquivo",
#     0x800711C7). Trocando a entrada de PNG para DIB, o bloqueio desaparece.
#     Como o programa e distribuido sem assinatura digital, nao vale a pena
#     economizar alguns KB ao custo de nao abrir na maquina do professor.
#
# O preco e um arquivo maior (o 256 px sozinho ocupa ~270 KB), irrelevante
# perto de um executavel de 63 MB.

function ConvertTo-Dib {
    param([System.Drawing.Bitmap]$Bmp)

    $w = $Bmp.Width
    $h = $Bmp.Height

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # BITMAPINFOHEADER. A altura e o dobro porque o DIB de icone guarda a
    # imagem (XOR) seguida da mascara de transparencia (AND).
    $bw.Write([UInt32]40)          # biSize
    $bw.Write([Int32]$w)           # biWidth
    $bw.Write([Int32]($h * 2))     # biHeight
    $bw.Write([UInt16]1)           # biPlanes
    $bw.Write([UInt16]32)          # biBitCount
    $bw.Write([UInt32]0)           # biCompression = BI_RGB
    $bw.Write([UInt32]($w * $h * 4))
    $bw.Write([Int32]0); $bw.Write([Int32]0)
    $bw.Write([UInt32]0); $bw.Write([UInt32]0)

    # Dados XOR: BGRA, de baixo para cima (convencao do DIB).
    $dados = $Bmp.LockBits(
        (New-Object System.Drawing.Rectangle(0, 0, $w, $h)),
        [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    $linha = New-Object byte[] ($w * 4)
    for ($y = $h - 1; $y -ge 0; $y--) {
        $ptr = [IntPtr]::Add($dados.Scan0, $y * $dados.Stride)
        [System.Runtime.InteropServices.Marshal]::Copy($ptr, $linha, 0, $w * 4)
        $bw.Write($linha)
    }
    $Bmp.UnlockBits($dados)

    # Mascara AND: com 32 bits por pixel quem manda e o canal alfa, entao a
    # mascara vai zerada (tudo opaco). Cada linha e alinhada em 4 bytes.
    $bytesPorLinha = [Math]::Floor(($w + 31) / 32) * 4
    $vazia = New-Object byte[] $bytesPorLinha
    for ($y = 0; $y -lt $h; $y++) { $bw.Write($vazia) }

    $bw.Flush()
    $saida = $ms.ToArray()
    $bw.Dispose(); $ms.Dispose()

    # A virgula impede que o PowerShell "desenrole" o array e o devolva como
    # Object[] pelo pipeline: sem ela, o BinaryWriter recebe o tipo errado e
    # grava um arquivo corrompido.
    return , [byte[]]$saida
}

$imagens = @()
foreach ($t in $tamanhos) {
    $bmp = New-IconeRelogio -Tam $t

    $dados = ConvertTo-Dib -Bmp $bmp
    $imagens += [PSCustomObject]@{ Tam = $t; Dados = $dados; Formato = "DIB" }
    $bmp.Dispose()
}

$saidaCompleta = [System.IO.Path]::GetFullPath($Saida)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($saidaCompleta)) | Out-Null

$fs = [System.IO.File]::Create($saidaCompleta)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR
$bw.Write([UInt16]0)                    # reservado
$bw.Write([UInt16]1)                    # tipo 1 = icone
$bw.Write([UInt16]$imagens.Count)

# Offset da primeira imagem, depois do diretorio inteiro
$offset = 6 + (16 * $imagens.Count)

foreach ($img in $imagens) {
    # 0 no campo de largura/altura significa 256 px
    $dim = if ($img.Tam -ge 256) { 0 } else { $img.Tam }
    $bw.Write([Byte]$dim)               # largura
    $bw.Write([Byte]$dim)               # altura
    $bw.Write([Byte]0)                  # cores na paleta (0 = sem paleta)
    $bw.Write([Byte]0)                  # reservado
    $bw.Write([UInt16]1)                # planos
    $bw.Write([UInt16]32)               # bits por pixel
    $bw.Write([UInt32]$img.Dados.Length)
    $bw.Write([UInt32]$offset)
    $offset += $img.Dados.Length
}

foreach ($img in $imagens) {
    $bw.Write([byte[]]$img.Dados, 0, $img.Dados.Length)
}

$bw.Close(); $fs.Close()

$info = Get-Item $saidaCompleta
Write-Host "Icone gerado: $($info.FullName)"
Write-Host "Tamanho: $([Math]::Round($info.Length/1KB,1)) KB  |  $($imagens.Count) resolucoes"
foreach ($img in $imagens) {
    Write-Host ("  {0,3} px  {1}  {2,8:N0} bytes" -f $img.Tam, $img.Formato, $img.Dados.Length)
}

# Validacao: se o GDI+ nao conseguir reabrir o arquivo, o icone esta malformado.
try {
    $teste = New-Object System.Drawing.Icon($saidaCompleta, (New-Object System.Drawing.Size(32, 32)))
    Write-Host "Validacao: o Windows conseguiu abrir o icone ($($teste.Width)x$($teste.Height))." -ForegroundColor Green
    $teste.Dispose()
}
catch {
    Write-Host "ERRO: o icone gerado nao pode ser lido: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

