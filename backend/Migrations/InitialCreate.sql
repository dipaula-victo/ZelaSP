IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [BAIRRO] (
        [id] int NOT NULL IDENTITY,
        [nome] nvarchar(120) NOT NULL,
        [subprefeitura] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_BAIRRO] PRIMARY KEY ([id]),
        CONSTRAINT [CK_BAIRRO_Id] CHECK ([id] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [CATEGORIA_OCORRENCIA] (
        [id] int NOT NULL IDENTITY,
        [nome] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_CATEGORIA_OCORRENCIA] PRIMARY KEY ([id]),
        CONSTRAINT [CK_CATEGORIA_OCORRENCIA_Id] CHECK ([id] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [EVENTO_SEGURANCA] (
        [id] int NOT NULL IDENTITY,
        [tipo_evento] nvarchar(80) NOT NULL,
        [tipo_dado] nvarchar(80) NOT NULL,
        [origem] nvarchar(120) NOT NULL,
        [destino] nvarchar(120) NOT NULL,
        [acao] nvarchar(120) NOT NULL,
        [ocorrido_em] datetime2(3) NOT NULL,
        CONSTRAINT [PK_EVENTO_SEGURANCA] PRIMARY KEY ([id]),
        CONSTRAINT [CK_EVENTO_SEGURANCA_Id] CHECK ([id] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [DISPOSITIVO] (
        [id] int NOT NULL IDENTITY,
        [bairro_id] int NOT NULL,
        [nome] nvarchar(120) NOT NULL,
        [local] nvarchar(250) NOT NULL,
        CONSTRAINT [PK_DISPOSITIVO] PRIMARY KEY ([id]),
        CONSTRAINT [CK_DISPOSITIVO_Id] CHECK ([id] > 0),
        CONSTRAINT [FK_DISPOSITIVO_BAIRRO_bairro_id] FOREIGN KEY ([bairro_id]) REFERENCES [BAIRRO] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [OCORRENCIA] (
        [id] int NOT NULL IDENTITY,
        [bairro_id] int NOT NULL,
        [categoria_id] int NOT NULL,
        [descricao] nvarchar(4000) NOT NULL,
        [status] varchar(20) NOT NULL,
        [criada_em] datetime2(3) NOT NULL,
        [origem] varchar(20) NOT NULL,
        CONSTRAINT [PK_OCORRENCIA] PRIMARY KEY ([id]),
        CONSTRAINT [CK_OCORRENCIA_Id] CHECK ([id] > 0),
        CONSTRAINT [CK_OCORRENCIA_Origem] CHECK ([origem] IN ('APP', 'SENSOR')),
        CONSTRAINT [CK_OCORRENCIA_Status] CHECK ([status] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')),
        CONSTRAINT [FK_OCORRENCIA_BAIRRO_bairro_id] FOREIGN KEY ([bairro_id]) REFERENCES [BAIRRO] ([id]),
        CONSTRAINT [FK_OCORRENCIA_CATEGORIA_OCORRENCIA_categoria_id] FOREIGN KEY ([categoria_id]) REFERENCES [CATEGORIA_OCORRENCIA] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [MENSAGEM_RISCO] (
        [id] int NOT NULL IDENTITY,
        [evento_seguranca_id] int NOT NULL,
        [mensagem] nvarchar(2000) NOT NULL,
        [origem_mensagem] nvarchar(40) NOT NULL,
        [status_traducao] nvarchar(40) NOT NULL,
        [gerada_em] datetime2(3) NOT NULL,
        CONSTRAINT [PK_MENSAGEM_RISCO] PRIMARY KEY ([id]),
        CONSTRAINT [CK_MENSAGEM_RISCO_Id] CHECK ([id] > 0),
        CONSTRAINT [FK_MENSAGEM_RISCO_EVENTO_SEGURANCA_evento_seguranca_id] FOREIGN KEY ([evento_seguranca_id]) REFERENCES [EVENTO_SEGURANCA] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [LEITURA_SENSOR] (
        [id] int NOT NULL IDENTITY,
        [dispositivo_id] int NOT NULL,
        [nivel_percentual] decimal(5,2) NOT NULL,
        [lida_em] datetime2(3) NOT NULL,
        CONSTRAINT [PK_LEITURA_SENSOR] PRIMARY KEY ([id]),
        CONSTRAINT [CK_LEITURA_SENSOR_Id] CHECK ([id] > 0),
        CONSTRAINT [CK_LEITURA_SENSOR_NivelPercentual] CHECK ([nivel_percentual] >= 0 AND [nivel_percentual] <= 100),
        CONSTRAINT [FK_LEITURA_SENSOR_DISPOSITIVO_dispositivo_id] FOREIGN KEY ([dispositivo_id]) REFERENCES [DISPOSITIVO] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [HISTORICO_STATUS] (
        [id] int NOT NULL IDENTITY,
        [ocorrencia_id] int NOT NULL,
        [status_anterior] varchar(20) NOT NULL,
        [status_novo] varchar(20) NOT NULL,
        [alterado_em] datetime2(3) NOT NULL,
        [executor] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_HISTORICO_STATUS] PRIMARY KEY ([id]),
        CONSTRAINT [CK_HISTORICO_STATUS_Id] CHECK ([id] > 0),
        CONSTRAINT [CK_HISTORICO_STATUS_StatusAnterior] CHECK ([status_anterior] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')),
        CONSTRAINT [CK_HISTORICO_STATUS_StatusNovo] CHECK ([status_novo] IN ('ABERTA', 'EM_ANALISE', 'RESOLVIDA')),
        CONSTRAINT [FK_HISTORICO_STATUS_OCORRENCIA_ocorrencia_id] FOREIGN KEY ([ocorrencia_id]) REFERENCES [OCORRENCIA] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE TABLE [IMAGEM_OCORRENCIA] (
        [id] int NOT NULL IDENTITY,
        [ocorrencia_id] int NOT NULL,
        [titulo] nvarchar(100) NOT NULL,
        [nome_arquivo] nvarchar(255) NOT NULL,
        [mime] nvarchar(100) NOT NULL,
        [dados] varbinary(max) NOT NULL,
        [data_utc] datetime2(3) NOT NULL,
        [rm_lider] nvarchar(20) NOT NULL,
        [executor] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_IMAGEM_OCORRENCIA] PRIMARY KEY ([id]),
        CONSTRAINT [CK_IMAGEM_OCORRENCIA_Id] CHECK ([id] > 0),
        CONSTRAINT [FK_IMAGEM_OCORRENCIA_OCORRENCIA_ocorrencia_id] FOREIGN KEY ([ocorrencia_id]) REFERENCES [OCORRENCIA] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DISPOSITIVO_bairro_id] ON [DISPOSITIVO] ([bairro_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HISTORICO_STATUS_ocorrencia_id] ON [HISTORICO_STATUS] ([ocorrencia_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IMAGEM_OCORRENCIA_ocorrencia_id] ON [IMAGEM_OCORRENCIA] ([ocorrencia_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LEITURA_SENSOR_dispositivo_id] ON [LEITURA_SENSOR] ([dispositivo_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MENSAGEM_RISCO_evento_seguranca_id] ON [MENSAGEM_RISCO] ([evento_seguranca_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OCORRENCIA_bairro_id] ON [OCORRENCIA] ([bairro_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OCORRENCIA_categoria_id] ON [OCORRENCIA] ([categoria_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114191732_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114191732_InitialCreate', N'10.0.6');
END;

COMMIT;
GO

