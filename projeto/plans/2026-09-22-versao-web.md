# Versão web do CronoAula: plano de implementação

> **Para quem executa:** use superpowers:subagent-driven-development (recomendado) ou
> superpowers:executing-plans para seguir este plano tarefa por tarefa. Os passos usam caixas
> (`- [ ]`) para acompanhamento.

**Objetivo:** publicar o CronoAula também como página estática em
`cronoaula.escalada.dev/usar/`, com as mesmas regras do programa de Windows, e trocar a abertura
do site para oferecer "Usar no navegador" (em destaque) e "Baixar para Windows".

**Arquitetura:** módulos ES em JavaScript puro, carregados direto pelo navegador, sem build e
sem biblioteca. Quatro módulos sem DOM (`tempo`, `alertas`, `motor`, `ajustes`) e um de som com
o contexto de áudio injetado são testados com `node --test`. Um módulo (`tela.js`) liga tudo à
página. Um arquivo de casos compartilhados, na raiz, é lido pelos testes em C# e em JavaScript
para as duas implementações não divergirem.

**Tecnologias:** HTML, CSS, JavaScript (módulos ES), Web Audio API, Fullscreen API, Screen Wake
Lock API, `<dialog>`, `localStorage`. Testes: `node:test` (Node 24 na máquina de
desenvolvimento), xUnit (.NET 8) para `SiteTests` e `CasosCompartilhadosTests`.

**Especificação:** [projeto/specs/2026-09-22-versao-web-design.md](../specs/2026-09-22-versao-web-design.md).

## Restrições gerais

- Servidor só serve estático (Apache do cPanel): nada de PHP, banco ou Node em `public/`.
- A página não faz requisição a terceiros: sem CDN, sem fonte externa, sem script de medição.
- Tudo em pt-BR. Texto da interface sem jargão técnico.
- Cores do `Marca.xaml`: página `#151917`, cartão `#1E2422`, dígitos `#F5F7F2`, principal `#6AAF21` com texto `#022F10`, secundários `#333B37`, texto secundário `#98A199`, armado `#398024`, faixas `#E09B2D` (reta final), `#E8752D` (último minuto), `#E86A6F` (tempo excedido).
- Estado nunca só por cor: toda faixa não normal tem o nome escrito.
- Tempos rápidos fixos: 5, 10, 15, 30 e 50 minutos.
- Chave do armazenamento: `cronoaula.ajustes`.
- Comando dos testes web: `node --test "web-testes/*.test.js"` (com aspas; o Node 24 não aceita a pasta sozinha, conferido em 22/09/2026). A tarefa 8 corrige a especificação, que cita `node --test web-testes/`.
- Nenhum arquivo de teste dentro de `public/`.
- **Commits sem nenhuma linha de coautoria ou crédito a ferramenta de IA** (regra do Manfred, sem exceção): nada de `Co-Authored-By`, nada de "Generated with".
- Trabalho no branch `versao-web`. Merge em `main` e deploy no cPanel só com autorização explícita do Manfred no chat.
- No Windows, rodar os comandos pelo Git Bash (ferramenta Bash) a partir de `C:\COWORK\CODE\CRONOAULA`.

## Estrutura de arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `casos-compartilhados.json` | criar | casos de leitura, formatação e faixas, lidos pelos dois lados |
| `public/usar/js/tempo.js` | criar | `interpretar(texto)` e `formatar(ms)` |
| `public/usar/js/alertas.js` | criar | `faixa`, `nome` e `estadoEscrito` (linha de estado) |
| `public/usar/js/motor.js` | criar | contagem por relógio monotônico, eventos `aviso` e `fim` |
| `public/usar/js/ajustes.js` | criar | padrões, normalização, leitura e gravação no armazenamento |
| `public/usar/js/som.js` | criar | `planejar` (puro) e `criarSom` (Web Audio, contexto injetável) |
| `public/usar/js/tela.js` | criar | DOM, teclado, tela cheia, modo projeção, Wake Lock, ajustes, laço de desenho |
| `public/usar/index.html` | criar | página da versão web |
| `public/usar/usar.css` | criar | visual da página, tela cheia, painel e rodapé |
| `public/usar/sons/alerta.wav`, `aviso.wav` | copiar | cópias idênticas de `CronoAula/Assets/` |
| `web-testes/tempo.test.js`, `alertas.test.js`, `motor.test.js`, `ajustes.test.js`, `som.test.js` | criar | testes da versão web |
| `CronoAula.Tests/CasosCompartilhadosTests.cs` | criar | os mesmos casos contra `TimeParser` e `MainViewModel.ComputeAlert` |
| `CronoAula.Tests/SiteTests.cs` | alterar | `.wav` aceito, página `usar/`, referências, sons, sem terceiros, rodapé, abertura, `.cpanel.yml` |
| `public/index.html` | alterar | botões da opção B e a linha que explica a diferença |
| `.cpanel.yml` | alterar | `test -f` da página `usar/` |
| `build.ps1` | alterar | roda os testes web depois dos do programa |
| `CONTRIBUTING.md`, `README.md`, `PUBLICAR-SITE.md` | alterar | Node, seção da versão web, purge da Cloudflare |
| `projeto/specs/2026-09-22-versao-web-design.md` | alterar | comando correto do `node --test` |

---

### Tarefa 1: casos compartilhados e `tempo.js`

**Arquivos:**
- Criar: `casos-compartilhados.json`
- Criar: `public/usar/js/tempo.js`
- Criar: `web-testes/tempo.test.js`
- Criar: `CronoAula.Tests/CasosCompartilhadosTests.cs`

**Interfaces:**
- Consome: `TimeParser.TryParse(string?, out TimeSpan)` e `TimeParser.Format(TimeSpan)` do programa (já existem em `CronoAula/Core/TimeParser.cs`).
- Produz: `interpretar(texto: string) => number | null` (milissegundos inteiros) e `formatar(ms: number) => string`. O JSON tem as chaves `interpretar` (`{ texto, ms }`, `ms` nulo quando rejeita) e `formatar` (`{ ms, texto }`); a tarefa 2 acrescenta `faixa`.

- [ ] **Passo 1: criar os casos compartilhados**

`casos-compartilhados.json`:

```json
{
  "_leia": "Casos lidos pelos testes em C# (CasosCompartilhadosTests) e em JavaScript (web-testes). Mudou uma regra? Mude aqui e nos dois lados.",
  "interpretar": [
    { "texto": "25:30", "ms": 1530000 },
    { "texto": "05:00", "ms": 300000 },
    { "texto": "0:45", "ms": 45000 },
    { "texto": "90:00", "ms": 5400000 },
    { "texto": "10: 30", "ms": 630000 },
    { "texto": "01:05:00", "ms": 3900000 },
    { "texto": "02:00:30", "ms": 7230000 },
    { "texto": "25", "ms": 1500000 },
    { "texto": "5", "ms": 300000 },
    { "texto": " 10 ", "ms": 600000 },
    { "texto": "25.5", "ms": 1530000 },
    { "texto": "25,5", "ms": 1530000 },
    { "texto": "7,5", "ms": 450000 },
    { "texto": "", "ms": null },
    { "texto": "   ", "ms": null },
    { "texto": "abc", "ms": null },
    { "texto": "5min", "ms": null },
    { "texto": "10:99", "ms": null },
    { "texto": "1:60:00", "ms": null },
    { "texto": "1:2:3:4", "ms": null },
    { "texto": "-5", "ms": null },
    { "texto": "-5:00", "ms": null },
    { "texto": "1:", "ms": null },
    { "texto": ":30", "ms": null }
  ],
  "formatar": [
    { "ms": 0, "texto": "00:00" },
    { "ms": 300000, "texto": "05:00" },
    { "ms": 1530000, "texto": "25:30" },
    { "ms": 3540000, "texto": "59:00" },
    { "ms": 3599000, "texto": "59:59" },
    { "ms": 3599500, "texto": "01:00:00" },
    { "ms": 3600000, "texto": "01:00:00" },
    { "ms": 3900000, "texto": "01:05:00" },
    { "ms": 59500, "texto": "01:00" },
    { "ms": 59499, "texto": "00:59" },
    { "ms": -80000, "texto": "-01:20" },
    { "ms": -3600000, "texto": "-01:00:00" }
  ]
}
```

- [ ] **Passo 2: escrever o teste em JavaScript**

`web-testes/tempo.test.js`:

```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { interpretar, formatar } from '../public/usar/js/tempo.js';

const casos = JSON.parse(readFileSync(new URL('../casos-compartilhados.json', import.meta.url), 'utf8'));

test('interpretar segue os casos compartilhados', () => {
  for (const { texto, ms } of casos.interpretar)
    assert.equal(interpretar(texto), ms, `"${texto}"`);
});

test('formatar segue os casos compartilhados', () => {
  for (const { ms, texto } of casos.formatar)
    assert.equal(formatar(ms), texto, `${ms} ms`);
});

test('interpretar rejeita o que não é texto', () => {
  assert.equal(interpretar(null), null);
  assert.equal(interpretar(undefined), null);
  assert.equal(interpretar(25), null);
});
```

- [ ] **Passo 3: rodar e ver falhar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: FALHA com `Cannot find module` apontando para `public/usar/js/tempo.js`.

- [ ] **Passo 4: implementar `tempo.js`**

`public/usar/js/tempo.js`:

```js
// Leitura do tempo digitado e formatacao do mostrador.
// Mesmas regras do TimeParser do programa de Windows; os casos em
// casos-compartilhados.json garantem que as duas versoes concordam.

const INTEIRO = /^\d+$/;
const MINUTOS = /^(\d+\.?\d*|\.\d+)$/;

/**
 * Converte o texto digitado em milissegundos.
 * Aceita "MM:SS" (minutos podem passar de 59), "HH:MM:SS" e so minutos,
 * com fracao por ponto ou virgula ("7,5"). Devolve null se nao entender.
 */
export function interpretar(texto) {
  if (typeof texto !== 'string') return null;
  const limpo = texto.trim();
  if (limpo === '') return null;

  if (limpo.includes(':')) {
    const partes = limpo.split(':').map((p) => p.trim());
    if (partes.length < 2 || partes.length > 3) return null;
    if (!partes.every((p) => INTEIRO.test(p))) return null;
    const n = partes.map(Number);

    if (n.length === 2) {
      const [mm, ss] = n;
      if (ss > 59) return null;
      return (mm * 60 + ss) * 1000;
    }

    const [hh, mm, ss] = n;
    if (mm > 59 || ss > 59) return null;
    return ((hh * 60 + mm) * 60 + ss) * 1000;
  }

  const normalizado = limpo.replace(/,/g, '.');
  if (!MINUTOS.test(normalizado)) return null;
  return Math.round(Number(normalizado) * 60000);
}

/**
 * "MM:SS" abaixo de uma hora e "HH:MM:SS" a partir dela, arredondando para o
 * segundo mais proximo. Negativo (tempo excedido) leva "-" na frente.
 */
export function formatar(ms) {
  const negativo = ms < 0;
  const total = Math.round(Math.abs(ms) / 1000);
  const hh = Math.floor(total / 3600);
  const mm = Math.floor((total % 3600) / 60);
  const ss = total % 60;
  const dois = (v) => String(v).padStart(2, '0');
  const corpo = hh >= 1 ? `${dois(hh)}:${dois(mm)}:${dois(ss)}` : `${dois(mm)}:${dois(ss)}`;
  return negativo ? `-${corpo}` : corpo;
}
```

- [ ] **Passo 5: rodar e ver passar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 3 testes, 0 falhas.

- [ ] **Passo 6: escrever o teste em C#**

`CronoAula.Tests/CasosCompartilhadosTests.cs`:

```csharp
using System.Text.Json;
using CronoAula.Core;

namespace CronoAula.Tests;

/// <summary>
/// Le casos-compartilhados.json, o mesmo arquivo que os testes da versao web
/// leem. Se a regra mudar so no C# ou so no JavaScript, um dos lados falha.
/// </summary>
public class CasosCompartilhadosTests
{
    private static JsonElement Casos()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidato = Path.Combine(dir, "casos-compartilhados.json");
            if (File.Exists(candidato))
                return JsonDocument.Parse(File.ReadAllText(candidato)).RootElement;
            dir = Path.GetDirectoryName(dir);
        }

        throw new FileNotFoundException("casos-compartilhados.json nao foi encontrado a partir de " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Interpretar_SegueOsCasosCompartilhados()
    {
        var falhas = new List<string>();

        foreach (var caso in Casos().GetProperty("interpretar").EnumerateArray())
        {
            var texto = caso.GetProperty("texto").GetString();
            var esperado = caso.GetProperty("ms");
            var ok = TimeParser.TryParse(texto, out var tempo);

            if (esperado.ValueKind == JsonValueKind.Null)
            {
                if (ok)
                    falhas.Add($"\"{texto}\" deveria ser rejeitado");
            }
            else if (!ok || tempo.TotalMilliseconds != esperado.GetDouble())
            {
                var obtido = ok ? tempo.TotalMilliseconds.ToString() : "rejeitado";
                falhas.Add($"\"{texto}\": esperado {esperado.GetDouble()} ms, obtido {obtido}");
            }
        }

        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
    }

    [Fact]
    public void Formatar_SegueOsCasosCompartilhados()
    {
        var falhas = new List<string>();

        foreach (var caso in Casos().GetProperty("formatar").EnumerateArray())
        {
            var ms = caso.GetProperty("ms").GetDouble();
            var esperado = caso.GetProperty("texto").GetString();
            var obtido = TimeParser.Format(TimeSpan.FromMilliseconds(ms));

            if (obtido != esperado)
                falhas.Add($"{ms} ms: esperado \"{esperado}\", obtido \"{obtido}\"");
        }

        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
    }
}
```

- [ ] **Passo 7: rodar o C#**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~CasosCompartilhados"`
Esperado: 2 testes aprovados. Se algum caso falhar aqui e passar no JavaScript, a regra do
JavaScript está diferente do programa: corrija o `tempo.js`, nunca o `TimeParser`.

- [ ] **Passo 8: commit**

```bash
git add casos-compartilhados.json public/usar/js/tempo.js web-testes/tempo.test.js CronoAula.Tests/CasosCompartilhadosTests.cs
git commit -m "Versao web: leitura e formatacao do tempo, com casos compartilhados com o C#"
```

---

### Tarefa 2: `alertas.js`, faixas e linha de estado

**Arquivos:**
- Alterar: `casos-compartilhados.json` (nova chave `faixa`)
- Criar: `public/usar/js/alertas.js`
- Criar: `web-testes/alertas.test.js`
- Alterar: `CronoAula.Tests/CasosCompartilhadosTests.cs` (novo teste)

**Interfaces:**
- Consome: `MainViewModel.ComputeAlert(TimeSpan remaining, TimeSpan duration)` e o enum `AlertLevel { Normal, Atencao, Urgente, Estourado }` de `CronoAula.ViewModels` (internos, visíveis aos testes por `InternalsVisibleTo`).
- Produz:
  - `faixa(restanteMs: number, duracaoMs: number) => 'normal' | 'atencao' | 'urgente' | 'estourado'`
  - `nome(faixa) => string` (`''`, `'reta final'`, `'último minuto'`, `'tempo excedido'`)
  - `estadoEscrito({ estado, faixa, armado, toque }) => string`, com `estado` em `'parado' | 'contando' | 'pausado'`, `armado` em minutos ou `null`, `toque` booleano.

- [ ] **Passo 1: acrescentar os casos de faixa**

Em `casos-compartilhados.json`, depois do array `formatar` (lembre da vírgula depois do `]` de
`formatar`), acrescentar:

```json
  "faixa": [
    { "restanteMs": 480000, "duracaoMs": 600000, "faixa": "normal" },
    { "restanteMs": 540000, "duracaoMs": 600000, "faixa": "normal" },
    { "restanteMs": 120001, "duracaoMs": 600000, "faixa": "normal" },
    { "restanteMs": 120000, "duracaoMs": 600000, "faixa": "atencao" },
    { "restanteMs": 114000, "duracaoMs": 600000, "faixa": "atencao" },
    { "restanteMs": 60001, "duracaoMs": 600000, "faixa": "atencao" },
    { "restanteMs": 60000, "duracaoMs": 600000, "faixa": "urgente" },
    { "restanteMs": 45000, "duracaoMs": 600000, "faixa": "urgente" },
    { "restanteMs": 30000, "duracaoMs": 600000, "faixa": "urgente" },
    { "restanteMs": 0, "duracaoMs": 600000, "faixa": "estourado" },
    { "restanteMs": -1000, "duracaoMs": 600000, "faixa": "estourado" },
    { "restanteMs": -80000, "duracaoMs": 600000, "faixa": "estourado" },
    { "restanteMs": 300000, "duracaoMs": 3000000, "faixa": "normal" },
    { "restanteMs": 108000, "duracaoMs": 3000000, "faixa": "atencao" },
    { "restanteMs": 180000, "duracaoMs": 300000, "faixa": "normal" },
    { "restanteMs": 50000, "duracaoMs": 300000, "faixa": "urgente" },
    { "restanteMs": 84000, "duracaoMs": 420000, "faixa": "atencao" },
    { "restanteMs": 84001, "duracaoMs": 420000, "faixa": "normal" },
    { "restanteMs": 0, "duracaoMs": 0, "faixa": "estourado" }
  ]
```

- [ ] **Passo 2: escrever o teste em JavaScript**

`web-testes/alertas.test.js`:

```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { faixa, nome, estadoEscrito } from '../public/usar/js/alertas.js';

const casos = JSON.parse(readFileSync(new URL('../casos-compartilhados.json', import.meta.url), 'utf8'));

test('faixa segue os casos compartilhados', () => {
  for (const c of casos.faixa)
    assert.equal(faixa(c.restanteMs, c.duracaoMs), c.faixa, `${c.restanteMs} de ${c.duracaoMs}`);
});

test('cada faixa não normal tem nome escrito', () => {
  assert.equal(nome('normal'), '');
  assert.equal(nome('atencao'), 'reta final');
  assert.equal(nome('urgente'), 'último minuto');
  assert.equal(nome('estourado'), 'tempo excedido');
});

test('linha de estado: pausado vem primeiro', () => {
  assert.equal(estadoEscrito({ estado: 'pausado', faixa: 'estourado', armado: null, toque: false }), 'pausado');
});

test('linha de estado: tempo excedido vem antes do tempo carregado', () => {
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'estourado', armado: 5, toque: false }), 'tempo excedido');
});

test('linha de estado: tempo rápido carregado, com clique ou toque', () => {
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'normal', armado: 10, toque: false }),
    '10 min carregado. Clique de novo para iniciar');
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'normal', armado: 10, toque: true }),
    '10 min carregado. Toque de novo para iniciar');
});

test('linha de estado: tempo carregado não aparece contando', () => {
  assert.equal(estadoEscrito({ estado: 'contando', faixa: 'normal', armado: 10, toque: false }), '');
});

test('linha de estado: nome da faixa, contando ou parado', () => {
  assert.equal(estadoEscrito({ estado: 'contando', faixa: 'atencao', armado: null, toque: false }), 'reta final');
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'urgente', armado: null, toque: false }), 'último minuto');
});

test('linha de estado: vazia contando no normal, pronto parado', () => {
  assert.equal(estadoEscrito({ estado: 'contando', faixa: 'normal', armado: null, toque: false }), '');
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'normal', armado: null, toque: false }), 'pronto');
});
```

- [ ] **Passo 3: rodar e ver falhar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: FALHA em `alertas.test.js` com `Cannot find module`.

- [ ] **Passo 4: implementar `alertas.js`**

`public/usar/js/alertas.js`:

```js
// Faixas de alerta e a linha de estado embaixo dos digitos.
// Mesmas regras do MainViewModel.ComputeAlert do programa de Windows.

const MINUTO = 60000;

const NOMES = {
  normal: '',
  atencao: 'reta final',
  urgente: 'último minuto',
  estourado: 'tempo excedido',
};

/**
 * Reta final nos ultimos 20% do tempo ou 2 minutos, o que for menor; ultimo
 * minuto no minuto final; tempo excedido ao zerar ou passar do zero.
 */
export function faixa(restanteMs, duracaoMs) {
  if (restanteMs <= 0) return 'estourado';
  if (restanteMs <= MINUTO) return 'urgente';
  const limite = Math.min(duracaoMs * 0.2, 2 * MINUTO);
  return restanteMs <= limite ? 'atencao' : 'normal';
}

export function nome(f) {
  return NOMES[f] ?? '';
}

/**
 * Texto da linha de estado, em ordem de prioridade: pausado; tempo excedido;
 * tempo rapido carregado; nome da faixa; vazio contando no normal; pronto.
 */
export function estadoEscrito({ estado, faixa: f, armado, toque }) {
  if (estado === 'pausado') return 'pausado';
  if (f === 'estourado') return nome(f);
  if (armado !== null && armado !== undefined && estado === 'parado')
    return `${armado} min carregado. ${toque ? 'Toque' : 'Clique'} de novo para iniciar`;
  if (f !== 'normal') return nome(f);
  return estado === 'contando' ? '' : 'pronto';
}
```

- [ ] **Passo 5: rodar e ver passar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 11 testes, 0 falhas.

- [ ] **Passo 6: acrescentar o teste de faixas em C#**

Em `CronoAula.Tests/CasosCompartilhadosTests.cs`, acrescentar `using CronoAula.ViewModels;` logo
abaixo de `using CronoAula.Core;` e este método dentro da classe, depois de
`Formatar_SegueOsCasosCompartilhados`:

```csharp
    [Fact]
    public void Faixa_SegueOsCasosCompartilhados()
    {
        var falhas = new List<string>();

        foreach (var caso in Casos().GetProperty("faixa").EnumerateArray())
        {
            var restante = TimeSpan.FromMilliseconds(caso.GetProperty("restanteMs").GetDouble());
            var duracao = TimeSpan.FromMilliseconds(caso.GetProperty("duracaoMs").GetDouble());
            var esperado = caso.GetProperty("faixa").GetString();
            var obtido = MainViewModel.ComputeAlert(restante, duracao).ToString().ToLowerInvariant();

            if (obtido != esperado)
                falhas.Add($"{restante.TotalMilliseconds} de {duracao.TotalMilliseconds} ms: esperado {esperado}, obtido {obtido}");
        }

        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
    }
```

- [ ] **Passo 7: rodar o C#**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~CasosCompartilhados"`
Esperado: 3 testes aprovados.

- [ ] **Passo 8: commit**

```bash
git add casos-compartilhados.json public/usar/js/alertas.js web-testes/alertas.test.js CronoAula.Tests/CasosCompartilhadosTests.cs
git commit -m "Versao web: faixas de alerta e linha de estado"
```

---

### Tarefa 3: `motor.js`

**Arquivos:**
- Criar: `public/usar/js/motor.js`
- Criar: `web-testes/motor.test.js`

**Interfaces:**
- Consome: nada.
- Produz: `criarMotor({ relogio, negativo = true, avisoEm = null })`, em que `relogio()` devolve milissegundos monotônicos e `avisoEm` é o limiar do aviso em milissegundos ou `null`. O objeto tem:
  - `carregar(ms)`, `iniciar()`, `pausar()`, `zerar()`, `somar(ms)`
  - `atualizar() => Array<'aviso' | 'fim'>`
  - `restante() => number`, `duracao() => number`
  - `estado() => 'parado' | 'contando' | 'pausado'`
  - `estourado() => boolean` (restante abaixo de zero)
  - `configurar({ negativo?, avisoEm? })`

- [ ] **Passo 1: escrever o teste**

`web-testes/motor.test.js` (porta todos os casos de `CronoAula.Tests/TimerEngineTests.cs`, menos
o de relógio real, e acrescenta os da especificação):

```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { criarMotor } from '../public/usar/js/motor.js';

const MINUTO = 60000;
const SEGUNDO = 1000;

// Relogio falso: so anda quando o teste manda.
function relogioFalso() {
  let agora = 0;
  const relogio = () => agora;
  relogio.avancar = (ms) => { agora += ms; };
  return relogio;
}

function montar(opcoes = {}) {
  const relogio = relogioFalso();
  const motor = criarMotor({ relogio, ...opcoes });
  return { relogio, motor };
}

test('carregar deixa pronto com o tempo cheio, sem contar', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  assert.equal(motor.estado(), 'parado');
  assert.equal(motor.restante(), 10 * MINUTO);
  relogio.avancar(5 * MINUTO);
  assert.equal(motor.restante(), 10 * MINUTO);
});

test('carregar tempo negativo vira zero', () => {
  const { motor } = montar();
  motor.carregar(-5 * SEGUNDO);
  assert.equal(motor.duracao(), 0);
});

test('iniciar conta regressivo', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(3 * MINUTO);
  assert.equal(motor.estado(), 'contando');
  assert.equal(motor.restante(), 7 * MINUTO);
});

test('iniciar com o cronômetro já contando não faz nada', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(2 * MINUTO);
  motor.iniciar();
  relogio.avancar(1 * MINUTO);
  assert.equal(motor.restante(), 7 * MINUTO);
});

test('pausar congela o tempo e continuar retoma', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(2 * MINUTO);
  motor.pausar();
  relogio.avancar(5 * MINUTO);
  assert.equal(motor.restante(), 8 * MINUTO);
  assert.equal(motor.estado(), 'pausado');
  motor.iniciar();
  relogio.avancar(3 * MINUTO);
  assert.equal(motor.restante(), 5 * MINUTO);
});

test('zerar volta ao tempo cheio, parado', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(4 * MINUTO);
  motor.zerar();
  assert.equal(motor.estado(), 'parado');
  assert.equal(motor.restante(), 10 * MINUTO);
});

test('somar estende sem perder a contagem', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(4 * MINUTO);
  motor.somar(MINUTO);
  assert.equal(motor.restante(), 7 * MINUTO);
});

test('tirar minuto nunca deixa a duração negativa', () => {
  const { motor } = montar();
  motor.carregar(30 * SEGUNDO);
  motor.somar(-MINUTO);
  assert.equal(motor.duracao(), 0);
});

test('contagem negativa ligada segue abaixo de zero', () => {
  const { relogio, motor } = montar({ negativo: true });
  motor.carregar(MINUTO);
  motor.iniciar();
  relogio.avancar(80 * SEGUNDO);
  motor.atualizar();
  assert.equal(motor.estourado(), true);
  assert.equal(motor.restante(), -20 * SEGUNDO);
  assert.equal(motor.estado(), 'contando');
});

test('contagem negativa desligada para em zero', () => {
  const { relogio, motor } = montar({ negativo: false });
  motor.carregar(MINUTO);
  motor.iniciar();
  relogio.avancar(80 * SEGUNDO);
  assert.equal(motor.restante(), 0);
  motor.atualizar();
  assert.equal(motor.estourado(), false);
  assert.equal(motor.restante(), 0);
  assert.equal(motor.estado(), 'parado');
});

test('fim acontece uma vez ao cruzar o zero', () => {
  const { relogio, motor } = montar();
  motor.carregar(30 * SEGUNDO);
  motor.iniciar();
  relogio.avancar(29 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), []);
  relogio.avancar(2 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
  assert.deepEqual(motor.atualizar(), []);
});

test('aviso acontece uma vez no limiar', () => {
  const { relogio, motor } = montar({ avisoEm: 2 * MINUTO });
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(7 * MINUTO);
  assert.deepEqual(motor.atualizar(), []);
  relogio.avancar(1.5 * MINUTO);
  assert.deepEqual(motor.atualizar(), ['aviso']);
  assert.deepEqual(motor.atualizar(), []);
});

test('somar tempo que volta ao positivo permite novo fim', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * SEGUNDO);
  motor.iniciar();
  relogio.avancar(11 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
  motor.somar(30 * SEGUNDO);
  relogio.avancar(30 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
});

test('parado ou pausado, atualizar não produz eventos', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * SEGUNDO);
  relogio.avancar(20 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), []);
  motor.iniciar();
  motor.pausar();
  relogio.avancar(20 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), []);
});

test('configurar muda a contagem negativa e o aviso com a página aberta', () => {
  const { relogio, motor } = montar({ negativo: true, avisoEm: null });
  motor.configurar({ negativo: false, avisoEm: MINUTO });
  motor.carregar(2 * MINUTO);
  motor.iniciar();
  relogio.avancar(90 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['aviso']);
  relogio.avancar(60 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
  assert.equal(motor.restante(), 0);
  assert.equal(motor.estado(), 'parado');
});

test('sem deriva: 6.000 atualizações em 10 minutos terminam exatamente em zero', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  for (let i = 0; i < 6000; i++) {
    relogio.avancar(100);
    motor.atualizar();
  }
  assert.equal(motor.restante(), 0);
});
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: FALHA em `motor.test.js` com `Cannot find module`.

- [ ] **Passo 3: implementar `motor.js`**

`public/usar/js/motor.js`:

```js
// Contagem regressiva. Mesmas regras do TimerEngine do programa de Windows.
//
// O restante e sempre (duracao - tempo decorrido), com o decorrido lido de um
// relogio monotonico. Nunca se soma tick de temporizador, entao milhares de
// atualizacoes nao acumulam erro.

export function criarMotor({ relogio, negativo = true, avisoEm = null }) {
  let duracaoMs = 0;
  let acumulado = 0;   // decorrido ate a ultima pausa
  let inicio = 0;      // leitura do relogio quando a contagem atual comecou
  let estado = 'parado';
  let fimDisparado = false;
  let avisoDisparado = false;
  let contagemNegativa = negativo;
  let limiarAviso = avisoEm;

  const decorrido = () => (estado === 'contando' ? acumulado + (relogio() - inicio) : acumulado);
  const bruto = () => duracaoMs - decorrido();

  function carregar(ms) {
    duracaoMs = Math.max(0, ms);
    acumulado = 0;
    fimDisparado = false;
    avisoDisparado = false;
    estado = 'parado';
  }

  function iniciar() {
    if (estado === 'contando') return;
    inicio = relogio();
    estado = 'contando';
  }

  function pausar() {
    if (estado !== 'contando') return;
    acumulado += relogio() - inicio;
    estado = 'pausado';
  }

  function zerar() {
    acumulado = 0;
    fimDisparado = false;
    avisoDisparado = false;
    estado = 'parado';
  }

  function somar(ms) {
    duracaoMs = Math.max(0, duracaoMs + ms);
    // Voltou ao positivo: o fim pode acontecer de novo mais tarde.
    if (bruto() > 0) fimDisparado = false;
  }

  function atualizar() {
    if (estado !== 'contando') return [];
    const eventos = [];
    const rem = bruto();

    if (limiarAviso !== null && !avisoDisparado && rem <= limiarAviso && rem > 0) {
      avisoDisparado = true;
      eventos.push('aviso');
    }

    if (rem <= 0 && !fimDisparado) {
      fimDisparado = true;
      eventos.push('fim');
      if (!contagemNegativa) {
        acumulado = decorrido();
        estado = 'parado';
      }
    }

    return eventos;
  }

  function restante() {
    const rem = bruto();
    return !contagemNegativa && rem < 0 ? 0 : rem;
  }

  function configurar(opcoes) {
    if ('negativo' in opcoes) contagemNegativa = opcoes.negativo;
    if ('avisoEm' in opcoes) limiarAviso = opcoes.avisoEm;
  }

  return {
    carregar,
    iniciar,
    pausar,
    zerar,
    somar,
    atualizar,
    restante,
    duracao: () => duracaoMs,
    estado: () => estado,
    estourado: () => restante() < 0,
    configurar,
  };
}
```

- [ ] **Passo 4: rodar e ver passar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 27 testes, 0 falhas.

- [ ] **Passo 5: commit**

```bash
git add public/usar/js/motor.js web-testes/motor.test.js
git commit -m "Versao web: motor da contagem, com os casos do TimerEngine"
```

---

### Tarefa 4: `ajustes.js`

**Arquivos:**
- Criar: `public/usar/js/ajustes.js`
- Criar: `web-testes/ajustes.test.js`

**Interfaces:**
- Consome: nada. O armazenamento é qualquer objeto com `getItem(chave)` e `setItem(chave, valor)`, ou `null`.
- Produz:
  - `CHAVE = 'cronoaula.ajustes'`
  - `PADROES` congelado: `{ som: true, volume: 0.7, repeticoes: 5, intervalo: 3, avisoLigado: true, avisoMinutos: 5, negativo: true, ultimoMinutos: 50 }`
  - `normalizar(bruto) => ajustes` (objeto novo, sempre completo)
  - `carregar(armazenamento) => ajustes` (nunca lança)
  - `salvar(armazenamento, ajustes) => boolean` (nunca lança; `false` se não gravou)

- [ ] **Passo 1: escrever o teste**

`web-testes/ajustes.test.js`:

```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { CHAVE, PADROES, normalizar, carregar, salvar } from '../public/usar/js/ajustes.js';

function armazenamentoFalso(inicial = {}) {
  const dados = new Map(Object.entries(inicial));
  return {
    dados,
    getItem: (k) => (dados.has(k) ? dados.get(k) : null),
    setItem: (k, v) => { dados.set(k, String(v)); },
  };
}

const quebrado = {
  getItem() { throw new Error('bloqueado'); },
  setItem() { throw new Error('bloqueado'); },
};

test('sem nada guardado, usa os padrões', () => {
  assert.deepEqual(carregar(armazenamentoFalso()), PADROES);
});

test('os padrões são os da especificação', () => {
  assert.deepEqual({ ...PADROES }, {
    som: true, volume: 0.7, repeticoes: 5, intervalo: 3,
    avisoLigado: true, avisoMinutos: 5, negativo: true, ultimoMinutos: 50,
  });
});

test('salvar e carregar devolve o mesmo', () => {
  const a = armazenamentoFalso();
  const meus = { ...PADROES, som: false, volume: 0.3, repeticoes: 0, intervalo: 10, avisoMinutos: 2, negativo: false, ultimoMinutos: 7.5 };
  assert.equal(salvar(a, meus), true);
  assert.ok(a.dados.has(CHAVE));
  assert.deepEqual(carregar(a), meus);
});

test('valores fora da faixa são trazidos para dentro', () => {
  const r = normalizar({ volume: 3, repeticoes: 999, intervalo: 0, avisoMinutos: 500 });
  assert.equal(r.volume, 1);
  assert.equal(r.repeticoes, 60);
  assert.equal(r.intervalo, 1);
  assert.equal(r.avisoMinutos, 120);
  const s = normalizar({ volume: -1, repeticoes: -3, intervalo: 99, avisoMinutos: 0 });
  assert.equal(s.volume, 0);
  assert.equal(s.repeticoes, 0);
  assert.equal(s.intervalo, 30);
  assert.equal(s.avisoMinutos, 1);
});

test('contagens viram números inteiros', () => {
  const r = normalizar({ repeticoes: 2.6, intervalo: 4.2, avisoMinutos: 3.5 });
  assert.equal(r.repeticoes, 3);
  assert.equal(r.intervalo, 4);
  assert.equal(r.avisoMinutos, 4);
});

test('último tempo fora de 0 a 1440 volta para 50', () => {
  assert.equal(normalizar({ ultimoMinutos: 2000 }).ultimoMinutos, 50);
  assert.equal(normalizar({ ultimoMinutos: -1 }).ultimoMinutos, 50);
  assert.equal(normalizar({ ultimoMinutos: 0 }).ultimoMinutos, 0);
  assert.equal(normalizar({ ultimoMinutos: 1440 }).ultimoMinutos, 1440);
});

test('tipo errado volta ao padrão', () => {
  const r = normalizar({ som: 'sim', volume: '0.5', repeticoes: null, negativo: 1, ultimoMinutos: 'x' });
  assert.equal(r.som, true);
  assert.equal(r.volume, 0.7);
  assert.equal(r.repeticoes, 5);
  assert.equal(r.negativo, true);
  assert.equal(r.ultimoMinutos, 50);
});

test('JSON corrompido volta aos padrões', () => {
  assert.deepEqual(carregar(armazenamentoFalso({ [CHAVE]: '{isso não é json' })), PADROES);
  assert.deepEqual(carregar(armazenamentoFalso({ [CHAVE]: '42' })), PADROES);
  assert.deepEqual(carregar(armazenamentoFalso({ [CHAVE]: 'null' })), PADROES);
});

test('armazenamento que lança erro não impede de funcionar', () => {
  assert.deepEqual(carregar(quebrado), PADROES);
  assert.equal(salvar(quebrado, PADROES), false);
});

test('sem armazenamento nenhum, também funciona', () => {
  assert.deepEqual(carregar(null), PADROES);
  assert.equal(salvar(null, PADROES), false);
});

test('carregar devolve cópia, sem expor os padrões', () => {
  const r = carregar(armazenamentoFalso());
  r.volume = 0.1;
  assert.equal(PADROES.volume, 0.7);
});
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: FALHA em `ajustes.test.js` com `Cannot find module`.

- [ ] **Passo 3: implementar `ajustes.js`**

`public/usar/js/ajustes.js`:

```js
// Preferencias guardadas no navegador. O armazenamento vem de fora (na pagina,
// localStorage; nos testes, um falso). Armazenamento bloqueado ou corrompido
// nunca impede a pagina de funcionar: ela usa os padroes e segue.

export const CHAVE = 'cronoaula.ajustes';

export const PADROES = Object.freeze({
  som: true,
  volume: 0.7,
  repeticoes: 5,
  intervalo: 3,
  avisoLigado: true,
  avisoMinutos: 5,
  negativo: true,
  ultimoMinutos: 50,
});

const LIGA_DESLIGA = ['som', 'avisoLigado', 'negativo'];

// chave: [minimo, maximo, inteiro]
const FAIXAS = {
  volume: [0, 1, false],
  repeticoes: [0, 60, true],
  intervalo: [1, 30, true],
  avisoMinutos: [1, 120, true],
};

const numero = (v) => typeof v === 'number' && Number.isFinite(v);

export function normalizar(bruto) {
  const b = bruto && typeof bruto === 'object' ? bruto : {};
  const r = { ...PADROES };

  for (const chave of LIGA_DESLIGA)
    if (typeof b[chave] === 'boolean') r[chave] = b[chave];

  for (const [chave, [min, max, inteiro]] of Object.entries(FAIXAS)) {
    if (!numero(b[chave])) continue;
    const v = inteiro ? Math.round(b[chave]) : b[chave];
    r[chave] = Math.min(max, Math.max(min, v));
  }

  // O ultimo tempo usado nao e trazido para dentro da faixa: fora dela, volta a 50.
  if (numero(b.ultimoMinutos) && b.ultimoMinutos >= 0 && b.ultimoMinutos <= 1440)
    r.ultimoMinutos = b.ultimoMinutos;

  return r;
}

export function carregar(armazenamento) {
  try {
    const texto = armazenamento.getItem(CHAVE);
    if (!texto) return { ...PADROES };
    return normalizar(JSON.parse(texto));
  } catch {
    return { ...PADROES };
  }
}

export function salvar(armazenamento, ajustes) {
  try {
    armazenamento.setItem(CHAVE, JSON.stringify(normalizar(ajustes)));
    return true;
  } catch {
    return false;
  }
}
```

- [ ] **Passo 4: rodar e ver passar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 38 testes, 0 falhas.

- [ ] **Passo 5: commit**

```bash
git add public/usar/js/ajustes.js web-testes/ajustes.test.js
git commit -m "Versao web: ajustes guardados no navegador"
```

---

### Tarefa 5: `som.js`

**Arquivos:**
- Criar: `public/usar/js/som.js`
- Criar: `web-testes/som.test.js`

**Interfaces:**
- Consome: nada (o contexto de áudio e a busca dos arquivos são injetáveis).
- Produz:
  - `planejar({ fimEmMs, avisoEmMs, repeticoes, intervaloS, volume, ligado }) => Array<{ som: 'alerta' | 'aviso', emS: number, volume: number }>`
  - `criarSom({ criarContexto?, buscar? })` com:
    - `liberar() => Promise` (cria o contexto no primeiro gesto e carrega os sons; idempotente)
    - `agendar({ fimEmMs, avisoEmMs, repeticoes, intervaloS, volume, ligado }) => Promise` (cancela o anterior e agenda; `avisoEmMs` nulo desliga o aviso)
    - `cancelar()`
    - `silenciar() => boolean` (só interrompe um alerta de fim que já começou)
    - `testarAlerta(volume) => Promise`, `testarAviso(volume) => Promise`

Regras: `ligado` falso desliga os dois sons (como o `SoundEnabled` do programa). O aviso toca a 70%
do volume. `repeticoes` 0 quer dizer 60 toques. Só entra no plano o que está no futuro.

- [ ] **Passo 1: escrever o teste**

`web-testes/som.test.js`:

```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { planejar, criarSom } from '../public/usar/js/som.js';

const base = { fimEmMs: 10000, avisoEmMs: 4000, repeticoes: 5, intervaloS: 3, volume: 0.5, ligado: true };

test('plano: aviso uma vez a 70% e alerta repetido a partir do zero', () => {
  assert.deepEqual(planejar(base), [
    { som: 'aviso', emS: 4, volume: 0.35 },
    { som: 'alerta', emS: 10, volume: 0.5 },
    { som: 'alerta', emS: 13, volume: 0.5 },
    { som: 'alerta', emS: 16, volume: 0.5 },
    { som: 'alerta', emS: 19, volume: 0.5 },
    { som: 'alerta', emS: 22, volume: 0.5 },
  ]);
});

test('plano: zero repetições toca 60 vezes, e o teto é 60', () => {
  assert.equal(planejar({ ...base, avisoEmMs: null, repeticoes: 0 }).length, 60);
  assert.equal(planejar({ ...base, avisoEmMs: null, repeticoes: 100 }).length, 60);
});

test('plano: som desligado não toca nada', () => {
  assert.deepEqual(planejar({ ...base, ligado: false }), []);
});

test('plano: só o que está no futuro', () => {
  assert.deepEqual(planejar({ ...base, avisoEmMs: null }).filter((p) => p.som === 'aviso'), []);
  assert.deepEqual(planejar({ ...base, avisoEmMs: -1 }).filter((p) => p.som === 'aviso'), []);
  assert.deepEqual(planejar({ ...base, avisoEmMs: null, fimEmMs: 0 }), []);
  assert.deepEqual(planejar({ ...base, avisoEmMs: null, fimEmMs: -5000 }), []);
});

// Contexto de audio falso: registra o que foi agendado.
function montar() {
  const fontes = [];
  const ctx = {
    currentTime: 0,
    state: 'running',
    destination: {},
    resume() { this.state = 'running'; return Promise.resolve(); },
    decodeAudioData: (dados) => Promise.resolve({ nome: dados.nome }),
    createGain: () => ({ gain: { value: 1 }, connect() {} }),
    createBufferSource() {
      const fonte = {
        buffer: null, ganho: null, inicio: null, parado: false,
        connect(g) { this.ganho = g; },
        start(quando) { this.inicio = quando; },
        stop() { this.parado = true; },
      };
      fontes.push(fonte);
      return fonte;
    },
  };
  const buscar = async (url) => ({
    ok: true,
    arrayBuffer: async () => ({ nome: String(url).includes('aviso') ? 'aviso' : 'alerta' }),
  });
  const som = criarSom({ criarContexto: () => ctx, buscar });
  return { ctx, fontes, som };
}

test('agendar antes de liberar não quebra e não agenda', async () => {
  const { fontes, som } = montar();
  await som.agendar(base);
  assert.equal(fontes.length, 0);
});

test('agendar põe cada som no relógio do áudio, com o volume certo', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  assert.deepEqual(fontes.map((f) => [f.buffer.nome, f.inicio, f.ganho.gain.value]), [
    ['aviso', 4, 0.35],
    ['alerta', 10, 0.5],
    ['alerta', 13, 0.5],
    ['alerta', 16, 0.5],
    ['alerta', 19, 0.5],
    ['alerta', 22, 0.5],
  ]);
});

test('agendar de novo cancela o anterior', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  await som.agendar({ ...base, avisoEmMs: null, repeticoes: 1 });
  assert.equal(fontes.slice(0, 6).every((f) => f.parado), true);
  assert.equal(fontes.length, 7);
  assert.equal(fontes[6].parado, false);
});

test('silenciar não cancela um alerta que ainda não começou', async () => {
  const { ctx, fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  ctx.currentTime = 9;
  assert.equal(som.silenciar(), false);
  assert.equal(fontes.some((f) => f.parado), false);
});

test('silenciar interrompe o alerta que já começou e as repetições restantes', async () => {
  const { ctx, fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  ctx.currentTime = 11;
  assert.equal(som.silenciar(), true);
  assert.equal(fontes.filter((f) => f.buffer.nome === 'alerta').every((f) => f.parado), true);
  assert.equal(som.silenciar(), false);
});

test('um teste de som tocando não faz o silenciar cancelar o alerta futuro', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  await som.testarAlerta(0.5);
  assert.equal(som.silenciar(), false);
  assert.equal(fontes.slice(0, 6).some((f) => f.parado), false);
});

test('testar toca na hora; o aviso a 70%', async () => {
  const { ctx, fontes, som } = montar();
  ctx.currentTime = 3;
  await som.testarAviso(1);
  assert.equal(fontes.length, 1);
  assert.equal(fontes[0].buffer.nome, 'aviso');
  assert.equal(fontes[0].inicio, 3);
  assert.equal(fontes[0].ganho.gain.value, 0.7);
});

test('cancelar para tudo', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  som.cancelar();
  assert.equal(fontes.every((f) => f.parado), true);
});
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: FALHA em `som.test.js` com `Cannot find module`.

- [ ] **Passo 3: implementar `som.js`**

`public/usar/js/som.js`:

```js
// Os dois sons do CronoAula, agendados no relogio do audio do navegador.
//
// Com a aba em segundo plano, o Chrome atrasa temporizadores em ate um minuto.
// O relogio do audio nao sofre esse atraso, entao o som e agendado com
// AudioBufferSourceNode.start(quando) em vez de disparado por setTimeout.

const MAX_TOQUES = 60;
const VOLUME_AVISO = 0.7;

const SONS = {
  alerta: new URL('../sons/alerta.wav', import.meta.url),
  aviso: new URL('../sons/aviso.wav', import.meta.url),
};

/** O que tocar e quando, em segundos a partir de agora. So entra o que esta no futuro. */
export function planejar({ fimEmMs, avisoEmMs, repeticoes, intervaloS, volume, ligado }) {
  const plano = [];
  if (!ligado) return plano;

  if (avisoEmMs !== null && avisoEmMs !== undefined && avisoEmMs > 0)
    plano.push({ som: 'aviso', emS: avisoEmMs / 1000, volume: volume * VOLUME_AVISO });

  if (fimEmMs > 0) {
    const toques = repeticoes > 0 ? Math.min(repeticoes, MAX_TOQUES) : MAX_TOQUES;
    for (let i = 0; i < toques; i++)
      plano.push({ som: 'alerta', emS: fimEmMs / 1000 + i * intervaloS, volume });
  }

  return plano;
}

export function criarSom({
  criarContexto = () => new (window.AudioContext || window.webkitAudioContext)(),
  buscar = (url) => fetch(url),
} = {}) {
  let ctx = null;
  let buffers = null;
  let carregando = null;
  let agendados = [];  // { fonte, tipo: 'alerta' | 'aviso' | 'teste', quando }
  let geracao = 0;     // invalida um agendar que ainda esperava os sons carregarem

  async function carregarBuffers() {
    try {
      const [alerta, aviso] = await Promise.all([SONS.alerta, SONS.aviso].map(async (url) => {
        const resposta = await buscar(url);
        if (!resposta.ok) throw new Error('som indisponivel');
        return ctx.decodeAudioData(await resposta.arrayBuffer());
      }));
      buffers = { alerta, aviso };
    } catch {
      carregando = null; // tenta de novo no proximo gesto
    }
  }

  /** Chamado a cada clique ou tecla: o navegador so libera audio depois de um gesto. */
  function liberar() {
    if (!ctx) {
      try {
        ctx = criarContexto();
      } catch {
        return Promise.resolve();
      }
    }
    if (ctx.state !== 'running') ctx.resume().catch(() => {});
    if (!buffers && !carregando) carregando = carregarBuffers();
    return carregando ?? Promise.resolve();
  }

  function parar(fonte) {
    try {
      fonte.stop();
    } catch {
      // ja tinha parado
    }
  }

  function tocar(chave, quando, volume, tipo) {
    const fonte = ctx.createBufferSource();
    fonte.buffer = buffers[chave];
    const ganho = ctx.createGain();
    ganho.gain.value = volume;
    fonte.connect(ganho);
    ganho.connect(ctx.destination);
    fonte.start(Math.max(quando, ctx.currentTime));
    agendados.push({ fonte, tipo, quando });
  }

  function cancelar() {
    geracao++;
    for (const a of agendados) parar(a.fonte);
    agendados = [];
  }

  async function agendar(opcoes) {
    cancelar();
    const minha = geracao;
    if (!ctx) return;
    const base = ctx.currentTime;
    await liberar();
    if (minha !== geracao || !buffers) return;
    for (const item of planejar(opcoes)) tocar(item.som, base + item.emS, item.volume, item.som);
  }

  /** Interrompe o alerta de fim se ele ja comecou. Alerta ainda no futuro continua agendado. */
  function silenciar() {
    if (!ctx) return false;
    const alertas = agendados.filter((a) => a.tipo === 'alerta');
    if (!alertas.some((a) => a.quando <= ctx.currentTime)) return false;
    for (const a of alertas) parar(a.fonte);
    agendados = agendados.filter((a) => a.tipo !== 'alerta');
    return true;
  }

  async function testar(chave, volume) {
    await liberar();
    if (!buffers) return;
    tocar(chave, ctx.currentTime, volume, 'teste');
  }

  return {
    liberar,
    agendar,
    cancelar,
    silenciar,
    testarAlerta: (volume) => testar('alerta', volume),
    testarAviso: (volume) => testar('aviso', volume * VOLUME_AVISO),
  };
}
```

- [ ] **Passo 4: rodar e ver passar**

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 50 testes, 0 falhas. Se `testar toca na hora` falhar com `fontes.length` 0, o
`testar` não chamou `liberar()` antes de conferir `buffers`.

- [ ] **Passo 5: commit**

```bash
git add public/usar/js/som.js web-testes/som.test.js
git commit -m "Versao web: sons agendados no relogio do audio"
```

---

### Tarefa 6: a página `usar/`

**Arquivos:**
- Copiar: `CronoAula/Assets/alerta.wav` e `aviso.wav` para `public/usar/sons/`
- Alterar: `CronoAula.Tests/SiteTests.cs`
- Criar: `public/usar/index.html`
- Criar: `public/usar/usar.css`
- Criar: `public/usar/js/tela.js`

**Interfaces:**
- Consome: `criarMotor` (tarefa 3), `interpretar`, `formatar` (tarefa 1), `faixa`, `estadoEscrito` (tarefa 2), `criarSom` (tarefa 5), `carregar`, `salvar`, `normalizar`, `PADROES` (tarefa 4).
- Produz: a página. IDs usados pelo `tela.js`: `digitos`, `estado`, `iniciar`, `zerar`, `mais-um`, `form-tempo`, `campo-tempo`, `erro-tempo`, `tela-cheia`, `abrir-ajustes`, `barra-iniciar`, `barra-zerar`, `barra-mais`, `barra-sair`, `ajustes` (o `<dialog>`), `form-ajustes`, `volume-texto`, `testar-alerta`, `testar-aviso`, `restaurar`. Tempos rápidos: botões com `data-min`. O `body` recebe `data-faixa` e as classes `cheia`, `projecao` e `barra-visivel`.

- [ ] **Passo 1: escrever os testes do site**

Em `CronoAula.Tests/SiteTests.cs`:

1. Acrescentar `using System.Text.RegularExpressions;` como primeira linha do arquivo, antes de `namespace`.
2. Acrescentar `".wav"` ao `ExtensoesDeSite`, que fica:

```csharp
    private static readonly HashSet<string> ExtensoesDeSite = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".css", ".js", ".ico", ".png", ".svg", ".jpg", ".jpeg", ".webp",
        ".txt", ".xml", ".webmanifest", ".wav"
    };
```

3. Acrescentar estes testes no fim da classe, antes da última `}`:

```csharp
    private static string PastaUsar() => Path.Combine(PastaDoSite(), "usar");

    [Fact]
    public void Usar_PaginaExiste()
    {
        Assert.True(File.Exists(Path.Combine(PastaUsar(), "index.html")), "public/usar/index.html nao existe.");
    }

    [Fact]
    public void Usar_TodoArquivoCarregadoExiste()
    {
        // A pagina e os modulos se referem uns aos outros por caminho relativo.
        // Um nome errado so apareceria no navegador, como tela parada.
        var raiz = PastaDoSite();
        var faltando = new List<string>();

        void Conferir(string origem, string referencia)
        {
            if (referencia.StartsWith("http", StringComparison.Ordinal) || referencia.StartsWith('#') || referencia.StartsWith("mailto:", StringComparison.Ordinal))
                return;

            var caminho = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(origem)!, referencia.Replace('/', Path.DirectorySeparatorChar)));
            if (referencia.EndsWith('/'))
                caminho = Path.Combine(caminho, "index.html");

            if (!File.Exists(caminho))
                faltando.Add($"{Path.GetRelativePath(raiz, origem)} -> {referencia}");
        }

        var html = Path.Combine(PastaUsar(), "index.html");
        foreach (Match m in Regex.Matches(File.ReadAllText(html), "(?:src|href)=\"([^\"]+)\""))
            Conferir(html, m.Groups[1].Value);

        foreach (var js in Directory.EnumerateFiles(Path.Combine(PastaUsar(), "js"), "*.js"))
        {
            var texto = File.ReadAllText(js);
            foreach (Match m in Regex.Matches(texto, "from '([^']+)'"))
                Conferir(js, m.Groups[1].Value);
            foreach (Match m in Regex.Matches(texto, @"new URL\('([^']+)', import\.meta\.url\)"))
                Conferir(js, m.Groups[1].Value);
        }

        Assert.True(faltando.Count == 0, "Referencias quebradas: " + string.Join(", ", faltando));
    }

    [Theory]
    [InlineData("alerta.wav")]
    [InlineData("aviso.wav")]
    public void Usar_SonsSaoOsMesmosDoPrograma(string nome)
    {
        var raiz = PastaDoSite();
        var web = File.ReadAllBytes(Path.Combine(raiz, "usar", "sons", nome));
        var programa = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(raiz)!, "CronoAula", "Assets", nome));

        Assert.True(web.AsSpan().SequenceEqual(programa),
            $"public/usar/sons/{nome} difere de CronoAula/Assets/{nome}. Copie o do programa de novo.");
    }

    [Fact]
    public void Public_NaoTemArquivoDeTeste()
    {
        var raiz = PastaDoSite();
        var testes = Directory.EnumerateFiles(raiz, "*.test.*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(raiz, f))
            .ToList();

        Assert.True(testes.Count == 0, "Arquivos de teste dentro de public/: " + string.Join(", ", testes) + ". Eles ficam em web-testes/.");
    }

    [Fact]
    public void Usar_NaoCarregaNadaDeFora()
    {
        // Promessa de "sem rede e sem telemetria": a pagina nao busca nada em
        // outro endereco. Links clicaveis no rodape nao contam, sao navegacao.
        var pasta = PastaUsar();

        var html = File.ReadAllText(Path.Combine(pasta, "index.html"));
        Assert.DoesNotMatch("src=\"(https?:)?//", html);
        Assert.DoesNotMatch("<link[^>]+rel=\"(stylesheet|preload|modulepreload|preconnect)\"[^>]+href=\"(https?:)?//", html);

        var css = File.ReadAllText(Path.Combine(pasta, "usar.css"));
        Assert.DoesNotContain("@import", css);
        Assert.DoesNotMatch(@"url\(\s*['""]?(https?:)?//", css);

        foreach (var js in Directory.EnumerateFiles(Path.Combine(pasta, "js"), "*.js"))
            Assert.DoesNotMatch("https?://", File.ReadAllText(js));
    }

    [Fact]
    public void Usar_RodapeSegueOPadraoDoEcossistema()
    {
        var html = File.ReadAllText(Path.Combine(PastaUsar(), "index.html"));

        Assert.Contains("Desenvolvido e publicado por", html);
        Assert.Contains("href=\"https://www.manfred.com.br\"", html);
        Assert.Contains("href=\"https://escalada.dev\"", html);
        Assert.Contains("src=\"../logo-mt.png\"", html);
        Assert.Contains("GPL-3.0", html[html.IndexOf("<footer", StringComparison.Ordinal)..]);
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~SiteTests"`
Esperado: FALHAM `Usar_PaginaExiste`, `Usar_TodoArquivoCarregadoExiste`,
`Usar_SonsSaoOsMesmosDoPrograma`, `Usar_NaoCarregaNadaDeFora` e
`Usar_RodapeSegueOPadraoDoEcossistema` (arquivos inexistentes). `Public_NaoTemArquivoDeTeste`
passa.

- [ ] **Passo 3: copiar os sons**

```bash
mkdir -p public/usar/sons && cp CronoAula/Assets/alerta.wav CronoAula/Assets/aviso.wav public/usar/sons/
```

- [ ] **Passo 4: criar `public/usar/index.html`**

```html
<!DOCTYPE html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>CronoAula no navegador</title>
<meta name="description" content="Cronômetro de contagem regressiva para aulas, direto no navegador. Projete para a turma em tela cheia ou use no celular, sem instalar nada.">
<meta name="theme-color" content="#151917">
<link rel="icon" href="../favicon.ico">
<link rel="canonical" href="https://cronoaula.escalada.dev/usar/">
<link rel="stylesheet" href="usar.css">
<script type="module" src="js/tela.js"></script>
</head>
<body data-faixa="normal">

<main class="palco">
  <div class="topo">
    <button type="button" class="discreto" id="tela-cheia">Tela cheia (F)</button>
    <button type="button" class="discreto" id="abrir-ajustes" aria-haspopup="dialog">Ajustes</button>
  </div>

  <section class="cartao" aria-label="Cronômetro">
    <div class="digitos" id="digitos" role="timer">50:00</div>
    <div class="estado" id="estado" aria-live="polite">pronto</div>

    <div class="linha presets" role="group" aria-label="Tempos rápidos, em minutos">
      <button type="button" class="botao" data-min="5" aria-pressed="false">5</button>
      <button type="button" class="botao" data-min="10" aria-pressed="false">10</button>
      <button type="button" class="botao" data-min="15" aria-pressed="false">15</button>
      <button type="button" class="botao" data-min="30" aria-pressed="false">30</button>
      <button type="button" class="botao" data-min="50" aria-pressed="false">50</button>
    </div>

    <div class="linha controles">
      <button type="button" class="botao principal" id="iniciar">Iniciar</button>
      <button type="button" class="botao" id="zerar">Zerar</button>
      <button type="button" class="botao" id="mais-um">+1 min</button>
    </div>

    <form class="linha" id="form-tempo" autocomplete="off">
      <label class="oculto" for="campo-tempo">Tempo: minutos, MM:SS ou HH:MM:SS</label>
      <input type="text" id="campo-tempo" placeholder="25:00" enterkeyhint="done" spellcheck="false">
      <button type="submit" class="botao">Carregar</button>
    </form>
    <p class="erro" id="erro-tempo" role="alert" hidden>Não entendi esse tempo. Use 25, 25:30 ou 01:05:00.</p>
  </section>

  <p class="atalhos">Espaço inicia e pausa · R zera · setas somam e tiram 1 minuto · F tela cheia</p>
  <p class="voltar"><a href="../">Sobre o CronoAula e a versão para Windows</a></p>

  <div class="barra" id="barra" role="toolbar" aria-label="Controles da tela cheia">
    <button type="button" class="botao principal" id="barra-iniciar">Pausar</button>
    <button type="button" class="botao" id="barra-zerar">Zerar</button>
    <button type="button" class="botao" id="barra-mais">+1 min</button>
    <button type="button" class="botao" id="barra-sair">Sair (Esc)</button>
  </div>
</main>

<dialog id="ajustes" aria-labelledby="titulo-ajustes">
  <form method="dialog" id="form-ajustes">
    <h2 id="titulo-ajustes" class="oculto">Ajustes</h2>

    <h3 class="grupo">Som</h3>
    <label class="opcao"><input type="checkbox" name="som"> Tocar sinal ao terminar</label>
    <label class="opcao">Volume
      <input type="range" name="volume" min="0" max="100" step="5">
      <span id="volume-texto">70%</span>
    </label>
    <div class="opcao">
      <span>Repetir</span>
      <input type="number" name="repeticoes" min="0" max="60" step="1" inputmode="numeric" aria-label="Quantas vezes repetir o alerta">
      <span>vezes, a cada</span>
      <input type="number" name="intervalo" min="1" max="30" step="1" inputmode="numeric" aria-label="Segundos entre as repetições">
      <span>s</span>
    </div>
    <p class="dica">Com 0, repete até alguém mexer, no máximo 60 vezes.</p>
    <div class="opcao">
      <button type="button" class="botao pequeno" id="testar-alerta">Testar alerta</button>
      <button type="button" class="botao pequeno" id="testar-aviso">Testar aviso</button>
    </div>

    <h3 class="grupo">Contagem</h3>
    <label class="opcao"><input type="checkbox" name="negativo"> Continuar contando em negativo após o zero</label>
    <div class="opcao">
      <input type="checkbox" id="aviso-ligado" name="avisoLigado">
      <label for="aviso-ligado">Avisar faltando</label>
      <input type="number" name="avisoMinutos" min="1" max="120" step="1" inputmode="numeric" aria-label="Minutos antes do fim">
      <span>minutos</span>
    </div>

    <div class="acoes">
      <button type="button" class="botao" id="restaurar">Restaurar padrões</button>
      <button type="submit" class="botao principal" value="pronto">Pronto</button>
    </div>
  </form>
</dialog>

<footer class="rodape">
  <div class="rodape-texto">
    <p>
      Desenvolvido e publicado por
      <a class="rodape-forte" href="https://www.manfred.com.br" target="_blank" rel="noopener">MT - Manfred Tecnologia</a>
      na plataforma
      <a class="rodape-forte" href="https://escalada.dev" target="_blank" rel="noopener">Escalada.dev</a>
    </p>
    <p class="rodape-links">
      <a href="https://github.com/manfredjr/cronoaula#readme">Manual de uso</a> &middot;
      <a href="https://github.com/manfredjr/cronoaula">Código-fonte</a> &middot;
      <a href="https://github.com/manfredjr/cronoaula/blob/main/LICENSE">Licença GPL-3.0</a> &middot;
      <a href="https://github.com/manfredjr/cronoaula/issues">Avisar um problema</a>
    </p>
  </div>
  <a class="rodape-logo" href="https://www.manfred.com.br" target="_blank" rel="noopener">
    <img src="../logo-mt.png" alt="MT - Manfred Tecnologia" width="37" height="28">
  </a>
</footer>

</body>
</html>
```

- [ ] **Passo 5: criar `public/usar/usar.css`**

A página é sempre escura, como o programa: não há versão clara. O logo do rodapé tem sempre o
fundo areia fino atrás.

```css
/* Versao web do CronoAula. Identidade do Marca.xaml: grafite de fundo, areia
   nos digitos, verde da MT so como acento. A pagina e sempre escura, como o
   programa de Windows. Sem fonte externa: tudo do sistema. */
:root {
  color-scheme: dark;
  --pagina: #151917;
  --grafite: #1e2422;
  --areia: #f5f7f2;
  --sec: #98a199;
  --botao: #333b37;
  --botao-hover: #3d4641;
  --borda: #ffffff1f;
  --borda-forte: #ffffff33;
  --campo: #242a27;
  --verde-claro: #6aaf21;
  --verde-medio: #398024;
  --verde-profundo: #022f10;
  --atencao: #e09b2d;
  --urgente: #e8752d;
  --estouro: #e86a6f;
  --digitos: 700 1em/1 Consolas, "SF Mono", ui-monospace, Menlo, "Roboto Mono", monospace;
}

* { box-sizing: border-box; }
html, body { margin: 0; }
body {
  min-height: 100vh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  background: var(--pagina);
  color: var(--areia);
  font: 16px/1.5 -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
}
button, input { font: inherit; }
.oculto {
  position: absolute; width: 1px; height: 1px; overflow: hidden;
  clip: rect(0 0 0 0); white-space: nowrap;
}

/* ---- tela normal ---- */
.palco {
  flex: 1; width: 100%; max-width: 480px; margin: 0 auto;
  padding: 20px 16px 8px;
  display: flex; flex-direction: column; justify-content: center;
}
.topo { display: flex; justify-content: flex-end; gap: 8px; margin-bottom: 10px; }
.discreto {
  background: transparent; color: var(--sec);
  border: 1px solid var(--borda-forte); border-radius: 6px;
  padding: 5px 12px; font-size: .85rem; cursor: pointer;
}
.discreto:hover { color: var(--areia); border-color: #ffffff55; }

.cartao {
  background: var(--grafite); border: 1px solid var(--borda); border-radius: 14px;
  padding: 24px 18px 18px; text-align: center;
}
.digitos {
  font: var(--digitos);
  font-size: clamp(64px, 22vw, 112px);
  font-variant-numeric: tabular-nums;
  letter-spacing: .02em;
  color: var(--areia);
  white-space: nowrap;
}
.digitos.longo { font-size: clamp(44px, 14vw, 80px); }
.estado { min-height: 1.5em; margin: 8px 0 16px; color: var(--sec); font-size: .95rem; }

.linha { display: flex; flex-wrap: wrap; justify-content: center; gap: 8px; margin: 0 0 8px; }
.botao {
  background: var(--botao); color: var(--areia);
  border: 0; border-radius: 8px; padding: 10px 16px; min-width: 48px;
  cursor: pointer;
}
.botao:hover { background: var(--botao-hover); }
.botao.principal { background: var(--verde-claro); color: var(--verde-profundo); font-weight: 600; min-width: 112px; }
.botao.principal:hover { filter: brightness(1.08); }
.botao.armado { background: var(--verde-medio); color: #fff; }
.botao.pequeno { padding: 6px 12px; font-size: .88rem; }
button:focus-visible, input:focus-visible { outline: 2px solid var(--verde-claro); outline-offset: 2px; }

#form-tempo { margin: 6px 0 0; }
#campo-tempo {
  width: 120px; background: var(--campo); color: var(--areia);
  border: 1px solid var(--borda-forte); border-radius: 8px;
  padding: 9px 12px; text-align: center;
}
.erro { color: var(--estouro); font-size: .9rem; margin: 8px 0 0; }
.atalhos { color: var(--sec); font-size: .8rem; text-align: center; margin: 14px 0 0; }
.voltar { text-align: center; font-size: .85rem; margin: 10px 0 0; }
.voltar a { color: var(--sec); }
.voltar a:hover { color: var(--areia); }

/* ---- faixas de alerta: a cor acompanha o nome escrito na linha de estado ---- */
body[data-faixa="atencao"] .digitos { color: var(--atencao); }
body[data-faixa="urgente"] .digitos { color: var(--urgente); }
body[data-faixa="estourado"] .digitos {
  color: var(--estouro);
  animation: esmaecer .9s ease-in-out infinite alternate;
}
@keyframes esmaecer { from { opacity: 1; } to { opacity: .35; } }
@media (prefers-reduced-motion: reduce) {
  body[data-faixa="estourado"] .digitos { animation: none; }
}

/* ---- tela cheia (API do navegador) e modo projecao (iPhone) ---- */
body.cheia .topo, body.cheia .presets, body.cheia .controles, body.cheia #form-tempo,
body.cheia .erro, body.cheia .atalhos, body.cheia .voltar, body.cheia .rodape,
body.projecao .topo, body.projecao .presets, body.projecao .controles, body.projecao #form-tempo,
body.projecao .erro, body.projecao .atalhos, body.projecao .voltar, body.projecao .rodape {
  display: none;
}
body.cheia .palco, body.projecao .palco { max-width: none; padding: 0 16px; }
body.cheia .cartao, body.projecao .cartao { background: transparent; border: 0; padding: 0; }
body.cheia .digitos, body.projecao .digitos { font-size: min(26vw, 58vh); }
body.cheia .digitos.longo, body.projecao .digitos.longo { font-size: min(16vw, 40vh); }
body.cheia .estado, body.projecao .estado { font-size: clamp(18px, 3.5vw, 40px); margin-top: 12px; }
body.cheia:not(.barra-visivel) { cursor: none; }

.barra {
  display: none; position: fixed; right: 16px; bottom: 16px;
  gap: 8px; flex-wrap: wrap; justify-content: flex-end;
  padding: 8px; background: #262e2be6; border-radius: 10px;
  max-width: calc(100vw - 32px);
}
body.cheia.barra-visivel .barra, body.projecao.barra-visivel .barra { display: flex; }

/* ---- painel de ajustes ---- */
dialog {
  background: var(--grafite); color: var(--areia);
  border: 1px solid var(--borda); border-radius: 14px;
  padding: 20px; width: min(420px, calc(100vw - 32px));
}
dialog::backdrop { background: #000000aa; }
.grupo { color: var(--verde-claro); font-size: 1rem; font-weight: 600; margin: 4px 0 6px; }
.grupo + .opcao { margin-top: 0; }
.opcao { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; margin: 10px 0; }
.opcao input[type="number"] {
  width: 64px; background: var(--campo); color: var(--areia);
  border: 1px solid var(--borda-forte); border-radius: 6px; padding: 4px 6px;
}
.opcao input[type="range"] { flex: 1; min-width: 120px; accent-color: var(--verde-claro); }
.opcao input[type="checkbox"] { accent-color: var(--verde-claro); width: 18px; height: 18px; margin: 0; }
.dica { color: var(--sec); font-size: .82rem; margin: -4px 0 8px; }
.acoes { display: flex; justify-content: flex-end; flex-wrap: wrap; gap: 8px; margin-top: 18px; }

/* ---- rodape: mesmo padrao da abertura e dos produtos do Escalada.dev ---- */
.rodape {
  width: 100%; margin-top: 24px; padding: 12px 16px;
  border-top: 1px solid var(--borda);
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px;
  text-align: center; font-size: 12px; line-height: 1.5; color: var(--sec);
}
.rodape-texto p { margin: 0; }
.rodape-texto p + p { margin-top: 2px; }
.rodape a { color: var(--sec); text-decoration: none; }
.rodape a:hover { text-decoration: underline; }
.rodape a.rodape-forte { color: var(--areia); font-weight: 500; }
.rodape-logo {
  order: -1; flex-shrink: 0; display: inline-flex; border-radius: 2px;
  background: var(--areia); padding: 2px;
}
.rodape-logo img { height: 28px; width: auto; display: block; }
@media (min-width: 640px) {
  .rodape { flex-direction: row; gap: 12px; text-align: right; }
  .rodape-logo { order: 0; }
  .rodape-logo img { height: 38px; }
}
```

- [ ] **Passo 6: criar `public/usar/js/tela.js`**

```js
// Liga o cronometro a pagina. E o unico modulo que toca no DOM: botoes,
// teclado, tela cheia, modo projecao, tela acesa, ajustes e o laco de desenho.

import { criarMotor } from './motor.js';
import { interpretar, formatar } from './tempo.js';
import { faixa, estadoEscrito } from './alertas.js';
import { criarSom } from './som.js';
import { carregar, salvar, normalizar, PADROES } from './ajustes.js';

const MINUTO = 60000;
const $ = (id) => document.getElementById(id);

const el = {
  digitos: $('digitos'),
  estado: $('estado'),
  iniciar: $('iniciar'),
  zerar: $('zerar'),
  maisUm: $('mais-um'),
  presets: [...document.querySelectorAll('[data-min]')],
  formTempo: $('form-tempo'),
  campo: $('campo-tempo'),
  erro: $('erro-tempo'),
  telaCheia: $('tela-cheia'),
  abrirAjustes: $('abrir-ajustes'),
  barraIniciar: $('barra-iniciar'),
  barraZerar: $('barra-zerar'),
  barraMais: $('barra-mais'),
  barraSair: $('barra-sair'),
  dialogo: $('ajustes'),
  formAjustes: $('form-ajustes'),
  volumeTexto: $('volume-texto'),
  testarAlerta: $('testar-alerta'),
  testarAviso: $('testar-aviso'),
  restaurar: $('restaurar'),
};

// Em janela anonima ou com dados bloqueados, ate ler localStorage pode lancar.
let armazenamento = null;
try {
  armazenamento = window.localStorage;
} catch {
  armazenamento = null;
}

let ajustes = carregar(armazenamento);
const toque = window.matchMedia('(pointer: coarse)').matches;
const temTelaCheia = Boolean(document.documentElement.requestFullscreen && document.fullscreenEnabled);

const motor = criarMotor({
  relogio: () => performance.now(),
  negativo: ajustes.negativo,
  avisoEm: limiarAviso(),
});
const som = criarSom();

let armado = null;        // minutos do tempo rapido carregado, esperando o segundo clique
let avisoPassou = false;  // o aviso antecipado ja tocou ou ficou para tras nesta contagem
let quadroPedido = null;
let trava = null;         // Wake Lock
let temporizadorBarra = null;
const ultimo = { digitos: '', faixa: '', estado: null, botao: '', armado: undefined };

function limiarAviso() {
  return ajustes.avisoLigado ? ajustes.avisoMinutos * MINUTO : null;
}

// ---- desenho ---------------------------------------------------------------

function desenhar() {
  const restante = motor.restante();
  const estado = motor.estado();
  const f = faixa(restante, motor.duracao());

  const texto = formatar(restante);
  if (texto !== ultimo.digitos) {
    el.digitos.textContent = texto;
    el.digitos.classList.toggle('longo', texto.replace('-', '').length > 5);
    ultimo.digitos = texto;
  }

  if (f !== ultimo.faixa) {
    document.body.dataset.faixa = f;
    ultimo.faixa = f;
  }

  const escrito = estadoEscrito({ estado, faixa: f, armado, toque });
  if (escrito !== ultimo.estado) {
    el.estado.textContent = escrito;
    ultimo.estado = escrito;
  }

  let rotulo = 'Iniciar';
  if (estado === 'contando') rotulo = 'Pausar';
  else if (estado === 'pausado') rotulo = 'Continuar';
  if (rotulo !== ultimo.botao) {
    el.iniciar.textContent = rotulo;
    el.barraIniciar.textContent = rotulo;
    ultimo.botao = rotulo;
  }

  if (armado !== ultimo.armado) {
    for (const b of el.presets) {
      const sim = Number(b.dataset.min) === armado;
      b.classList.toggle('armado', sim);
      b.setAttribute('aria-pressed', String(sim));
    }
    ultimo.armado = armado;
  }
}

// Contando e com a aba visivel, redesenha a cada quadro. Parado, nada roda.
function quadro() {
  quadroPedido = null;
  const eventos = motor.atualizar();
  if (eventos.includes('aviso')) avisoPassou = true;
  if (eventos.includes('fim') && motor.estado() !== 'contando') soltarTelaAcesa();
  desenhar();
  acordar();
}

function acordar() {
  if (quadroPedido === null && motor.estado() === 'contando' && !document.hidden)
    quadroPedido = requestAnimationFrame(quadro);
}

// ---- acoes -------------------------------------------------------------------

function reagendar() {
  if (motor.estado() !== 'contando') return;
  const restante = motor.restante();
  const limiar = limiarAviso();
  if (limiar !== null && restante <= limiar) avisoPassou = true;
  som.agendar({
    fimEmMs: restante,
    avisoEmMs: limiar === null || avisoPassou ? null : restante - limiar,
    repeticoes: ajustes.repeticoes,
    intervaloS: ajustes.intervalo,
    volume: ajustes.volume,
    ligado: ajustes.som,
  });
}

function iniciar() {
  motor.iniciar();
  armado = null;
  reagendar();
  pedirTelaAcesa();
  desenhar();
  acordar();
}

function pausar() {
  motor.pausar();
  som.cancelar();
  soltarTelaAcesa();
  desenhar();
}

function alternar() {
  if (motor.estado() === 'contando') pausar();
  else iniciar();
}

function zerar() {
  motor.zerar();
  armado = null;
  avisoPassou = false;
  som.cancelar();
  soltarTelaAcesa();
  desenhar();
}

function carregarTempo(ms, minutosRapidos = null) {
  motor.carregar(ms);
  armado = minutosRapidos;
  avisoPassou = false;
  som.cancelar();
  soltarTelaAcesa();
  ajustes = { ...ajustes, ultimoMinutos: ms / MINUTO };
  salvar(armazenamento, ajustes);
  desenhar();
}

function tempoRapido(minutos) {
  if (armado === minutos && motor.estado() === 'parado') iniciar();
  else carregarTempo(minutos * MINUTO, minutos);
}

function somar(ms) {
  // Se o aviso ja ficou para tras, somar tempo nao faz ele tocar de novo.
  const limiar = limiarAviso();
  if (motor.estado() === 'contando' && limiar !== null && motor.restante() <= limiar) avisoPassou = true;
  motor.somar(ms);
  reagendar();
  desenhar();
}

// ---- tela acesa (Wake Lock) ---------------------------------------------------

async function pedirTelaAcesa() {
  if (trava || !('wakeLock' in navigator) || document.hidden) return;
  try {
    const nova = await navigator.wakeLock.request('screen');
    if (motor.estado() !== 'contando') {
      nova.release().catch(() => {});
      return;
    }
    trava = nova;
    trava.addEventListener('release', () => { trava = null; });
  } catch {
    trava = null; // sem suporte ou negado: segue sem
  }
}

function soltarTelaAcesa() {
  if (!trava) return;
  trava.release().catch(() => {});
  trava = null;
}

document.addEventListener('visibilitychange', () => {
  if (document.hidden) return;
  desenhar();
  acordar();
  if (motor.estado() === 'contando') pedirTelaAcesa();
});

// ---- tela cheia e modo projecao ----------------------------------------------

function emTelaCheia() {
  return document.fullscreenElement != null || document.body.classList.contains('projecao');
}

function ativarProjecao(sim) {
  document.body.classList.toggle('projecao', sim);
  if (sim) mostrarBarra();
  else document.body.classList.remove('barra-visivel');
}

function entrarTelaCheia() {
  // O Safari do iPhone nao tem tela cheia fora de video: la vale o modo projecao.
  if (temTelaCheia) document.documentElement.requestFullscreen().catch(() => ativarProjecao(true));
  else ativarProjecao(true);
}

function sairTelaCheia() {
  if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
  ativarProjecao(false);
}

function alternarTelaCheia() {
  if (emTelaCheia()) sairTelaCheia();
  else entrarTelaCheia();
}

function mostrarBarra() {
  if (!emTelaCheia()) return;
  document.body.classList.add('barra-visivel');
  clearTimeout(temporizadorBarra);
  temporizadorBarra = setTimeout(() => document.body.classList.remove('barra-visivel'), 3000);
}

document.addEventListener('fullscreenchange', () => {
  const cheia = document.fullscreenElement != null;
  document.body.classList.toggle('cheia', cheia);
  if (cheia) mostrarBarra();
  else document.body.classList.remove('barra-visivel');
});

// ---- ajustes -------------------------------------------------------------------

function preencherAjustes() {
  const f = el.formAjustes.elements;
  f.som.checked = ajustes.som;
  f.volume.value = String(Math.round(ajustes.volume * 100));
  f.repeticoes.value = String(ajustes.repeticoes);
  f.intervalo.value = String(ajustes.intervalo);
  f.negativo.checked = ajustes.negativo;
  f.avisoLigado.checked = ajustes.avisoLigado;
  f.avisoMinutos.value = String(ajustes.avisoMinutos);
  el.volumeTexto.textContent = `${Math.round(ajustes.volume * 100)}%`;
}

function lerAjustes() {
  const f = el.formAjustes.elements;
  // Campo vazio ou pela metade mantem o valor atual; o normalizar traz para a faixa.
  const numero = (campo, atual) => {
    const v = Number(campo.value);
    return campo.value.trim() === '' || !Number.isFinite(v) ? atual : v;
  };
  return normalizar({
    ...ajustes,
    som: f.som.checked,
    volume: numero(f.volume, ajustes.volume * 100) / 100,
    repeticoes: numero(f.repeticoes, ajustes.repeticoes),
    intervalo: numero(f.intervalo, ajustes.intervalo),
    negativo: f.negativo.checked,
    avisoLigado: f.avisoLigado.checked,
    avisoMinutos: numero(f.avisoMinutos, ajustes.avisoMinutos),
  });
}

function aplicarAjustes(novos) {
  ajustes = novos;
  salvar(armazenamento, ajustes);
  motor.configurar({ negativo: ajustes.negativo, avisoEm: limiarAviso() });
  reagendar();
  desenhar();
  acordar();
}

el.abrirAjustes.addEventListener('click', () => {
  preencherAjustes();
  el.dialogo.showModal();
});
el.formAjustes.addEventListener('input', () => {
  aplicarAjustes(lerAjustes());
  el.volumeTexto.textContent = `${Math.round(ajustes.volume * 100)}%`;
});
// Ao sair do campo, mostra o valor ja corrigido para dentro da faixa.
el.formAjustes.addEventListener('change', preencherAjustes);
el.restaurar.addEventListener('click', () => {
  aplicarAjustes({ ...PADROES, ultimoMinutos: ajustes.ultimoMinutos });
  preencherAjustes();
});
el.testarAlerta.addEventListener('click', () => som.testarAlerta(ajustes.volume));
el.testarAviso.addEventListener('click', () => som.testarAviso(ajustes.volume));

// ---- botoes, campo e teclado ----------------------------------------------------

// Encostar na pagina ja silencia o alerta de fim, como no programa: se o
// professor mexeu, e porque ja percebeu. Tambem libera o audio no primeiro gesto.
document.addEventListener('pointerdown', () => {
  som.liberar();
  som.silenciar();
  mostrarBarra();
}, true);
document.addEventListener('pointermove', mostrarBarra);

el.iniciar.addEventListener('click', alternar);
el.barraIniciar.addEventListener('click', alternar);
el.zerar.addEventListener('click', zerar);
el.barraZerar.addEventListener('click', zerar);
el.maisUm.addEventListener('click', () => somar(MINUTO));
el.barraMais.addEventListener('click', () => somar(MINUTO));
el.barraSair.addEventListener('click', sairTelaCheia);
el.telaCheia.addEventListener('click', alternarTelaCheia);
for (const b of el.presets)
  b.addEventListener('click', () => tempoRapido(Number(b.dataset.min)));

el.formTempo.addEventListener('submit', (e) => {
  e.preventDefault();
  const ms = interpretar(el.campo.value);
  if (ms === null) {
    el.erro.hidden = false;
    return;
  }
  el.erro.hidden = true;
  carregarTempo(ms);
  el.campo.blur(); // devolve o Espaco e as setas ao cronometro
});
el.campo.addEventListener('input', () => { el.erro.hidden = true; });

document.addEventListener('keydown', (e) => {
  som.liberar();
  som.silenciar();
  if (el.dialogo.open || e.ctrlKey || e.metaKey || e.altKey) return;
  if (e.target === el.campo) return;

  switch (e.key) {
    case ' ':
      // Espaco e sempre iniciar e pausar, mesmo com o foco num botao.
      e.preventDefault();
      if (!e.repeat) alternar();
      break;
    case 'r':
    case 'R':
      zerar();
      break;
    case 'ArrowUp':
      e.preventDefault();
      somar(MINUTO);
      break;
    case 'ArrowDown':
      e.preventDefault();
      somar(-MINUTO);
      break;
    case 'f':
    case 'F':
      alternarTelaCheia();
      break;
    case 'Escape':
      sairTelaCheia();
      break;
    default:
      break;
  }
});

// Sem isto, soltar o Espaco com o foco num botao tambem clicaria nele.
document.addEventListener('keyup', (e) => {
  if (e.key === ' ' && e.target instanceof HTMLButtonElement && !el.dialogo.open) e.preventDefault();
});

// ---- partida -------------------------------------------------------------------

if (toque) el.telaCheia.textContent = 'Tela cheia';
motor.carregar(ajustes.ultimoMinutos * MINUTO);
desenhar();
```

- [ ] **Passo 7: rodar os testes do site e os da versão web**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~SiteTests"`
Esperado: todos aprovados.

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 50 testes, 0 falhas (o `tela.js` não é importado pelos testes).

- [ ] **Passo 8: conferência rápida no navegador**

Iniciar o servidor local com `preview_start` e o nome `site-cronoaula` (já configurado em
`.claude/launch.json`, porta 8765) e abrir `http://127.0.0.1:8765/usar/`. Conferir no console
que não há erro de módulo, clicar em 5 duas vezes e ver a contagem começar. A conferência
completa fica na tarefa 9.

- [ ] **Passo 9: commit**

```bash
git add public/usar CronoAula.Tests/SiteTests.cs
git commit -m "Versao web: pagina usar/, com testes de referencias, sons e rodape"
```

---

### Tarefa 7: abertura do site com os dois caminhos

**Arquivos:**
- Alterar: `public/index.html` (bloco `.cta` e `.fineprint` do cabeçalho, linhas 169 a 177; estilo `.btn-alt`, linha 92; meta description, linha 7)
- Alterar: `CronoAula.Tests/SiteTests.cs`

**Interfaces:**
- Consome: a página `public/usar/index.html` (tarefa 6).
- Produz: link `href="usar/"` na abertura.

- [ ] **Passo 1: escrever o teste**

Acrescentar em `CronoAula.Tests/SiteTests.cs`, no fim da classe:

```csharp
    [Fact]
    public void Abertura_OfereceOsDoisCaminhos()
    {
        // Opcao B da maquete: navegador em destaque (botao cheio), Windows vazado.
        var html = File.ReadAllText(Path.Combine(PastaDoSite(), "index.html"));

        Assert.Matches("<a class=\"btn btn-main\" href=\"usar/\">\\s*Usar no navegador\\s*</a>", html);
        Assert.Matches("<a class=\"btn btn-alt\" href=\"https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe\">\\s*Baixar para Windows\\s*</a>", html);
        Assert.Contains("fica por cima do PowerPoint", html);
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~Abertura_OfereceOsDoisCaminhos"`
Esperado: FALHA no primeiro `Assert.Matches`.

- [ ] **Passo 3: trocar os botões da abertura**

Em `public/index.html`, substituir o bloco atual:

```html
  <div class="cta">
    <a class="btn btn-main" href="https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe">
      Baixar o CronoAula
    </a>
    <a class="btn btn-alt" href="https://github.com/manfredjr/cronoaula">Ver o projeto no GitHub</a>
  </div>
  <p class="fineprint">
    Gratuito e de código aberto. Windows 10 ou 11, 64 bits. Arquivo único, sem instalador.
  </p>
```

por:

```html
  <div class="cta">
    <a class="btn btn-main" href="usar/">Usar no navegador</a>
    <a class="btn btn-alt" href="https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe">Baixar para Windows</a>
  </div>
  <p class="fineprint">
    No navegador, dá para projetar para a turma e usar sem instalar nada.<br>
    Para Windows, o programa fica por cima do PowerPoint. Windows 10 ou 11, 64 bits, arquivo único.
  </p>
```

O link para o GitHub continua no rodapé ("Código-fonte").

- [ ] **Passo 4: deixar o botão de Windows vazado**

Em `public/index.html`, trocar a linha:

```css
  .btn-alt { border-color: var(--border); color: var(--ink); background: var(--surface); }
```

por:

```css
  .btn-alt { border: 1.5px solid var(--accent); color: var(--accent); background: transparent; }
```

- [ ] **Passo 5: atualizar a descrição da página**

Em `public/index.html`, trocar a linha 7:

```html
<meta name="description" content="Cronômetro regressivo que fica visível por cima do PowerPoint e do ambiente de programação. Gratuito, sem instalador, feito para professores. Software livre.">
```

por:

```html
<meta name="description" content="Cronômetro regressivo para aulas. Use no navegador, para projetar ou no celular, ou baixe para Windows e deixe por cima do PowerPoint. Gratuito, feito para professores. Software livre.">
```

- [ ] **Passo 6: rodar os testes do site**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~SiteTests"`
Esperado: todos aprovados, inclusive `Site_BaixaOExecutavelDoGitHub`.

- [ ] **Passo 7: commit**

```bash
git add public/index.html CronoAula.Tests/SiteTests.cs
git commit -m "Abertura do site: usar no navegador em destaque, baixar para Windows vazado"
```

---

### Tarefa 8: build, deploy e documentação

**Arquivos:**
- Alterar: `build.ps1` (bloco `# --- Testes ---`)
- Alterar: `.cpanel.yml`
- Alterar: `CronoAula.Tests/SiteTests.cs`
- Alterar: `CONTRIBUTING.md` (seções "O que você precisa", "Preparar o ambiente", "O fluxo")
- Alterar: `README.md` (topo e "Privacidade")
- Alterar: `PUBLICAR-SITE.md` ("Publicar uma alteração do site")
- Alterar: `projeto/specs/2026-09-22-versao-web-design.md` (seção 9 e 11)

**Interfaces:**
- Consome: `node --test "web-testes/*.test.js"` (tarefas 1 a 5).
- Produz: `build.ps1` que exige os dois conjuntos de testes; `.cpanel.yml` que confere `public/usar/index.html`.

- [ ] **Passo 1: escrever o teste do `.cpanel.yml`**

Acrescentar em `CronoAula.Tests/SiteTests.cs`, no fim da classe:

```csharp
    [Fact]
    public void Deploy_ConfereAVersaoWeb()
    {
        // O deploy do cPanel para antes de publicar se a pagina da versao web sumir.
        var raiz = Path.GetDirectoryName(PastaDoSite())!;
        var yml = File.ReadAllText(Path.Combine(raiz, ".cpanel.yml"));

        Assert.Contains("test -f $REPO/public/usar/index.html", yml);
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~Deploy_ConfereAVersaoWeb"`
Esperado: FALHA no `Assert.Contains`.

- [ ] **Passo 3: acrescentar a conferência no `.cpanel.yml`**

Em `.cpanel.yml`, logo depois da linha `- test -f $REPO/public/favicon.ico`, acrescentar com a
mesma indentação (12 espaços antes do hífen):

```yaml
            - test -f $REPO/public/usar/index.html
```

Validar o YAML com o validador que já existe na pasta de rascunho da sessão:

Rodar: `PYTHONPATH="$SCRATCH/pyyaml" py "$SCRATCH/validar_cpanel.py"`, com
`SCRATCH=/c/Users/Usuario/AppData/Local/Temp/claude/C--COWORK-CODE-CRONOAULA/ac657e9b-1fef-4aa1-afc2-59a918083c52/scratchpad`.
Se o validador não estiver lá (outra sessão), conferir que as linhas `- test -f` têm todas a
mesma indentação e seguir: o teste do passo 4 e o deploy do cPanel pegam o resto.

- [ ] **Passo 4: rodar o teste**

Rodar: `dotnet test --nologo --verbosity quiet --filter "FullyQualifiedName~Deploy_ConfereAVersaoWeb"`
Esperado: aprovado.

- [ ] **Passo 5: fazer o `build.ps1` rodar os testes da versão web**

Em `build.ps1`, substituir o bloco inteiro de `# --- Testes ---` até o `}` do `else` por:

```powershell
# --- Testes ------------------------------------------------------------------
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "Executando os testes do programa..." -ForegroundColor Cyan
    dotnet test --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "ERRO: os testes falharam. O executavel NAO foi gerado." -ForegroundColor Red
        exit 1
    }
    Write-Host "Testes do programa aprovados." -ForegroundColor Green

    # A versao web vive no mesmo repositorio e segue as mesmas regras. Sem Node,
    # o script para em vez de pular calado.
    if (-not (Get-Command node -CommandType Application -ErrorAction SilentlyContinue)) {
        Write-Host ""
        Write-Host "ERRO: o Node.js nao foi encontrado, e ele roda os testes da versao web." -ForegroundColor Red
        Write-Host "Instale o Node.js LTS em https://nodejs.org" -ForegroundColor Yellow
        Write-Host "Para gerar so o executavel, sem nenhum teste: .\build.ps1 -SkipTests" -ForegroundColor Yellow
        exit 1
    }

    Write-Host ""
    Write-Host "Executando os testes da versao web..." -ForegroundColor Cyan
    node --test "web-testes/*.test.js"
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "ERRO: os testes da versao web falharam. O executavel NAO foi gerado." -ForegroundColor Red
        exit 1
    }
    Write-Host "Testes da versao web aprovados." -ForegroundColor Green
}
else {
    Write-Host "Testes ignorados (-SkipTests), os do programa e os da versao web." -ForegroundColor Yellow
}
```

Atualizar também o comentário do parâmetro, logo acima de `[switch]$SkipTests`:

```powershell
    # Pula todos os testes, os do programa e os da versao web (nao recomendado).
```

- [ ] **Passo 6: conferir o `build.ps1`**

Rodar (PowerShell): `.\build.ps1`
Esperado: "Testes do programa aprovados.", "Testes da versao web aprovados." e o executável em
`dist\`. Depois, rodar `git status` e conferir que `dist/` não aparece (está no `.gitignore`).

- [ ] **Passo 7: `CONTRIBUTING.md`**

Na seção "O que você precisa", acrescentar depois da linha do .NET 8 SDK:

```markdown
- [Node.js](https://nodejs.org) LTS, só para rodar os testes da versão web. O Node não vai para
  o servidor: o site é todo estático.
```

Na seção "Preparar o ambiente", depois do bloco com `dotnet test`, acrescentar:

````markdown
```bash
node --test "web-testes/*.test.js"
```
````

E trocar o parágrafo "Os 107 testes devem passar antes de você começar a mexer em qualquer
coisa." por:

```markdown
Os dois conjuntos de testes devem passar antes de você começar a mexer em qualquer coisa.
```

Na seção "O fluxo", trocar o item 4 por:

```markdown
4. Garanta que `dotnet test` e `node --test "web-testes/*.test.js"` continuam passando.
   Mudou uma regra do cronômetro? Mude nos dois lados e em `casos-compartilhados.json`.
```

- [ ] **Passo 8: `README.md`**

Logo depois do parágrafo que termina em "use a página de [Releases](...)." (antes do `---`),
acrescentar:

```markdown
### **[Usar no navegador](https://cronoaula.escalada.dev/usar/)**

Sem instalar nada. Serve para projetar o tempo para a turma em tela cheia, para computadores
que não deixam instalar programas e para usar no celular ou tablet. O que ela não faz: ficar
por cima do PowerPoint e receber atalhos com outro programa em foco. Para isso, use o programa
de Windows.

No Safari, principalmente no iPhone com a tela bloqueada, o navegador pode suspender o som da
página. Enquanto conta, a página pede para a tela não apagar, o que evita o caso na maioria
das vezes.
```

Na seção "Privacidade", acrescentar no fim:

```markdown
A versão no navegador segue a mesma regra: não busca nada fora de `cronoaula.escalada.dev`, e
as preferências ficam só no navegador de quem usa.
```

- [ ] **Passo 9: `PUBLICAR-SITE.md`**

Na seção "Publicar uma alteração do site", trocar o item 4 por:

```markdown
4. **Purgar o cache da Cloudflare** (Caching, Configuration, Purge Everything) e conferir. A
   página HTML chega sem cache, mas `.js`, `.css` e `.wav` da versão web podem ficar guardados
   na borda, e o navegador receberia o motor antigo com a página nova.
5. Abrir `https://cronoaula.escalada.dev/usar/`, carregar um tempo rápido e conferir que conta.
```

- [ ] **Passo 10: corrigir o comando na especificação**

Em `projeto/specs/2026-09-22-versao-web-design.md`, trocar as duas ocorrências de
`node --test web-testes/` por `node --test "web-testes/*.test.js"`. Na seção 9, depois da frase
"O Node não vai para o servidor.", acrescentar: "O padrão entre aspas é necessário: o Node 24
não aceita a pasta sozinha."

- [ ] **Passo 11: rodar tudo**

Rodar: `dotnet test --nologo --verbosity quiet`
Esperado: todos aprovados (164 do programa mais os novos).

Rodar: `node --test "web-testes/*.test.js"`
Esperado: 50 testes, 0 falhas.

- [ ] **Passo 12: commit**

```bash
git add build.ps1 .cpanel.yml CronoAula.Tests/SiteTests.cs CONTRIBUTING.md README.md PUBLICAR-SITE.md projeto/specs/2026-09-22-versao-web-design.md
git commit -m "Build roda os testes da versao web; deploy e documentacao da versao web"
```

---

### Tarefa 9: conferência no navegador

**Arquivos:** nenhum, salvo correções que a conferência encontrar (cada correção com seu commit).

**Interfaces:**
- Consome: tudo das tarefas 1 a 8.
- Produz: a lista de conferência preenchida, para o relatório final ao Manfred.

- [ ] **Passo 1: abrir a página local**

`preview_start` com o nome `site-cronoaula` e navegar até `http://127.0.0.1:8765/usar/`.
Conferir com `read_console_messages` (só erros) que não há nenhum.

- [ ] **Passo 2: comportamento no computador**

Conferir, um item por vez, e anotar o resultado:

1. Ao abrir: `50:00` e "pronto" (ou o último tempo usado).
2. Clicar em 5: `05:00`, botão 5 em verde médio, "5 min carregado. Clique de novo para iniciar".
3. Clicar em 5 de novo: começa a contar, botão principal vira "Pausar", linha de estado vazia.
4. Espaço pausa ("pausado", botão "Continuar"); Espaço de novo continua.
5. Seta para cima soma 1 minuto; seta para baixo tira.
6. R zera: volta a `05:00`, parado, "pronto".
7. Digitar `0:05` e Enter: `00:05`, campo perde o foco. Espaço inicia; em 5 s aparece
   "tempo excedido", dígitos em `#E86A6F` piscando, e a contagem segue em `-00:01`, `-00:02`.
8. Digitar `abc` e Enter: aparece a mensagem de erro, sem janela de alerta; digitar some com ela.
9. Reta final e último minuto: a reta final só aparece em durações acima de 5 minutos (abaixo
   disso, 20% da duração fica menor que o último minuto). **No começo da conferência**, abrir
   uma segunda aba (`tabs_create`) em `/usar/`, carregar `6` e iniciar. Seguir os outros itens
   na primeira aba e voltar à segunda: em 01:12 entra "reta final" em âmbar `#E09B2D`; em 01:00,
   "último minuto" em laranja `#E8752D`. Os limites exatos já estão cobertos pelos casos
   compartilhados; aqui se confere a cor e o nome na tela.
10. Ajustes: abrir, desligar "Continuar contando em negativo", fechar com Esc; carregar `0:03`,
    iniciar: para em `00:00`, "tempo excedido", botão "Iniciar".
11. Ajustes sobrevivem a recarregar a página (F5).
12. `read_network_requests`: só pedidos para `127.0.0.1:8765`.

- [ ] **Passo 3: som**

Com o volume do computador ligado, pedir ao Manfred para ouvir, porque o navegador interno pode
não ter saída de áudio: "Testar alerta", "Testar aviso", e um `0:05` até o fim (5 toques a cada
3 s). Um clique durante o alerta silencia; um clique antes do zero não cancela o alerta que
vai tocar. Por fim, o critério da aba em segundo plano: carregar `7`, iniciar, trocar para outra
aba por mais de 5 minutos e conferir que o alerta toca no instante do zero, não até um minuto
depois. Registrar o resultado que o Manfred informar.

- [ ] **Passo 4: tela cheia**

Tecla F: só dígitos e linha de estado, grandes. Mover o mouse: a barra aparece no canto
inferior direito e some em 3 s. Esc sai. Se o navegador interno recusar a tela cheia, a página
cai no modo projeção: conferir que o visual é o mesmo e que Esc também sai.

- [ ] **Passo 5: celular**

`resize_window` com `preset: "mobile"`, recarregar. Conferir: sem rolagem lateral
(`document.documentElement.scrollWidth <= 375` via `javascript_tool`), linha de estado com
"Toque de novo para iniciar", botão "Tela cheia" sem "(F)", rodapé empilhado com o logo em
cima. Voltar com `preset: "desktop"`.

- [ ] **Passo 6: modo escuro e movimento reduzido**

`resize_window` com `colorScheme: "light"` e depois `"dark"`: a página é igual nos dois (é
sempre escura) e o logo do rodapé tem o fundo areia. Conferir no CSS que a regra de
`prefers-reduced-motion` desliga o piscar (inspecionar `getComputedStyle(digitos).animationName`
com a mídia emulada, se o navegador permitir; senão, registrar como conferido por leitura).

- [ ] **Passo 7: abertura**

Navegar até `http://127.0.0.1:8765/`. "Usar no navegador" cheio, "Baixar para Windows" vazado,
a linha da diferença embaixo. Clicar em "Usar no navegador" leva a `/usar/`. Conferir em 375 px
também.

- [ ] **Passo 8: rodar tudo mais uma vez e relatar**

Rodar: `dotnet test --nologo --verbosity quiet` e `node --test "web-testes/*.test.js"`.
Esperado: tudo aprovado.

Relatar ao Manfred o resultado de cada item dos passos 2 a 7, com o que falhou e foi corrigido.
**Não abrir PR nem fazer merge sem ele pedir.** Depois do merge autorizado, o deploy segue o
`PUBLICAR-SITE.md`, e a conferência em produção inclui `read_network_requests` na página publicada
para pegar script injetado pela Cloudflare (risco registrado na seção 12 da especificação).
