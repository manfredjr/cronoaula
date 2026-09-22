# Publicar o site no cPanel

O CronoAula tem duas publicações, e elas não se misturam:

| O quê | Para onde | Como |
|---|---|---|
| O programa (`CronoAula.exe`) | GitHub Releases | `.\publicar.ps1 -Versao X.Y.Z` |
| O site (pasta `docs/`) | cronoaula.escalada.dev, no cPanel da GoDaddy | Git Version Control do cPanel |

Este documento trata do site. Ele segue o roteiro padrão da conta
(`roteiro-publicacao-git-cpanel.md`), com as diferenças abaixo. Leia o roteiro
antes; as regras da seção 2 dele valem aqui.

## Como fica neste projeto

- O cPanel clona este próprio repositório em `~/repositories/cronoaula`. Não há
  repositório de publicação: o site é estático, versionado e vem de um lugar só.
- A raiz do subdomínio é `repositories/cronoaula/docs`. O código C# fica fora da
  raiz e não é servido.
- O clone é por **HTTPS**, sem chave: o repositório é público. Os passos 5.1 a
  5.6 do roteiro não se aplicam, e nada em `~/.ssh` é tocado. Se o repositório
  ficar privado, passa a valer o caminho da chave do roteiro.
- Não há PHP, banco, `.env` nem tarefa agendada.

**Tudo que entra em `docs/` fica público.** Nos outros projetos, `docs/` guarda
documentação interna; aqui é a raiz do site. O teste `SiteTests` recusa o que não
for arquivo de site ali dentro. Anotação e documentação vão na raiz do repositório.

## Primeira vez

Com o Pull Request que traz o `.cpanel.yml` já juntado ao `main`.

**Ordem importa.** O clone vem antes do subdomínio. Se o subdomínio for criado
primeiro, o cPanel cria a pasta `docs` dentro de `repositories/cronoaula`, e o
Git Version Control recusa clonar em pasta que não está vazia.

**1. Conferir que a pasta não existe** (Terminal do cPanel, colar o resultado):

```bash
if [ -e ~/repositories/cronoaula ]; then ls -la ~/repositories/cronoaula; else echo "nao existe: ~/repositories/cronoaula"; fi; ls ~/repositories
```

**2. Criar o clone** em Git Version Control, botão Criar:

| Campo | Valor |
|---|---|
| Clone a Repository | ligado |
| Clone URL | `https://github.com/manfredjr/cronoaula.git` |
| Repository Path | `repositories/cronoaula` |
| Repository Name | `cronoaula` |

**3. Abrir a pasta para o Apache** (colar o resultado, deve mostrar `755`):

```bash
chmod 755 ~/repositories/cronoaula && stat -c '%a %n' ~/repositories/cronoaula ~/repositories/cronoaula/docs/index.html
```

**4. Criar o subdomínio** em Domínios, botão Criar um novo domínio:

| Campo | Valor |
|---|---|
| Domínio | `cronoaula.escalada.dev` |
| Compartilhar raiz com o domínio principal | **desligado** |
| Raiz do documento | `repositories/cronoaula/docs` |

**5. Certificado.** Em Status SSL/TLS, rodar o AutoSSL e conferir que
`cronoaula.escalada.dev` ganhou certificado. Sem ele, a Cloudflare responde
**erro 526** (foi o que o endereço respondia antes desta publicação).

**6. Primeiro deploy.** Em Git Version Control, Gerenciar, aba Pull or Deploy:
Update from Remote, F5, Deploy HEAD Commit. Acompanhar:

```bash
tail -40 "$(ls -t ~/.cpanel/logs/vc_*deploy*.log | head -1)"
```

**7. Conferir.** Purgar o cache da Cloudflare e abrir
`https://cronoaula.escalada.dev/`. Conferir que o botão de download baixa o
executável do GitHub.

## Aposentar o endereço antigo

O site ficou até esta publicação no GitHub Pages, em `cronoaula.manfred.com.br`.
Depois que o endereço novo estiver conferido, nesta ordem:

1. **Redirecionar na Cloudflare**, na zona `manfred.com.br`: regra de
   redirecionamento de `cronoaula.manfred.com.br` para
   `https://cronoaula.escalada.dev`, código 301, preservando o caminho. O nome
   já passa pela Cloudflare, então a regra vale antes de a requisição chegar ao
   GitHub.
2. **Conferir** que o endereço antigo cai no novo.
3. **Desligar o GitHub Pages** do repositório.
4. **Tirar o `docs/CNAME`** num Pull Request. Ele só existe para o Pages.

Redirecionar antes de desligar evita que o endereço antigo passe por um período
respondendo erro.

## Publicar uma alteração do site

1. Alteração em `docs/` entra por Pull Request, e o merge só com autorização.
2. cPanel, Git Version Control, Gerenciar, aba Pull or Deploy.
3. Update from Remote, F5, Deploy HEAD Commit.
4. Purgar o cache da Cloudflare e conferir.

Uma versão nova do programa não exige deploy do site: o link de download aponta
sempre para a release mais recente.

## Diagnóstico

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| Erro 526 da Cloudflare | subdomínio sem certificado na GoDaddy | passo 5 |
| Erro 403 ou página em branco | pasta do clone com `700` | passo 3 |
| "directory already contains files" ao clonar | subdomínio criado antes do clone | desfazer o subdomínio, apagar a pasta vazia, voltar ao passo 1 |
| "The system cannot deploy" | página desatualizada, ou `error_log` solto no clone | F5; `git status --short` no clone |
| Site com versão antiga | cache da Cloudflare | purgar e conferir de novo |
