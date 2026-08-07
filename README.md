# Controlume
Sistema de PDV para loja física (discos, CDs, DVDs, eletrônicos e avulsos) — cadastro de produtos, controle de estoque, preços, vendas com pagamento em dinheiro/cartão/Pix (inclusive dividido) e fechamento de caixa. ASP.NET Core (Blazor Server) + PostgreSQL.

## Deploy em produção (Docker Compose)

Pensado para um home server atrás de Cloudflare Tunnel: o Kestrel só escuta HTTP na porta 8080 (sem certificado na aplicação), e o Cloudflare cuida do TLS na borda.

1. Copie `.env.example` para `.env` e defina uma `POSTGRES_PASSWORD` forte (as demais variáveis têm defaults razoáveis).
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
