# Controlume
Sistema de PDV para loja física (discos, CDs, DVDs, eletrônicos e avulsos) — cadastro de produtos, controle de estoque, preços, vendas com pagamento dividido em várias formas (dinheiro, cartão, Pix e canais externos como o Mercado Livre), cancelamento de venda, sangria e fechamento de caixa. ASP.NET Core (Blazor Server) + PostgreSQL.

## Acesso e papéis

Toda rota exige usuário autenticado; sem cookie válido o sistema redireciona para `/login`. O papel do usuário decide o que aparece e o que pode ser executado — e a checagem vale tanto na tela quanto na camada de serviço, então esconder o botão não é a única barreira.

| Papel | Venda, caixa e sangria | Cadastros (produto, tipo, forma de pagamento) | Cancelar venda | Históricos | Usuários |
|---|---|---|---|---|---|
| `Admin` | sim | sim | de qualquer caixa, aberto ou fechado | sim | sim |
| `Operador` | sim | não acessa as telas | só de caixa ainda aberto | sim | não acessa |
| `Stakeholder` | só leitura | só leitura | nunca | sim | não acessa |

Os papéis são fixos (`Admin`, `Operador`, `Stakeholder`) — o que se cria são usuários, escolhendo um desses papéis.

### Gerenciando usuários

O Admin cria, edita, desativa e redefine senhas em **/usuarios**. É a única tela que o Stakeholder não enxerga, porque a lista mostra quem tem acesso ao sistema. Senha mínima de 8 caracteres; o login é a identidade do usuário e não muda depois de criado.

Duas travas para ninguém se trancar do lado de fora: não dá para desativar a própria conta, nem para desativar (ou rebaixar) o **último Admin ativo**.

Desativar alguém ou trocar o papel dele vale quase na hora, mesmo com a sessão aberta: o cookie é reconferido contra o banco a cada carregamento de página e o circuito Blazor revalida a cada 5 minutos.

### O primeiro Admin

Como não há tela para criar o primeiro acesso, o Admin de bootstrap vem da configuração `Usuarios:Seed` — em produção, `ADMIN_LOGIN`/`ADMIN_SENHA` no `.env` (veja [.env.example](.env.example)). O startup cria esse usuário se ele não existir e regrava a senha quando ela deixa de bater com a configuração, então **trocar `ADMIN_SENHA` e subir de novo é também o caminho de recuperação** se a senha do Admin se perder. Em Development, o usuário de teste está em [appsettings.Development.json](src/Controlume.Web/appsettings.Development.json) (`admin`/`admin123`) — os demais você cria pela tela.

Não há recuperação de senha por e-mail: quem esquece a senha recebe uma nova do Admin em /usuarios.

## Formas de pagamento e canais externos

As formas de pagamento são um cadastro em **/formas-pagamento**, não uma lista fixa no código: quando entra um canal de venda novo, é só cadastrar. Cada forma carrega duas flags que decidem sozinhas como ela se comporta no resto do sistema:

- **Conta como caixa físico** — marca o que ocupa a gaveta. Só essas formas entram no saldo em dinheiro e no limite da sangria. Hoje só o Dinheiro.
- **Requer confirmação de recebimento** — para canais com repasse posterior, em que o dinheiro entra dias depois da venda. Hoje só o Mercado Livre.

Uma venda em Dinheiro, Cartão ou Pix já nasce com o pagamento recebido. Uma venda pelo Mercado Livre nasce **aguardando recebimento**, e a tela de detalhe da venda mostra um botão *Marcar como recebido* para quando o repasse cair. Essa marcação é de mão única: uma vez confirmada, não há como desfazer — nem pela tela, nem chamando o serviço.

Forma de pagamento já usada em alguma venda **não pode ser excluída**, só desativada (mesma regra dos tipos de produto). Desativar tira a forma da tela de venda sem mexer no histórico.

## Cancelamento de venda

Cancelar é um delete lógico: a venda continua no histórico, marcada como **Cancelada** e com o motivo à vista. O motivo é obrigatório.

- O **estoque volta**: a quantidade de cada item é devolvida ao produto.
- A venda **sai de todos os cálculos financeiros** — saldo em dinheiro, resumo de caixa e totais passam a ignorá-la.
- Quem cancela depende do papel e do estado do caixa daquela venda: o `Operador` só alcança venda de caixa ainda aberto; o `Admin` cancela de qualquer caixa; o `Stakeholder` nunca vê o botão.
- Não há reabertura nem edição de venda cancelada. Para corrigir um erro, registre uma venda nova.

Cancelar uma venda de um caixa **já fechado** não reescreve o `SaldoFinal` daquele fechamento: ele é um dado histórico congelado (ver abaixo). Quando isso acontece, a tela de detalhe do caixa passa a mostrar um aviso comparando o saldo registrado com o recalculado — a diferença precisa ser acertada na mão, na próxima abertura ou sangria.

## Caixa, sangria e saldo em dinheiro

- **Sangria** é uma retirada de dinheiro da gaveta, com motivo obrigatório (`Pagamento` ou `Compra`) e descrição opcional. Só pode ser registrada com caixa aberto e nunca pode deixar o saldo em dinheiro negativo — o limite é o saldo do momento (inicial + vendas em dinheiro − sangrias já feitas), não o valor inicial.
- O **saldo em dinheiro** aparece ao vivo enquanto o caixa está aberto. Só entram nessa conta as formas marcadas como *conta como caixa físico*: cartão, Pix e canais externos não ocupam a gaveta.
- No fechamento, esse saldo é congelado em `SaldoFinal`.
- Ao abrir o próximo caixa, o `ValorInicial` já vem preenchido com o `SaldoFinal` do último fechamento. É sugestão: a contagem física manda, e o campo continua editável.

## Deploy em produção (Docker Compose)

Pensado para um home server atrás de Cloudflare Tunnel: o Kestrel só escuta HTTP na porta 8080 (sem certificado na aplicação), e o Cloudflare cuida do TLS na borda.

1. Copie `.env.example` para `.env`, defina uma `POSTGRES_PASSWORD` forte e uma `ADMIN_SENHA` (ambas são obrigatórias; as demais variáveis têm defaults razoáveis). Depois de subir, entre com o Admin e crie os outros usuários em `/usuarios`.
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
