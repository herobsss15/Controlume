using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Controlume.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fechamentos_caixa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data_abertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fechamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valor_inicial = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fechamentos_caixa", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_produto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipos_produto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fechamento_caixa_id = table.Column<int>(type: "integer", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendas", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendas_fechamentos_caixa_fechamento_caixa_id",
                        column: x => x.fechamento_caixa_id,
                        principalTable: "fechamentos_caixa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_produto_id = table.Column<int>(type: "integer", nullable: false),
                    artista_grupo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    preco = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    quantidade_estoque = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_produtos", x => x.id);
                    table.ForeignKey(
                        name: "fk_produtos_tipos_produto_tipo_produto_id",
                        column: x => x.tipo_produto_id,
                        principalTable: "tipos_produto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "venda_pagamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venda_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venda_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_venda_pagamentos_vendas_venda_id",
                        column: x => x.venda_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_venda",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venda_id = table.Column<int>(type: "integer", nullable: false),
                    produto_id = table.Column<int>(type: "integer", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_tabela = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_venda = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_venda", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_venda_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_itens_venda_vendas_venda_id",
                        column: x => x.venda_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tipos_produto",
                columns: new[] { "id", "ativo", "nome" },
                values: new object[,]
                {
                    { 1, true, "Disco" },
                    { 2, true, "CD" },
                    { 3, true, "DVD" },
                    { 4, true, "Eletrônico" },
                    { 5, true, "Avulso" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_caixa_status",
                table: "fechamentos_caixa",
                column: "status",
                unique: true,
                filter: "status = 'Aberto'");

            migrationBuilder.CreateIndex(
                name: "ix_itens_venda_produto_id",
                table: "itens_venda",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_venda_venda_id",
                table: "itens_venda",
                column: "venda_id");

            migrationBuilder.CreateIndex(
                name: "ix_produtos_tipo_produto_id",
                table: "produtos",
                column: "tipo_produto_id");

            migrationBuilder.CreateIndex(
                name: "ix_venda_pagamentos_venda_id",
                table: "venda_pagamentos",
                column: "venda_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendas_fechamento_caixa_id",
                table: "vendas",
                column: "fechamento_caixa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_venda");

            migrationBuilder.DropTable(
                name: "venda_pagamentos");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "vendas");

            migrationBuilder.DropTable(
                name: "tipos_produto");

            migrationBuilder.DropTable(
                name: "fechamentos_caixa");
        }
    }
}
