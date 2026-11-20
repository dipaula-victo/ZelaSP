# Requisitos para demonstração real do LLM — ainda não executada

Inspeção somente por leitura de `OpenAiExplicacaoCidadaClient.cs`, `RiscoCidadaoService.cs`, contratos e registro de DI em Program.cs. Não foram lidos valores de user-secrets, tokens ou variáveis com chaves; disponibilidade de credencial/conta não foi verificada. Nenhum acesso externo.

## Implementação existente

- Configuração: `RiscoAi:Enabled`, `Provider`, `Model`, `ApiKey`. Habilitação padrão false; provider OpenAI; modelo padrão gpt-4.1-mini. Esses são valores do código, não confirmação de disponibilidade na conta remota.
- Endpoint fixo: https://api.openai.com/v1/responses; HTTP/JSON nativos, HttpClient por DI, Bearer apenas no header do pedido externo. Redirecionamento e cookies desabilitados; loggers do cliente removidos.
- Payload reconstruído de DTO tipado: período, pontuação, nível e contagens por categoria. Não inclui descrições, entidades, imagens, IDs individuais, nomes, executores ou textos livres de auditoria.
- Requer consistência entre contagens, score e nível antes do envio. Modelo e formato de credencial passam por validação local; essa validação não confirma autorização remota.
- Solicita saída JSON estruturada com somente texto, store=false, stream=false e max_output_tokens=300. store=false não deve ser apresentado como garantia geral de inexistência de retenção pelo provedor; requisitos contratuais precisam ser conferidos separadamente.
- Timeout de cinco segundos; limite de resposta 32 KiB inclusive sem Content-Length; sem repetição automática. Cancelamento do solicitante propagado.
- Rejeita status/estrutura inesperados, múltiplos textos, texto acima de 600 caracteres, caracteres proibidos, links e PII reconhecida. Serviço revalida e mantém pontuação determinística, independente da IA.
- Falha, configuração ausente, timeout ou texto rejeitado resultam em FALLBACK. Resposta aceita pelo adapter e serviço recebe origem LLM.

## Autorizações e pré-condições futuras

1. Aprovação expressa para chamada externa e respectivo custo; definir número máximo de consultas. Refresh, foco e clientes extras podem gerar novas chamadas mesmo sem retry automático.
2. Responsável configura credencial fora dos arquivos versionados, por user-secrets ou variável de ambiente restrita ao processo. Não solicitar que envie a chave no chat, não imprimir, não inserir em argumentos visíveis nem capturar em evidências.
3. Confirmar conta, saldo/limites, permissão e disponibilidade do modelo, conectividade HTTPS e regras de tratamento dos agregados. Não desativar TLS ou contornar proxy/permissões.
4. Selecionar build atualizado, portas livres, Development e banco autorizado; preservar processos preexistentes. Revisar eventual configuração herdada sem revelar segredos.
5. Habilitar explicitamente RiscoAi:Enabled somente no processo autorizado. Manter a política CORS restrita e perfil de navegador isolado; permitir apenas o GET de risco.
6. Usar agregados existentes, mesmo zerados. Não criar eventos/PII para tornar a demonstração mais expressiva. Não reenviar após falha sem decisão explícita.

## Evidências e critérios de sucesso

Registrar horário, modelo configurado sem credencial, GET observado, JSON de saída validado, score/contagens, origem e captura da explicação no dashboard. Comparar pontuação e nível com o cálculo determinístico, sem exigir texto idêntico ao fallback.

Origem LLM precisa vir do backend real atualizado, em perfil novo e sem fixtures. HTTP 200 do endpoint com FALLBACK, sozinho, não prova que houve inferência externa: o retorno também ocorre com IA desabilitada, erro remoto ou validação recusada. O contrato público deliberadamente não expõe o motivo detalhado do fallback. Se for necessária prova adicional do status/request-id remoto, aprovar antes um procedimento seguro; não adicionar logging de prompts, respostas brutas ou tokens por conta própria.

## Riscos e limites

- Timeout curto, indisponibilidade, limite de conta ou resposta rejeitada podem produzir fallback legítimo. Não fabricar sucesso nem repetir automaticamente.
- Texto sem PII reconhecida ainda pode ser semanticamente incorreto; conferir explicação cidadã, sem delegar pontuação à IA. Detectores não reconhecem toda informação pessoal possível.
- Contagens agregadas são metadados de segurança; envio externo requer aprovação mesmo sem identificação individual.
- Demonstração de bloqueio real de sink inseguro permanece separada: eventos unsafeLog do simulador Mobile não são produtor real no backend.
- Esta preparação não valida modelo, credencial, rede, cobrança, inferência remota ou integração nativa Android/iOS.

Até aprovação, manter o LLM desabilitado. A evidência atual comprova exclusivamente API real com fallback, em [evidências permanentes](evidencias/integracao-real-5188-20261119/README.md).
