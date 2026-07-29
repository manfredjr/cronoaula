# Como contribuir com o CronoAula

O CronoAula nasceu de uma necessidade real de sala de aula. Se você é professor
e usa o programa, sua opinião vale tanto quanto código: relatar o que atrapalha
já é contribuir.

Não é preciso ser programador para ajudar.

---

## Sem escrever código

**Relatar um problema.** Abra uma *issue* em
[Issues](../../issues) descrevendo o que aconteceu. Ajuda muito informar:

- versão do Windows (10 ou 11);
- o que você estava fazendo quando o problema apareceu;
- o que esperava que acontecesse;
- se dá para repetir o problema e como.

**Sugerir uma melhoria.** Também pelas *issues*. Explique o cenário de aula em
que a mudança faria diferença — o contexto pedagógico é mais útil que a
descrição técnica da solução.

**Contar como você usa.** Cada disciplina organiza o tempo de um jeito. Saber
como o cronômetro é usado em outras aulas orienta as próximas versões.

---

## Escrevendo código

### O que você precisa

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10 ou 11, 64 bits (o projeto usa WPF e APIs do Windows)
- Um editor: Visual Studio 2022, Rider ou VS Code

### Preparar o ambiente

```bash
git clone https://github.com/manfredjr/cronoaula.git
```

```bash
dotnet build
```

```bash
dotnet test
```

Os 107 testes devem passar antes de você começar a mexer em qualquer coisa. Se
algum falhar em uma cópia limpa, isso já é uma issue.

### Gerar o executável

```bash
.\build.ps1
```

O script roda os testes e recusa publicar se algum falhar. O resultado sai em
`.\dist\CronoAula.exe`.

### O fluxo

1. Faça um *fork* do repositório.
2. Crie um branch com nome descritivo: `git checkout -b som-personalizado`.
3. Faça as alterações, com commits pequenos e mensagens claras.
4. Garanta que `dotnet test` continua passando.
5. Abra um *pull request* explicando **o problema de sala de aula** que a
   mudança resolve, não apenas o que o código faz.

### Regras da casa

**Testes acompanham a lógica.** Tudo em `Core/` é testável sem interface
gráfica, e é assim que deve continuar. Mudou `TimerEngine`, `TimeParser`,
`AppSettings` ou `SoundService`? Traga o teste junto.

**Sem dependências externas.** O programa é um arquivo único, autossuficiente,
sem instalador e sem runtime na máquina. Um pacote NuGet novo precisa de uma
justificativa forte — na prática, quase nunca vale a pena.

**Sem rede e sem telemetria.** O CronoAula não acessa a internet e não coleta
nada. Isso não é detalhe de implementação, é uma promessa feita ao usuário.
Contribuições que quebrem isso não serão aceitas.

**Nada de privilégio de administrador.** O programa roda em máquina de
laboratório e computador de escola, onde o professor muitas vezes não é admin.

**A janela não pode roubar o foco.** Qualquer mexida no comportamento de
"ficar por cima" precisa preservar a flag `SWP_NOACTIVATE`. Sem ela, a passagem
de slides pelo teclado trava no meio da aula.

**Português nos textos de interface.** As mensagens visíveis ao usuário são em
português do Brasil. Comentários e nomes de código também estão em português —
mantenha o padrão do arquivo que você está editando.

### Antes de abrir um PR grande

Se a ideia for grande (um recurso novo, uma mudança de arquitetura), abra uma
issue antes para conversarmos. Evita você investir tempo em algo que não se
encaixa no rumo do projeto.

Há uma lista do que está fora do escopo desta versão na seção
"Ainda não implementado" do [README](README.md) — são bons pontos de partida.

---

## Licença das contribuições

O CronoAula é distribuído sob a
[GNU General Public License v3.0](LICENSE). Ao enviar um pull request, você
concorda que sua contribuição seja licenciada nos mesmos termos.

Na prática: o código é livre para qualquer um usar, estudar e modificar, e quem
distribuir uma versão modificada precisa também abrir o código dela. As
melhorias voltam para a comunidade de professores.
