# Publicar o site no cPanel

O CronoAula tem duas publicações, e elas não se misturam:

| O quê | Para onde | Como |
|---|---|---|
| O programa (`CronoAula.exe`) | GitHub Releases | `.\publicar.ps1 -Versao X.Y.Z` |
| O site (pasta `public/`) | cronoaula.escalada.dev, no cPanel da GoDaddy | Git Version Control do cPanel |

Este documento trata do site. Ele segue o roteiro padrão da conta
(`roteiro-publicacao-git-cpanel.md`), com as diferenças abaixo. Leia o roteiro
antes; as regras da seção 2 dele valem aqui.

## Como fica neste projeto

- O cPanel clona este próprio repositório em `~/repositories/cronoaula`. Não há
  repositório de publicação: o site é estático, versionado e vem de um lugar só.
- A raiz do subdomínio é `repositories/cronoaula/public`, como nos demais
  projetos da conta. O código C# fica fora da raiz e não é servido.
- O clone é por **HTTPS**, sem chave: o repositório é público. Os passos 5.1 a
  5.6 do roteiro não se aplicam, e nada em `~/.ssh` é tocado. Se o repositório
  ficar privado, passa a valer o caminho da chave do roteiro.
- Não há PHP, banco, `.env` nem tarefa agendada.

Tudo que entra em `public/` fica público. O teste `SiteTests` recusa o que não
for arquivo de site ali dentro. Anotação e documentação vão na raiz do
repositório.

## Primeira vez

Com o Pull Request que traz o `.cpanel.yml` já juntado ao `main`.

**Ordem importa.** O clone vem antes do subdomínio. Se o subdomínio for criado
primeiro, o cPanel cria a pasta `public` dentro de `repositories/cronoaula`, e o
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
chmod 755 ~/repositories/cronoaula && stat -c '%a %n' ~/repositories/cronoaula ~/repositories/cronoaula/public/index.html
```

**4. Criar o subdomínio** em Domínios, botão Criar um novo domínio:

| Campo | Valor |
|---|---|
| Domínio | `cronoaula.escalada.dev` |
| Compartilhar raiz com o domínio principal | **desligado** (vem ligado por padrão) |
| Raiz do documento | `repositories/cronoaula/public` |

**5. Certificado.** Em Status SSL/TLS, rodar o AutoSSL e conferir que
`cronoaula.escalada.dev` ganhou certificado. Sem ele, a Cloudflare responde
**erro 526**. Se o AutoSSL falhar para esse nome, deixar o registro `cronoaula`
como "DNS only" na Cloudflare, rodar de novo e voltar para o modo com proxy.

**6. Primeiro deploy.** Em Git Version Control, Gerenciar, aba Pull or Deploy:
Update from Remote, F5, Deploy HEAD Commit. Acompanhar:

```bash
tail -40 "$(ls -t ~/.cpanel/logs/vc_*deploy*.log | head -1)"
```

**7. Conferir.** Purgar o cache da Cloudflare e abrir
`https://cronoaula.escalada.dev/`. Conferir que o botão de download baixa o
executável do GitHub.

## O endereço antigo

Até a mudança para `public/`, o site era servido pelo GitHub Pages em
`cronoaula.manfred.com.br`, a partir da pasta `docs/`. O Pages só serve da raiz
do repositório ou de `docs/`, então ele parou de funcionar quando a pasta foi
renomeada. Para quem ainda tiver o link antigo:

1. **Redirecionar na Cloudflare**, na zona `manfred.com.br`: regra de
   redirecionamento de `cronoaula.manfred.com.br` para
   `https://cronoaula.escalada.dev`, código 301, preservando o caminho. O nome
   passa pela Cloudflare, então a regra vale antes de a requisição chegar ao
   GitHub.
2. **Conferir** que o endereço antigo cai no novo.
3. **Desligar o GitHub Pages** do repositório, para ele não seguir tentando
   publicar uma pasta que não existe mais.

## Publicar uma alteração do site

1. Alteração em `public/` entra por Pull Request, e o merge só com autorização.
2. cPanel, Git Version Control, Gerenciar, aba Pull or Deploy.
3. Update from Remote, F5, Deploy HEAD Commit.
4. **Purgar o cache da Cloudflare** (Caching, Configuration, Purge Everything) e conferir. A
   página HTML chega sem cache, mas `.js`, `.css` e `.wav` da versão web podem ficar guardados
   na borda, e o navegador receberia o motor antigo com a página nova.
5. Abrir `https://cronoaula.escalada.dev/usar/`, carregar um tempo rápido e conferir que conta.

Uma versão nova do programa não exige deploy do site: o link de download aponta
sempre para a release mais recente.

## Diagnóstico

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| Erro 526 da Cloudflare | subdomínio sem certificado na GoDaddy | passo 5 |
| Página "Coming Soon" direto na GoDaddy | subdomínio não criado no cPanel | passo 4 |
| Erro 403 ou página em branco | pasta do clone com `700`, ou raiz do subdomínio errada | passo 3; conferir a raiz em Domínios |
| "directory already contains files" ao clonar | subdomínio criado antes do clone | desfazer o subdomínio, apagar a pasta vazia, voltar ao passo 1 |
| "untracked working tree files would be overwritten" no Update | o cPanel criou arquivo dentro de `public/` | listar `public/`, mover o que sobrou para fora e repetir o Update |
| "The system cannot deploy" | página desatualizada, ou `error_log` solto no clone | F5; `git status --short` no clone |
| Site com versão antiga | cache da Cloudflare | purgar e conferir de novo |
