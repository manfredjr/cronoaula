# CronoAula

Cronômetro de contagem regressiva para aulas. Fica em um canto da tela, sempre
visível por cima do PowerPoint, do navegador e do Dev-C++, sem atrapalhar o
conteúdo projetado.

Feito para o professor controlar o tempo de cada etapa da aula: explicação,
exercício, prova, intervalo.

Software livre, sob licença [GPL-3.0](LICENSE). Gratuito para qualquer
professor usar, e aberto para quem quiser melhorar.

### **[Baixar o CronoAula.exe](https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe)**

Baixa apenas o programa, um arquivo só. Você não precisa do código-fonte para
usá-lo. Se quiser ver todas as versões, use a página de
[Releases](https://github.com/manfredjr/cronoaula/releases).

---

## Por que ele existe

Nas aulas de Fundamentos de Programação, o tempo é parte da estratégia
pedagógica.

A disciplina começa pelo desenvolvimento do raciocínio lógico, pela organização
de etapas e pela construção de soluções claras para cada problema. Ao longo do
semestre, os alunos avançam por conteúdos como entrada e saída de dados,
estruturas condicionais, estruturas de repetição, vetores, matrizes e funções.
Cada conteúdo novo depende da compreensão do anterior.

Na Metodologia em Escalada, a aula é organizada em ciclos: explicação, atividade
prática, tempo de desenvolvimento, correção comentada e consolidação do
conteúdo. O professor apresenta um problema, define um período para que os
alunos desenvolvam a solução e, ao final, faz a correção com a turma. Isso
estimula o raciocínio individual, a aplicação prática dos conceitos e a
identificação das dificuldades antes de avançar.

Por isso o controle do tempo de cada atividade é essencial. O cronômetro ajuda a:

- estabelecer objetivos claros para cada etapa;
- manter o ritmo e a organização da aula;
- equilibrar explicação, prática e correção;
- informar aos alunos quanto tempo ainda têm;
- evitar que uma atividade comprometa as etapas seguintes;
- criar períodos de concentração e desenvolvimento autônomo;
- conduzir exercícios, avaliações, apresentações e intervalos.

No início esse controle era feito com o cronômetro do Google. Ele cumpria a
função, mas exigia manter o navegador aberto, alternar entre janelas e muitas
vezes interromper a apresentação ou o ambiente de programação usado na aula.

O CronoAula foi desenvolvido para resolver esse problema. É um cronômetro
regressivo que permanece visível durante toda a atividade, sem interromper a
explicação, os slides, o desenvolvimento dos algoritmos ou a correção dos
exercícios.

### Principais recursos

- Permanece sobre qualquer programa, inclusive apresentações e ambientes de
  programação.
- Tem transparência ajustável, para acompanhar o tempo sem esconder o conteúdo.
- Pode ser redimensionado e posicionado em qualquer canto da tela.
- Exibe o tempo em tela cheia quando ele precisa ser projetado para toda a
  turma.
- É controlado por atalhos de teclado, sem tirar o foco da apresentação ou do
  código.
- Funciona sem internet, sem telemetria e sem permissões administrativas.

O CronoAula organiza o ritmo da aprendizagem. Enquanto o professor conduz a
aula, os alunos veem o tempo disponível e sabem quando analisar, desenvolver,
testar e concluir cada atividade.

CronoAula - o tempo como parte da aprendizagem.

O mesmo texto está dentro do programa, em **botão direito**, na opção
**Sobre o CronoAula**.

---

## Instalação

**Não existe instalador, e você não precisa de um.** Nem do .NET, nem de
bibliotecas, nem de privilégio de administrador.

1. Baixe o
   **[CronoAula.exe](https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe)**.
2. Copie o arquivo para qualquer pasta do seu computador (por exemplo, a Área
   de Trabalho).
3. Dê um duplo clique nele.

O link acima entrega sempre a versão mais recente. Não é preciso baixar o
código-fonte: ele interessa a quem quiser estudar ou modificar o programa, não
a quem só quer usá-lo em aula.

Só isso. É um arquivo único e autossuficiente: o runtime do .NET, a interface
gráfica, o ícone e os dois sons estão todos embutidos dentro dele.

**Requisitos:** Windows 10 ou 11, 64 bits.

### Duas coisas que podem acontecer na primeira vez

**A primeira execução demora cerca de 10 segundos.** É normal e acontece uma
única vez por versão: o Windows precisa descompactar o conteúdo do executável em
uma pasta temporária. Da segunda vez em diante, o aplicativo abre em menos de
1 segundo.

**O Windows pode barrar o programa.** Como o executável não é assinado
digitalmente, você pode ver "O Windows protegeu o seu computador" (SmartScreen).
Clique em **Mais informações** e depois em **Executar assim mesmo**.

Se o computador tiver o **Controle de Aplicativo Inteligente** (Smart App
Control) ligado, o bloqueio é mais severo e não oferece a opção de executar
assim mesmo. Veja [Distribuição](#distribuição-preciso-de-um-instalador) para
saber como verificar isso e o que fazer.

---

## Como usar

### Iniciar uma contagem

Os botões de tempo rápido (5, 10, 15, 30 e 50 minutos) funcionam em **dois
cliques**:

| Ação | O que acontece |
|---|---|
| 1º clique no botão | Carrega o tempo. O botão fica azul e aparece "carregado" |
| 2º clique no mesmo botão | Começa a contagem |

**Por que dois cliques?** Para não iniciar uma contagem por engano no meio da
aula. Um clique só carrega e mostra o tempo; nada começa a correr até você
confirmar. Se preferir, o botão **Iniciar** também dá partida a qualquer momento,
sem precisar do segundo clique.

Clicar em um botão diferente apenas troca o tempo carregado, sem iniciar nada.

### Tempo personalizado

Digite no campo de baixo e pressione **Enter** (ou clique em **Carregar**):

| Você digita | Significa |
|---|---|
| `25` | 25 minutos |
| `25:30` | 25 minutos e 30 segundos |
| `01:05:00` | 1 hora e 5 minutos |
| `7,5` | 7 minutos e 30 segundos |

### Controles

- **Iniciar / Pausar / Continuar**: o botão azul muda de nome conforme o estado.
- **Zerar**: volta ao tempo cheio, sem começar a contar.
- **+1 min**: estende a atividade em andamento sem perder a contagem. Útil
  quando a turma precisa de um pouco mais de tempo.

### Quando o tempo acaba

O cronômetro **não para no zero**. Ele passa a contar quanto a atividade
estourou, mostrando `-01:20` em vermelho. Assim você sabe exatamente quantos
minutos precisa recuperar no restante da aula.

Se preferir que ele pare em `00:00`, desligue em **Preferências**, seção
**Contagem**, na opção **Continuar contando em negativo**.

---

## Avisos visuais

As cores mudam sozinhas, sem depender do som. Isso importa numa sala barulhenta
ou quando você prefere não incomodar a turma:

| Cor | Texto na tela | Quando |
|---|---|---|
| Areia | (nenhum) | Tempo normal |
| Âmbar | reta final | Últimos 20% do tempo, ou 2 minutos, o que for menor |
| Laranja | último minuto | Último minuto |
| Vermelho piscando | tempo excedido | Zero e tempo excedido |

Cada faixa aparece com o nome escrito, não apenas pela cor. O manual da marca
exige isso, e há um motivo prático: da última fileira da sala, a diferença entre
âmbar e laranja não é confiável, e parte das pessoas não distingue bem esses
tons.

O "20% ou 2 minutos, o que for menor" evita que uma aula de 50 minutos comece a
alertar faltando 10 minutos. Na prática: atividades curtas avisam
proporcionalmente, atividades longas avisam sempre nos últimos 2 minutos.

O vermelho pisca com um esmaecimento suave, não com um pisca-pisca agressivo,
para não competir com a atenção da turma.

---

## Sons

São **dois sons diferentes**, de propósito: você precisa distinguir "está
acabando" de "acabou" sem olhar para a tela.

| Momento | Som | Comportamento |
|---|---|---|
| Aviso antecipado | Toque único e discreto | Toca **uma vez** |
| Fim do tempo | Padrão de duas notas alternadas | **Repete** 5 vezes, a cada 3 s |

O alerta de fim repete porque o professor raramente está olhando para o
computador quando o tempo acaba. Costuma estar atendendo um aluno ou no outro
lado da sala. Um bipe único passa despercebido.

### Como silenciar

O alerta para sozinho depois das repetições configuradas. Mas qualquer uma
destas ações o interrompe na hora:

- clicar em qualquer ponto da janela;
- usar qualquer botão (Iniciar, Pausar, Zerar, +1 min, um tempo rápido);
- acionar qualquer atalho global;
- botão direito e **Silenciar alerta**.

A lógica é simples: se você encostou no cronômetro, é porque já percebeu.

### Ajustes

Em **Preferências**, seção **Som**, você define quantas vezes repete e de quantos em
quantos segundos. Colocando **0 repetições**, o alerta toca até você interromper
(com um teto de segurança de 60 toques, para o caso de a sala ficar vazia).

Também dá para desligar o som por completo ou baixar o volume, e há botões para
testar cada um dos dois sons antes de salvar.

---

## Teclas de atalho

Funcionam **mesmo com o foco em outro programa**. Você pode estar passando
slides no PowerPoint ou escrevendo código no Dev-C++ e os atalhos continuam
respondendo.

| Atalho | Ação |
|---|---|
| `Ctrl + Alt + S` | Iniciar / Pausar |
| `Ctrl + Alt + R` | Zerar |
| `Ctrl + Alt` + seta para cima | Somar 1 minuto |
| `Ctrl + Alt` + seta para baixo | Subtrair 1 minuto |
| `Ctrl + Alt + H` | Mostrar / Esconder a janela |
| `Ctrl + Alt + F` | Entrar / sair da tela cheia |

Todos podem ser trocados em **Preferências**, seção **Atalhos globais**. Formato aceito:
um ou mais modificadores (`Ctrl`, `Alt`, `Shift`, `Win`) mais uma tecla,
por exemplo `Ctrl+Shift+F9` ou `Ctrl+Alt+Up`. Deixe o campo em branco para
desativar um atalho.

Se outro programa já estiver usando uma combinação, o CronoAula avisa na hora,
dizendo qual atalho falhou e por quê. Os demais continuam funcionando
normalmente.

---

## A janela

- **Sempre por cima.** Continua visível inclusive sobre apresentações em tela
  cheia do PowerPoint. Pode ser desligado no menu do botão direito.
- **Arrastável.** Segure o botão esquerdo em qualquer ponto vazio da janela e
  arraste. Sobre os botões, o clique aciona o botão em vez de arrastar.
- **Transparência.** Segure `Ctrl` e gire a roda do mouse sobre a janela, entre
  30% e 100%. Quando o mouse passa por cima, ela volta a 100% para você
  conseguir clicar com facilidade.
- **Três tamanhos.** Pequeno, médio e grande, conforme a distância até o
  monitor. No menu do botão direito. Os tamanhos afetam **apenas os dígitos**:
  os botões mantêm sempre as mesmas medidas, porque um alvo de clique não
  precisa crescer junto com o relógio.
- **Encaixe nos cantos.** Menu do botão direito, em **Mover para**, com margem de
  20 px da borda.

Clique com o **botão direito** em qualquer ponto da janela para abrir o menu com
tamanhos, opacidade, "mover para", tela cheia, sempre por cima, preferências e
sair.

---

## Modo tela cheia

Pensado para provas e atividades longas, quando o cronômetro é projetado para a
turma inteira em vez de ficar no canto da sua tela.

**Para entrar ou sair**, use o que for mais prático no momento:

| Como | Observação |
|---|---|
| `F11` | Com a janela em foco |
| `Ctrl + Alt + F` | Atalho global: funciona com o foco no PowerPoint |
| Duplo clique no relógio | Em qualquer área que não seja botão |
| Botão direito, em Tela cheia | Permite escolher o monitor |
| `Esc` | Só para sair |

**Como fica:** fundo preto, sem transparência, e os dígitos ocupando a tela
inteira. Em uma tela de 1920x1080, um `02:00:00` fica com cerca de 25 cm de
altura, legível do fundo da sala.

Os controles ficam escondidos. Basta mexer o mouse para eles aparecerem, e
somem sozinhos após 3 segundos. Durante a prova, a turma vê apenas o relógio.

As cores de alerta continuam valendo: amarelo, laranja e o vermelho piscando ao
estourar o tempo.

### Com projetor conectado

Quando há mais de um monitor, o menu do botão direito vira **"Tela cheia em"** e
lista as telas disponíveis. A escolha fica salva para as próximas vezes.

Se o projetor for desconectado depois, o programa não fica preso a uma tela que
não existe mais: ele volta a usar o monitor onde a janela estiver.

**Controle sem tocar no mouse.** Com o cronômetro projetado, você provavelmente
está usando o computador para outra coisa. Os atalhos globais continuam
funcionando: `Ctrl+Alt+S` pausa e `Ctrl+Alt` com a seta para cima acrescenta um minuto quando a
turma pede mais tempo.

Ao sair, a janela volta exatamente à posição, tamanho e opacidade que tinha
antes. A tela cheia não altera nada das suas preferências salvas.

---

## Preferências

Botão direito, em **Preferências**:

- Som: ligar/desligar, volume, quantas vezes o alerta de fim repete e o
  intervalo entre as repetições, com botões para testar cada som.
- Contagem em negativo: ligar/desligar.
- Aviso antecipado: quantos minutos antes do fim avisar.
- Tempos rápidos: quais botões aparecem (até 6).
- Atalhos globais: remapear cada um.

Tudo é salvo automaticamente em:

```
%APPDATA%\CronoAula\config.json
```

Posição da janela, tamanho, opacidade e último tempo usado também são lembrados
entre execuções. Feche e reabra que ele volta exatamente como estava.

Se esse arquivo for apagado ou corrompido, o aplicativo abre normalmente com os
valores padrão, sem erro.

---

## Distribuição: preciso de um instalador?

**Não.** Basta copiar o `CronoAula.exe` e executar. Isso foi verificado, não
apenas presumido:

- O executável é **autocontido**: com ele rodando, nenhum módulo é carregado de
  `C:\Program Files\dotnet`. Ele usa apenas o runtime que carrega dentro de si.
- A pasta de saída contém **um único arquivo**. Não há DLLs ao lado.
- Não pede privilégio de administrador (declarado como `asInvoker` no
  `app.manifest`).
- O único arquivo que ele escreve é o `config.json` com as suas preferências.

Um instalador só faria sentido se você quisesse atalho no Menu Iniciar, início
automático com o Windows ou desinstalação pelo Painel de Controle. Nada disso é
necessário para o programa funcionar.

### Se você baixar o programa de um site

Baixar pela internet é diferente de copiar por pen drive. O navegador marca o
arquivo com a **Marca da Web** (*Mark of the Web*), um pequeno registro
invisível colado nele:

```
[ZoneTransfer]
ZoneId=3
HostUrl=https://seusite.com.br/CronoAula.exe
```

`ZoneId=3` quer dizer "veio da internet". É essa marca que faz o Windows exibir
**"O Windows protegeu o seu computador"** ao executar, mesmo que o arquivo
esteja perfeito.

**Como resolver na máquina que recebeu o arquivo**, escolha um caminho:

1. Na tela de aviso, clique em **Mais informações** e depois em **Executar assim mesmo**.
2. Clique com o botão direito no arquivo, abra **Propriedades**, marque
   **Desbloquear** e confirme em **OK**.
3. Pelo PowerShell:

```bash
Unblock-File .\CronoAula.exe
```

**Como evitar o aviso desde o começo:** copie o executável por pen drive, pasta
de rede ou compartilhamento interno da instituição. Esses caminhos não aplicam a
Marca da Web, e o programa abre direto.

### Confira se o arquivo chegou íntegro

Cada versão publicada acompanha um arquivo `CronoAula.exe.sha256.txt`. Para
conferir na máquina de destino:

```bash
Get-FileHash .\CronoAula.exe -Algorithm SHA256
```

Se o valor bater com o do arquivo `.txt`, o executável é exatamente o que foi
publicado. Não elimina o aviso do SmartScreen, mas prova que ninguém alterou o
arquivo no caminho.

### O obstáculo real não é o .NET, é a assinatura digital

O que pode impedir o CronoAula de rodar no computador do professor não é falta
de dependência. É o Windows desconfiar de um programa sem assinatura digital.

| Proteção | O que acontece | Tem saída? |
|---|---|---|
| SmartScreen | "O Windows protegeu o seu computador" | Sim, em *Mais informações* e *Executar assim mesmo* |
| Smart App Control | Bloqueia e **não** oferece alternativa | Não, sem assinar o programa |

Para saber se a máquina de destino tem o Smart App Control ligado, abra
**Segurança do Windows**, em **Controle de aplicativos e navegador**, na opção **Controle de
Aplicativo Inteligente**. Se estiver como "Ativado", um executável sem
assinatura será bloqueado, inclusive este.

Vale saber: o Smart App Control **só pode ser desligado**, nunca religado sem
reinstalar o Windows. Portanto não é algo a mexer por causa de um cronômetro.

Se você precisar rodar em máquinas com essa proteção ativa, a solução é assinar
o executável com um certificado de assinatura de código (Code Signing), que é
pago e emitido por uma autoridade certificadora. A alternativa gratuita seria
rodar o projeto pelo código-fonte com o SDK instalado.

### Assinatura digital: o que ela resolve

Um certificado de assinatura de código passa a identificar você como autor do
programa. Isso muda o comportamento do Windows em três frentes:

| Sem assinatura | Com assinatura |
|---|---|
| "Editor desconhecido" no aviso | Aparece o seu nome como editor |
| SmartScreen avisa em todo download | Some depois que a reputação é construída (imediato com certificado EV) |
| Smart App Control bloqueia | Passa a permitir |
| Antivírus dão mais falso positivo | Bem menos falso positivo |

Dois pontos práticos antes de investir:

- **É pago e anual.** Desde 2023 todos os certificados de assinatura de código
  exigem armazenamento em hardware (token físico) ou HSM na nuvem, o que
  encareceu e burocratizou o processo. Confirme os valores atuais direto com a
  autoridade certificadora.
- **Certificado comum não resolve na hora.** Com um certificado OV, o
  SmartScreen ainda pede um tempo de circulação até confiar no programa. Só o
  tipo EV (validação estendida) dá reputação imediata, e custa mais.

Para uso interno em uma instituição, costuma ser mais simples distribuir por
pasta de rede ou pen drive, ou pedir ao setor de TI que inclua o programa na
lista de permissões, que é a solução formal em parque gerenciado.

> Uma armadilha encontrada durante o desenvolvimento: o Smart App Control chegou
> a bloquear o programa por causa do **formato interno do ícone**. Um `.ico` com
> imagem comprimida em PNG fazia o Windows recusar o binário
> (`0x800711C7`). Gerando o ícone no formato DIB clássico, o bloqueio
> desapareceu. Por isso o gerador de ícone usa DIB em todas as resoluções.

---

## Compilar a partir do código

Só é necessário se você quiser modificar o programa. Para apenas usá-lo, o
`.exe` basta.

**Pré-requisito:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Gerar o executável único:

```bash
.\build.ps1
```

O script roda os testes antes de publicar e recusa gerar o executável se algum
falhar. O resultado sai em `.\dist\CronoAula.exe`.

Para pular os testes:

```bash
.\build.ps1 -SkipTests
```

O comando de publicação equivalente, sem o script:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Durante o desenvolvimento:

```bash
dotnet build
```

```bash
dotnet test
```

---

## Estrutura do projeto

```
CRONOAULA/
  CronoAula.sln
  build.ps1                      gera o executável único em .\dist
  tools/
    gerar-icone.ps1              redesenha Assets\cronoaula.ico
    gerar-sons.ps1               regera aviso.wav e alerta.wav
  CronoAula/
    CronoAula.csproj
    app.manifest                 sem privilégio de admin, DPI por monitor
    App.xaml / App.xaml.cs
    MainWindow.xaml / .cs        janela, arrastar, transparência, Win32
    PreferencesWindow.xaml / .cs
    AboutWindow.xaml / .cs       janela "Sobre" e origem do projeto
    Core/
      TimerEngine.cs             lógica do cronômetro (sem WPF, testável)
      IClock.cs                  abstração do relógio
      TimeParser.cs              interpreta e formata tempos
      AppSettings.cs             config.json
      GlobalHotkeyManager.cs     RegisterHotKey / user32.dll
      MonitorHelper.cs           lista os monitores (tela cheia)
      SoundService.cs            sons embutidos, volume e repetição
    ViewModels/
      MainViewModel.cs           liga o TimerEngine à interface
    Assets/
      cronoaula.ico              ícone (9 resoluções, formato DIB)
      alerta.wav                 fim do tempo (embutido)
      aviso.wav                  aviso antecipado (embutido)
  CronoAula.Tests/               107 testes
```

Os arquivos em `Assets/` já vêm prontos no repositório. Os scripts em `tools/`
só precisam ser executados se você quiser mudar o desenho do ícone ou o timbre
dos sons.

### Decisões técnicas

**Precisão.** O tempo restante é sempre calculado como
`duração - tempo decorrido`, lido de um `Stopwatch` (contador de alta precisão
do sistema). Nunca somando ticks de timer, que acumulariam erro em contagens
longas. Um teste simula 10 minutos com 6.000 atualizações e exige desvio zero.

**Ficar por cima de apresentações.** `Topmost = true` sozinho não vence o modo
de exibição do PowerPoint, que também se declara topmost e passa à frente ao
entrar em tela cheia. O CronoAula reafirma sua posição a cada 2 segundos com
`SetWindowPos`, usando a flag `SWP_NOACTIVATE` para não roubar o foco. Sem ela,
a passagem de slides pelo teclado travaria.

**Som no arquivo único.** Os dois `.wav` são embutidos como recurso do assembly
e lidos da memória, não do disco. Um arquivo solto ao lado do `.exe` quebraria a
premissa de arquivo único. Como o `System.Media.SoundPlayer` não tem controle de
volume, o volume é aplicado escalando as amostras PCM em memória antes de tocar,
sem depender de nenhum pacote externo.

**Ícone em DIB, não em PNG.** Um `.ico` pode guardar cada resolução como PNG
comprimido ou como DIB clássico. O PNG é bem menor, mas com ele o Smart App
Control do Windows 11 passou a bloquear o binário. Todas as resoluções usam DIB;
o ícone fica com 372 KB em vez de 8 KB, o que é irrelevante perto dos 63 MB do
executável.

**Identidade visual.** As cores seguem o manual da MT - Manfred Tecnologia e
ficam todas em [Marca.xaml](CronoAula/Marca.xaml), um único lugar. O fundo é o
verde profundo `#022F10`, os dígitos em areia `#F5F7F2` e o botão principal em
verde claro `#6AAF21` com texto verde profundo, par que o manual aprova em 5,48
de contraste. Texto branco sobre o verde claro daria 2,70 e é proibido.

Uma adaptação foi necessária e está documentada no próprio dicionário: o âmbar
e o vinho do manual são definidos para fundo claro e rendem 2,73 e 1,87 sobre o
verde profundo, abaixo do mínimo de 3,0 da própria tabela. A escala de alerta
usa versões clareadas dos mesmos tons, todas medidas acima de 4,5.

Os testes recalculam esses contrastes a partir das cores que estão de fato no
dicionário, então uma troca que quebre a legibilidade falha antes de chegar à
sala de aula.

**Consumo de CPU.** O relógio da interface só roda enquanto há contagem. Parado,
o consumo medido foi de 15 ms em 5 segundos, cerca de 0,3% de um núcleo.

**Tela cheia em pixels físicos.** A janela é posicionada com `SetWindowPos`
usando as coordenadas que a própria API de monitores devolve, sem passar pela
conversão de unidades do WPF. É o caminho que não erra quando o notebook e o
projetor têm escalas de DPI diferentes, situação comum em sala de aula. Os
dígitos são dimensionados por um `Viewbox`, que estica o texto até preencher a
tela sem nenhum cálculo de fonte.

**ReadyToRun foi testado e descartado.** Pré-compilar para nativo deixou a
partida mais lenta (1133 ms contra 905 ms): com a compressão do arquivo único
ligada, o executável maior custa mais para descompactar do que se ganha evitando
a compilação em tempo de execução.

---

## Privacidade

Sem telemetria, sem acesso à rede, sem necessidade de administrador. O único
arquivo que o programa escreve é o `config.json` com as suas preferências.

---

## Contribuindo

Se você é professor e usa o CronoAula, sua experiência de sala de aula vale
tanto quanto código. Relatar o que atrapalha já é contribuir.

- Problemas e sugestões: [Issues](../../issues)
- Quer mexer no código: leia o [CONTRIBUTING.md](CONTRIBUTING.md)

---

## Licença

Distribuído sob a **[GNU General Public License v3.0](LICENSE)**.

Você pode usar, estudar, modificar e redistribuir o programa livremente. Se
distribuir uma versão modificada, precisa também disponibilizar o código-fonte
dela sob a mesma licença - assim as melhorias continuam chegando a todos os
professores.

O programa é fornecido sem garantia de qualquer tipo, conforme os termos da
licença.

---

## Créditos

© 2026 MT - Manfred Tecnologia LTDA.

Criado a partir de uma necessidade real de sala de aula e disponibilizado para
outros professores que enfrentam o mesmo problema.

---

## Ainda não implementado

Deixado preparado para receber depois, mas fora do escopo desta versão:
sincronização com calendário, relatórios de tempo por aula, sequência automática
de etapas e ícone na bandeja do sistema.

Também não implementado, e a única coisa que resolveria o bloqueio em máquinas
com Smart App Control: assinatura digital do executável, que depende de um
certificado pago.
