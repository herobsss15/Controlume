# Controlume
Sistema de PDV para loja física (discos, CDs, DVDs, eletrônicos e avulsos) — cadastro de produtos, controle de estoque, preços, vendas com pagamento em dinheiro/cartão/Pix (inclusive dividido), sangria e fechamento de caixa. ASP.NET Core (Blazor Server) + PostgreSQL.

## Acesso e papéis

Toda rota exige usuário autenticado; sem cookie válido o sistema redireciona para `/login`. O papel do usuário decide o que aparece e o que pode ser executado — e a checagem vale tanto na tela quanto na camada de serviço, então esconder o botão não é a única barreira.

| Papel | Venda, caixa e sangria | Cadastro de produto e tipo de produto | Históricos |
|---|---|---|---|
| `Admin` | sim | sim | sim |
| `Operador` | sim | não acessa as telas | sim |
| `Stakeholder` | só leitura | só leitura | sim |

Não existe tela de cadastro de usuário: os usuários vêm da configuração `Usuarios:Seed` e são criados (ou têm a senha atualizada) no startup. Em produção isso são variáveis de ambiente — veja [.env.example](.env.example). Para resetar uma senha, troque a variável e suba de novo. Em Development, os usuários de teste estão em [appsettings.Development.json](src/Controlume.Web/appsettings.Development.json) (`admin`/`admin`, `operador`/`operador`, `stakeholder`/`stakeholder`).

Não há recuperação de senha por e-mail — a loja é pequena e o reset é manual, pela configuração.

## Caixa, sangria e saldo em dinheiro

- **Sangria** é uma retirada de dinheiro da gaveta, com motivo obrigatório (`Pagamento` ou `Compra`) e descrição opcional. Só pode ser registrada com caixa aberto e nunca pode deixar o saldo em dinheiro negativo — o limite é o saldo do momento (inicial + vendas em dinheiro − sangrias já feitas), não o valor inicial.
- O **saldo em dinheiro** aparece ao vivo enquanto o caixa está aberto. Cartão e Pix não entram nessa conta: não ocupam a gaveta.
- No fechamento, esse saldo é congelado em `SaldoFinal`.
- Ao abrir o próximo caixa, o `ValorInicial` já vem preenchido com o `SaldoFinal` do último fechamento. É sugestão: a contagem física manda, e o campo continua editável.

## Deploy em produção (Docker Compose)

Pensado para um home server atrás de Cloudflare Tunnel: o Kestrel só escuta HTTP na porta 8080 (sem certificado na aplicação), e o Cloudflare cuida do TLS na borda.

1. Copie `.env.example` para `.env`, defina uma `POSTGRES_PASSWORD` forte e uma `ADMIN_SENHA` (ambas são obrigatórias; as demais variáveis têm defaults razoáveis).
2. Suba tudo:

   ```bash
   docker compose up -d --build
   ```

Isso builda a imagem da `web` a partir do [Dockerfile](Dockerfile), sobe o Postgres (`db`) e só inicia a `web` depois que o Postgres passa no healthcheck. As migrations do Entity Framework rodam automaticamente no startup da aplicação (mesmo comportamento do ambiente de Development) — não é preciso rodar nenhum comando manual de migration.

No Cloudflare Tunnel, aponte o hostname público para `http://localhost:8080` (ou o endereço do host onde o container `web` expõe essa porta).

Para atualizar após um novo `git pull`:

```bash
docker compose up -d --build
```

## Cloudflare Access (configuração no painel, fora do repo)

Camada de borda na frente do login da aplicação: bloqueia bots, scanners e requests aleatórios *antes* de chegarem na tela de login. Nada disso depende de código no Controlume — é configuração no painel Cloudflare Zero Trust.

- [ ] Em **Zero Trust → Access → Applications**, criar uma aplicação do tipo **Self-hosted** apontando para o subdomínio do Controlume.
- [ ] Em **Policies**, criar uma política **Allow** com a regra **Emails** e a lista dos e-mails autorizados (só os de vocês).
- [ ] Em **Settings → Authentication**, deixar habilitado o método **One-time PIN** (código enviado por e-mail), para não depender de login social.
- [ ] Ajustar a duração da sessão (**Session Duration**) para algo confortável no balcão — sessões curtas obrigam a repetir o OTP com frequência.
- [ ] Testar em uma janela anônima: acessar o domínio deve pedir o e-mail e o código **antes** de mostrar a tela de login do Controlume.
- [ ] Testar com um e-mail fora da lista: o acesso deve ser negado pelo Cloudflare, sem chegar na aplicação.

O login da aplicação continua valendo depois disso: o Cloudflare Access diz *quem pode chegar até o sistema*, e o login do Controlume diz *quem é o usuário e o que ele pode fazer*.
