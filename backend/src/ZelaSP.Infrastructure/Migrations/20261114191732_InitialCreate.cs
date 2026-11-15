using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZelaSP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BAIRRO",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    subprefeitura = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BAIRRO", x => x.id);
                    table.CheckConstraint("CK_BAIRRO_Id", "[id] > 0");
                });

            migrationBuilder.CreateTable(
                name: "CATEGORIA_OCORRENCIA",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIA_OCORRENCIA", x => x.id);
                    table.CheckConstraint("CK_CATEGORIA_OCORRENCIA_Id", "[id] > 0");
                });

            migrationBuilder.CreateTable(
                name: "EVENTO_SEGURANCA",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_evento = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    tipo_dado = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    origem = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    destino = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    acao = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ocorrido_em = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVENTO_SEGURANCA", x => x.id);
                    table.CheckConstraint("CK_EVENTO_SEGURANCA_Id", "[id] > 0");
                });

            migrationBuilder.CreateTable(
                name: "DISPOSITIVO",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bairro_id = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    local = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DISPOSITIVO", x => x.id);
                    table.CheckConstraint("CK_DISPOSITIVO_Id", "[id] > 0");
                    table.ForeignKey(
                        name: "FK_DISPOSITIVO_BAIRRO_bairro_id",
                        column: x => x.bairro_id,
                        principalTable: "BAIRRO",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "OCORRENCIA",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bairro_id = table.Column<int>(type: "int", nullable: false),
                    categoria_id = table.Column<int>(type: "int", nullable: false),
                    descricao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    criada_em = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    origem = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCORRENCIA", x => x.id);
                    table.CheckConstraint("CK_OCORRENCIA_Id", "[id] > 0");
                    table.CheckConstraint("CK_OCORRENCIA_Origem", "[origem] IN ('APP', 'SENSOR')");
                    table.CheckConstraint("CK_OCORRENCIA_Status", "[status] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')");
                    table.ForeignKey(
                        name: "FK_OCORRENCIA_BAIRRO_bairro_id",
                        column: x => x.bairro_id,
                        principalTable: "BAIRRO",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_OCORRENCIA_CATEGORIA_OCORRENCIA_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "CATEGORIA_OCORRENCIA",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "MENSAGEM_RISCO",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evento_seguranca_id = table.Column<int>(type: "int", nullable: false),
                    mensagem = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    origem_mensagem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status_traducao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    gerada_em = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MENSAGEM_RISCO", x => x.id);
                    table.CheckConstraint("CK_MENSAGEM_RISCO_Id", "[id] > 0");
                    table.ForeignKey(
                        name: "FK_MENSAGEM_RISCO_EVENTO_SEGURANCA_evento_seguranca_id",
                        column: x => x.evento_seguranca_id,
                        principalTable: "EVENTO_SEGURANCA",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "LEITURA_SENSOR",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dispositivo_id = table.Column<int>(type: "int", nullable: false),
                    nivel_percentual = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    lida_em = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEITURA_SENSOR", x => x.id);
                    table.CheckConstraint("CK_LEITURA_SENSOR_Id", "[id] > 0");
                    table.CheckConstraint("CK_LEITURA_SENSOR_NivelPercentual", "[nivel_percentual] >= 0 AND [nivel_percentual] <= 100");
                    table.ForeignKey(
                        name: "FK_LEITURA_SENSOR_DISPOSITIVO_dispositivo_id",
                        column: x => x.dispositivo_id,
                        principalTable: "DISPOSITIVO",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "HISTORICO_STATUS",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ocorrencia_id = table.Column<int>(type: "int", nullable: false),
                    status_anterior = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    status_novo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    alterado_em = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    executor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HISTORICO_STATUS", x => x.id);
                    table.CheckConstraint("CK_HISTORICO_STATUS_Id", "[id] > 0");
                    table.CheckConstraint("CK_HISTORICO_STATUS_StatusAnterior", "[status_anterior] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')");
                    table.CheckConstraint("CK_HISTORICO_STATUS_StatusNovo", "[status_novo] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')");
                    table.ForeignKey(
                        name: "FK_HISTORICO_STATUS_OCORRENCIA_ocorrencia_id",
                        column: x => x.ocorrencia_id,
                        principalTable: "OCORRENCIA",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "IMAGEM_OCORRENCIA",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ocorrencia_id = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    nome_arquivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    mime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    dados = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    data_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    rm_lider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    executor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IMAGEM_OCORRENCIA", x => x.id);
                    table.CheckConstraint("CK_IMAGEM_OCORRENCIA_Id", "[id] > 0");
                    table.ForeignKey(
                        name: "FK_IMAGEM_OCORRENCIA_OCORRENCIA_ocorrencia_id",
                        column: x => x.ocorrencia_id,
                        principalTable: "OCORRENCIA",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DISPOSITIVO_bairro_id",
                table: "DISPOSITIVO",
                column: "bairro_id");

            migrationBuilder.CreateIndex(
                name: "IX_HISTORICO_STATUS_ocorrencia_id",
                table: "HISTORICO_STATUS",
                column: "ocorrencia_id");

            migrationBuilder.CreateIndex(
                name: "IX_IMAGEM_OCORRENCIA_ocorrencia_id",
                table: "IMAGEM_OCORRENCIA",
                column: "ocorrencia_id");

            migrationBuilder.CreateIndex(
                name: "IX_LEITURA_SENSOR_dispositivo_id",
                table: "LEITURA_SENSOR",
                column: "dispositivo_id");

            migrationBuilder.CreateIndex(
                name: "IX_MENSAGEM_RISCO_evento_seguranca_id",
                table: "MENSAGEM_RISCO",
                column: "evento_seguranca_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OCORRENCIA_bairro_id",
                table: "OCORRENCIA",
                column: "bairro_id");

            migrationBuilder.CreateIndex(
                name: "IX_OCORRENCIA_categoria_id",
                table: "OCORRENCIA",
                column: "categoria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HISTORICO_STATUS");

            migrationBuilder.DropTable(
                name: "IMAGEM_OCORRENCIA");

            migrationBuilder.DropTable(
                name: "LEITURA_SENSOR");

            migrationBuilder.DropTable(
                name: "MENSAGEM_RISCO");

            migrationBuilder.DropTable(
                name: "OCORRENCIA");

            migrationBuilder.DropTable(
                name: "DISPOSITIVO");

            migrationBuilder.DropTable(
                name: "EVENTO_SEGURANCA");

            migrationBuilder.DropTable(
                name: "CATEGORIA_OCORRENCIA");

            migrationBuilder.DropTable(
                name: "BAIRRO");
        }
    }
}
