# Preparação do futuro executor SQL — NÃO EXECUTAR

Documento para revisão. Não implementa cliente SQL, não abre conexões e não concede autorização. O comando integral e o código do futuro executor deverão ser apresentados ao PM/PO antes de qualquer execução. As strings de autorização nos arquivos SQL não substituem essa aprovação.

## Destino e pré-condições

1. Exigir configuração explícita de instância `.\SQLEXPRESS`, banco `ZelaSP_Dev`, servidor efetivo esperado `slayr\SQLEXPRESS`, marcador único, arquivo SQL selecionado e manifesto local. Não inferir destino de ambiente, user-secrets ou configurações da API; não conectar a master ou a outro banco como alternativa.
2. O futuro cliente deverá construir a conexão com um connection-string builder: DataSource e InitialCatalog explícitos, IntegratedSecurity=true, sem usuário/senha. Opções de TLS deverão integrar o comando revisado, sem alterar configurações da instância. Não imprimir a connection string ou a identidade pessoal do Windows.
3. Após autorização específica de leitura, conferir servidor, instância, DB_NAME(), ONLINE e autenticação Windows NTLM/KERBEROS. Executar 01 e revisar schema, migração, triggers, ausência do marcador e baseline. Divergência interrompe o fluxo antes de autorizar qualquer escrita.
4. Aprovação de leitura não autoriza 02. A criação das oito referências exige autorização específica, baseline registrado e janela exclusiva. Aprovações anteriores de outras tasks não são reutilizáveis. As operações HTTP, reinícios e qualquer serviço externo também exigem aprovação própria.

## Vinculação dos parâmetros

O futuro executor deverá usar comandos parametrizados e parâmetros explicitamente tipados; não concatenar valores, substituir texto SQL ou usar inferência de AddWithValue. A tabela de [parâmetros e IDs](README.md) é o contrato de vinculação de cada arquivo.

- Autorizacao: NVarChar(100); ServidorEsperado e BancoEsperado: NVarChar(128); Marcador: NVarChar(45).
- IDs: Int positivo, obtido dos resultados efetivamente confirmados e mapeado no manifesto. Nunca usar IDs históricos, MAX ou identidade global.
- Flags: Bit, somente após revisão das respectivas evidências, nunca definidas automaticamente como verdadeiras.
- InicioUtc/FimUtc: DateTime2 com escala efetiva do provider EF, preservando os limites capturados na resposta do indicador. Não recalcular a janela por outro relógio.

Validar ausência de parâmetros obrigatórios antes de enviar o comando. Consumir todos os resultsets: 02 deve fornecer dois bairros, quatro categorias e dois dispositivos, com IDs distintos e associações corretas. Registrar nomes lógicos e IDs sem pressupor sequência numérica.

O baseline de eventos deve ser capturado **antes do POST de B**. Serializar IDs como array JSON: `[]` significa consulta concluída sem eventos; `null`, propriedade ausente ou resultado não capturado significa baseline desconhecido e bloqueia avanço. Em Windows PowerShell 5.1, preservar explicitamente coleções vazias e unitárias, sem confundir saída vazia do pipeline com array confirmado. O futuro executor deverá ter essa serialização revisada antes da execução.

## Correlação operacional dos eventos

EVENTO_SEGURANCA não possui FK para OCORRENCIA. Comparar snapshots antes/depois em janela exclusiva, sem outros escritores: devem surgir exatamente cinco IDs novos, uma categoria permitida por ID, com metadados previstos. Nenhum ID anterior ao POST de B pode ser atribuído ao lote. Guardar ambos os snapshots e a revisão no manifesto; `EventosIsoladosConferidos` é confirmação operacional humana, não garantia do schema.

03 apresenta timestamps persistidos lado a lado e interrompe se divergirem. A igualdade exata ainda depende de validação real; não é uma premissa comprovada nem evidência causal suficiente. 05 apresenta snapshot de IDs/horários, sem selecionar eventos por igualdade temporal. Qualquer dúvida, concorrência ou divergência exige interrupção e revisão manual. Não escolher cinco eventos convenientes, alterar timestamps ou preencher artificialmente uma categoria ausente.

## Registro e interrupção

Antes de cada envio, registrar no diretório local ignorado `demo/execucoes/`: marcador, etapa, autorização revisada, destino esperado, hash do SQL, parâmetros não sensíveis, horário UTC e estado EM_ENVIO. Registrar uma única tentativa. Não registrar credenciais, payloads pessoais ou conteúdo binário.

O futuro executor deverá usar timeout finito (proposta: 30 segundos, sujeito à revisão), sem retry automático. Registrar sucesso somente após consumir todos os resultados, conferir guardas/quantidades e salvar IDs e evidências. Arquivo parcial ou THROW não é sucesso. Qualquer divergência interrompe as etapas seguintes.

Erro antes do envio permite registrar NÃO_ENVIADO. Timeout, queda da conexão, falha ao salvar resultados ou erro após envio tornam o resultado AMBÍGUO: o servidor pode ter confirmado a transação sem o cliente receber a resposta. Não repetir INSERT ou POST, não limpar nem reparar. Preservar logs locais e solicitar revisão; eventual recuperação por 05 requer autorização específica de leitura. O rollback interno de 02 não permite presumir ausência de registros quando o cliente perdeu a conexão.

## Checklist para aprovação posterior

- Comando/código integral do executor revisado, sem execução automática ao importar arquivos.
- Destino, autenticação, TLS, timeout e vinculação tipada conferidos.
- Baseline real, arrays vazios e janela exclusiva documentados.
- Autorizações separadas, resultados por etapa e parada em ambiguidade conferidos.
- Evidências locais protegidas pelo ignore; publicação de evidências selecionadas depende de revisão de conteúdo e autorização.

Estado atual: apenas preparação documental. Nenhuma conexão, compilação SQL ou execução foi realizada nesta correção.
