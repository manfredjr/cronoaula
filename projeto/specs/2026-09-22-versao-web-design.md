# Versão web do CronoAula

Especificação aprovada em conversa com o Manfred em 22/09/2026. Serve de base para o plano de
implementação.

## 1. Objetivo

Oferecer o CronoAula também no navegador, em `cronoaula.escalada.dev/usar/`, para três
situações:

- **projetar o tempo para a turma**, em tela cheia, numa prova ou atividade longa;
- **usar onde não dá para instalar**, como computador de laboratório ou de instituição que
  bloqueia o `.exe` (o Smart App Control já barrou o programa uma vez);
- **usar no celular ou tablet**, como cronômetro independente na mão do professor.

O programa de Windows continua existindo e continua sendo o único caminho para ficar por cima
do PowerPoint.

## 2. O que fica de fora, e por quê

| Fora | Motivo |
|---|---|
| Ficar por cima de outros programas | Página web não consegue. A janela flutuante do Chrome e do Edge (Picture-in-Picture) foi oferecida e não entrou: nenhuma das três situações precisa dela, e ela não existe no Firefox nem no Safari |
| Atalhos com o foco em outro programa | Navegador só recebe teclas com a página em foco |
| Transparência, tamanhos, encaixe nos cantos | Existem para a janelinha sobre os slides, que a web não tem |
| Celular como controle remoto do projetor | Exige servidor para sincronizar aparelhos. Fica para uma segunda etapa, se houver |
| Editar os tempos rápidos e trocar teclas | Quase ninguém muda. Os tempos são fixos em 5, 10, 15, 30 e 50 |
| Qualquer biblioteca ou serviço externo | A página não faz requisição a terceiros. A promessa de "sem rede e sem telemetria" do programa vale também para a versão web |

## 3. Onde fica

**Tudo é arquivo estático**, servido pelo Apache do cPanel. Não roda nada no servidor: nem PHP,
nem banco, nem Node. O cronômetro inteiro roda no navegador de quem abre a página.

```
public/
  index.html               abertura do site, agora com os dois caminhos
  usar/
    index.html             a versão web
    usar.css
    js/
      motor.js             contagem
      tempo.js             leitura do tempo digitado e formatação
      alertas.js           faixas de alerta e seus nomes
      som.js               os dois sons, com agendamento
      ajustes.js           preferências guardadas no navegador
      tela.js              liga tudo à página
    sons/
      alerta.wav           copia identica de CronoAula/Assets/alerta.wav
      aviso.wav            copia identica de CronoAula/Assets/aviso.wav
web-testes/                testes da versão web, fora de public/
casos-compartilhados.json  casos lidos pelos testes em C# e em JavaScript
```

Os arquivos em `js/` são módulos ES carregados direto pelo navegador, sem etapa de build.

### Abertura do site

A abertura (`public/index.html`) passa a ter dois botões, com o **navegador em destaque**
(opção B da maquete):

- **Usar no navegador**, botão cheio, leva a `/usar/`;
- **Baixar para Windows**, botão vazado, baixa o `.exe` da release mais recente.

Embaixo, uma linha diz a diferença: no navegador dá para projetar e usar sem instalar; para
Windows, o programa fica por cima do PowerPoint.

## 4. Telas

Seguem a maquete aprovada. Identidade visual do `Marca.xaml`: fundo grafite `#1E2422` (página
`#151917`), dígitos em areia `#F5F7F2`, botão principal verde claro `#6AAF21` com texto verde
profundo `#022F10`, botões secundários `#333B37`, texto secundário `#98A199`, faixas de alerta
`#E09B2D`, `#E8752D` e `#E86A6F`.

**Tela normal.** Um cartão central com: dígitos grandes; a linha de estado embaixo; os tempos
rápidos; Iniciar/Pausar/Continuar, Zerar e +1 min; o campo de tempo com Carregar. No canto
superior direito, discretos, os botões **Tela cheia (F)** e **Ajustes**. Embaixo do cartão, os
atalhos de teclado em texto pequeno. No fim da página, o rodapé do ecossistema, igual ao da
abertura.

**Tela cheia.** Só os dígitos e a linha de estado, ocupando a tela. Os controles (Pausar, Zerar,
+1 min, Sair) aparecem numa barra no canto inferior direito ao mover o mouse ou tocar na tela,
e somem depois de 3 segundos sem movimento. Esc ou F saem.

**Celular.** A mesma página, ajustada à largura. Sem rolagem lateral em 375 px.

**Ajustes.** Um painel por cima da tela, com: tocar sinal ao terminar; volume; repetir N vezes a
cada S segundos; testar alerta e testar aviso; continuar contando em negativo; avisar faltando
N minutos. Botões Restaurar padrões e Pronto. Esc fecha.

## 5. Regras de comportamento

As regras são as do programa de Windows, para que as duas versões se comportem igual.

**Contagem.** O restante é sempre `duração - tempo decorrido`, com o tempo decorrido lido de um
relógio monotônico (`performance.now()`). Nunca se soma tick de temporizador.

**Carregar** define a duração e zera o decorrido, parado. Duração negativa vira zero.
**Iniciar** e **Continuar** retomam; chamar com o cronômetro já contando não faz nada.
**Pausar** congela o decorrido. **Zerar** volta à duração cheia, parado.
**+1 min** e **-1 min** mudam a duração sem perder o decorrido; a duração nunca fica negativa.
Como no programa, só o +1 min tem botão; o -1 min fica no teclado (seta para baixo).

**Contagem negativa.** Ligada por padrão: ao passar do zero, o mostrador segue em negativo
(`-01:20`). Desligada, o cronômetro para em `00:00`.

**Faixas de alerta**, em ordem:

| Condição | Faixa | Cor | Nome escrito |
|---|---|---|---|
| restante menor ou igual a zero | estourado | `#E86A6F`, piscando | tempo excedido |
| restante até 1 minuto | urgente | `#E8752D` | último minuto |
| restante até o menor entre 20% da duração e 2 minutos | atenção | `#E09B2D` | reta final |
| demais casos | normal | `#F5F7F2` | (nenhum) |

O nome escrito aparece sempre que a faixa não for normal, com o cronômetro contando ou parado.
O manual da marca proíbe indicar estado só por cor.

**Linha de estado**, em ordem de prioridade: `pausado`; `tempo excedido`; preset carregado
(`10 min carregado. Clique de novo para iniciar`, com `Toque` no lugar de `Clique` quando o
ponteiro principal for toque, detectado por `matchMedia('(pointer: coarse)')`); nome da faixa; vazio se estiver contando na faixa normal;
`pronto` se estiver parado.

**Tempos rápidos com dois cliques.** O primeiro clique num tempo o carrega e o destaca em verde
médio. O segundo clique no mesmo tempo, com o cronômetro parado, inicia. Clicar em outro tempo
carrega o outro. Iniciar dá partida a qualquer momento.

**Tempo digitado.** Aceita `MM:SS` (minutos podem passar de 59: `90:00` é 1h30), `HH:MM:SS`
(minutos e segundos até 59) e só minutos, com fração por ponto ou vírgula (`7,5`). Rejeita vazio,
texto, negativo, segundos acima de 59 e mais de três partes. Enter carrega. Entrada inválida
mostra uma mensagem na própria tela, sem janela de alerta.

**Formatação.** `MM:SS` abaixo de uma hora e `HH:MM:SS` a partir dela, arredondando para o
segundo mais próximo. Negativo leva `-` na frente.

**Piscar.** Na faixa estourada, os dígitos fazem um esmaecimento suave de 0,9 s, ida e volta.
Com `prefers-reduced-motion`, a cor fica fixa, sem piscar.

**Atalhos**, com a página em foco e fora do campo de tempo: Espaço inicia e pausa, R zera, seta
para cima soma 1 minuto, seta para baixo tira 1 minuto, F entra e sai da tela cheia, Esc sai da
tela cheia ou fecha os Ajustes.

**Tela acesa.** Enquanto conta, a página pede ao navegador para não apagar a tela (Wake Lock).
Solta ao pausar ou parar e pede de novo ao voltar a ficar visível. Onde não houver suporte,
segue sem.

**Tela cheia no iPhone.** O Safari do iPhone não permite tela cheia fora de vídeo. Nele, o botão
ativa um modo projeção dentro da página: esconde os controles e amplia os dígitos, com a mesma
barra que aparece ao tocar.

**Custo parado.** Sem contagem, a página não redesenha nada. Contando, redesenha só com a aba
visível, a cada quadro do navegador.

## 6. Peças e interfaces

Cada peça tem uma tarefa só. As quatro primeiras não conhecem a página e são testadas sozinhas.

**`motor.js`** exporta `criarMotor({ relogio, negativo, avisoEm })`. O `relogio` é uma função
que devolve milissegundos; na página é `performance.now`, nos testes é um relógio falso. O motor
oferece `carregar(ms)`, `iniciar()`, `pausar()`, `zerar()`, `somar(ms)`, `atualizar()`,
`restante()`, `duracao()`, `estado()` (`parado`, `contando`, `pausado`), `estourado()` e
`configurar({ negativo, avisoEm })`, para aplicar ajustes mudados com a página aberta.
`atualizar()` devolve os eventos ocorridos desde a chamada anterior: `aviso` uma vez ao cruzar o
limiar antecipado, `fim` uma vez ao cruzar o zero. Somar tempo que traga o restante de volta
para o positivo permite novo `fim`. Com a contagem negativa desligada, o `fim` para o motor e o
`restante()` nunca fica abaixo de zero.

**`tempo.js`** exporta `interpretar(texto)`, que devolve milissegundos ou `null`, e
`formatar(ms)`.

**`alertas.js`** exporta `faixa(restanteMs, duracaoMs)` e `nome(faixa)`.

**`som.js`** exporta `criarSom()`, com `liberar()` (chamado no primeiro clique, por causa da
política de áudio dos navegadores), `agendar({ fimEmMs, avisoEmMs, repeticoes, intervaloS,
volume, ligado })`, `cancelar()`, `silenciar()`, `testarAlerta()` e `testarAviso()`.

**`ajustes.js`** exporta `carregar(armazenamento)` e `salvar(armazenamento, ajustes)`. Recebe o
armazenamento de fora, para os testes usarem um falso.

**`tela.js`** é a única que toca na página: botões, teclado, tela cheia, modo projeção, Wake
Lock, painel de ajustes e o laço de desenho.

## 7. Som com a aba em segundo plano

Com a aba em segundo plano, o Chrome passa a rodar os temporizadores dela no máximo uma vez por
minuto depois de alguns minutos. O mostrador continua certo, porque sai do relógio, mas um som
disparado por temporizador atrasaria até um minuto. É o caso de quem usa a versão web no
computador enquanto passa slides.

Por isso o som não é disparado por temporizador: ele é **agendado** no relógio do áudio do
navegador (`AudioBufferSourceNode.start(quando)`), que não sofre esse atraso.

- Ao iniciar, continuar, somar ou tirar minuto, e ao mudar um ajuste com o cronômetro
  contando, cancela o agendado e agenda de novo: o aviso antecipado, se ainda não tocou e o
  limiar estiver no futuro, e as repetições do alerta de fim a partir do instante do zero.
- Ao pausar, zerar ou carregar, cancela tudo.
- O alerta de fim toca `alerta.wav` N vezes, a cada S segundos (padrão 5 vezes a cada 3 s; 0 quer
  dizer até alguém mexer, com teto de 60). O aviso toca `aviso.wav` uma vez, a 70% do volume.
- O volume vem de um nó de ganho, sem mexer nas amostras.
- **Silenciar:** qualquer clique na página ou tecla interrompe o alerta de fim **se ele já tiver
  começado**, cancelando as repetições restantes. Alerta ainda no futuro continua agendado.
- Se o `fim` já tocou e o professor continua contando no negativo, o alerta não é agendado de
  novo.

## 8. Ajustes

Guardados em `localStorage`, na chave `cronoaula.ajustes`, como JSON. Valor fora da faixa é
trazido para dentro dela; valor ausente ou ilegível volta ao padrão. Armazenamento bloqueado ou
corrompido não impede a página de funcionar: ela usa os padrões e segue.

| Chave | Padrão | Faixa |
|---|---|---|
| `som` | `true` | ligado ou desligado |
| `volume` | `0.7` | 0 a 1 |
| `repeticoes` | `5` | 0 a 60 (0 = até parar) |
| `intervalo` | `3` | 1 a 30 segundos |
| `avisoLigado` | `true` | ligado ou desligado |
| `avisoMinutos` | `5` | 1 a 120 |
| `negativo` | `true` | ligado ou desligado |
| `ultimoMinutos` | `50` | 0 a 1440; fora disso, 50 |

Ao abrir, a página carrega o último tempo usado.

## 9. Testes

**Automáticos, só na máquina de desenvolvimento**, com `node --test web-testes/`. O Node não vai
para o servidor.

| Peça | O que cobrir |
|---|---|
| `motor.js` | todos os casos do `TimerEngineTests` do programa, inclusive a simulação de 10 minutos com 6.000 atualizações e restante exatamente zero |
| `tempo.js` | leitura e formatação, pelos casos compartilhados |
| `alertas.js` | faixas, pelos casos compartilhados, e os nomes |
| `ajustes.js` | padrões, valores fora da faixa, JSON corrompido, armazenamento que lança erro |

**Casos compartilhados.** `casos-compartilhados.json`, na raiz, guarda os casos de leitura do tempo
digitado, de formatação e de faixas de alerta, cada um com entrada e resultado esperado. Um teste
novo em C# (`CasosCompartilhadosTests`) e os testes em JavaScript leem o mesmo arquivo. Se alguém
mudar a regra de um lado só, os testes do outro lado falham.

**`SiteTests` passa a conferir:** que `public/usar/index.html` existe; que todo arquivo que a
página carrega existe; que `sons/alerta.wav` e `sons/aviso.wav` são idênticos aos de
`CronoAula/Assets`; que nenhum arquivo de teste (`*.test.*`) está em `public/`; e que a abertura
tem o link para `/usar/`. A lista de extensões aceitas em `public/` ganha `.wav`.

**`build.ps1`** passa a rodar os testes da versão web depois dos do programa. Sem Node instalado,
ele para com a instrução de instalar, em vez de pular calado. O `-SkipTests` que já existe
continua pulando todos os testes, os dois conjuntos, para quem só precisa gerar o executável.
Como o `publicar.ps1` chama o `build.ps1`, a publicação do programa também passa a exigi-los.
O `CONTRIBUTING.md` passa a listar o Node entre o que é preciso para contribuir.

**Conferência no navegador** antes da entrega: computador, celular (375 px), tela cheia, modo
escuro e o painel de ajustes.

## 10. Publicação e documentação

Mesmo fluxo do site, descrito em `PUBLICAR-SITE.md`: Pull Request, merge com autorização do
Manfred, e no cPanel Update from Remote, F5 e Deploy HEAD Commit. O `.cpanel.yml` passa a
conferir também `public/usar/index.html`.

**Depois de cada deploy, purgar o cache da Cloudflare.** A página HTML chega sem cache, mas os
arquivos `.js`, `.css` e `.wav` podem ficar guardados na borda, e o navegador receberia a versão
antiga do motor com a página nova.

`PUBLICAR-SITE.md` e `README.md` ganham a seção da versão web, e o `CONTRIBUTING.md` passa a
listar o Node.

## 11. Critérios de aceite

- `cronoaula.escalada.dev/usar/` abre e conta, em Chrome, Edge, Firefox e no celular.
- A abertura do site mostra a opção B: navegador em destaque, Windows vazado.
- As faixas, os nomes e a contagem negativa se comportam como no programa, e os casos
  compartilhados passam nos dois lados.
- Com a aba em segundo plano por mais de 5 minutos, o alerta de fim toca no instante do zero.
- Um clique silencia o alerta que está tocando, sem cancelar um alerta futuro.
- A tela não apaga enquanto conta, onde houver suporte.
- Ajustes sobrevivem a fechar e abrir a página, e a página funciona com o armazenamento
  bloqueado.
- A página não faz nenhuma requisição fora de `cronoaula.escalada.dev`.
- `dotnet test` e `node --test web-testes/` passam; o `build.ps1` roda os dois.

## 12. Riscos conhecidos

- **Safari e o áudio em segundo plano.** O Safari pode suspender o áudio da página quando ela sai
  de vista, principalmente no iPhone com a tela bloqueada. A tela acesa reduz o caso no celular,
  mas não há garantia no Safari. Fica registrado no README.
- **Wake Lock** não existe em navegadores antigos. Nesses, a tela pode apagar pelo tempo do
  próprio aparelho.
- **Cache da Cloudflare** servindo `.js` antigo depois de um deploy. Coberto pelo purge da seção
  10.
- **Duas implementações das regras**, em C# e em JavaScript. Coberto pelos casos compartilhados,
  que falham se uma delas mudar sozinha.
- **Medição injetada pela Cloudflare.** O painel da Cloudflare tem uma opção que injeta um script
  de medição em toda página, e foi o que aconteceu no ChamaAula. Conferido em 22/09/2026: hoje o
  site do CronoAula não tem script injetado nem recurso externo. Se a opção for ligada na zona
  `escalada.dev`, a promessa de "sem requisição a terceiros" quebra sem nenhuma mudança no
  código. A conferência do critério de aceite pega isso.
