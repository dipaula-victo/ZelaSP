# Cybersecurity — fluxo, validação e pendências

Etapa local, sem PDF final, publicação ou inferência externa. Branch observada: feature/task-008-academic-deliverables; nenhuma operação Git de escrita. A integração de PR9 à main foi informada pelo responsável; não houve fetch, merge ou troca de branch. Entregas C#/SOA e ApiExceptionHandler preservados.

## Implementação

O backend já possui `ProtecaoPii.Analisar`, auditoria atômica em `OcorrenciasStore.CriarAsync`, agregação em `RiscoCidadaoStore.ContarAsync`, cálculo em `CalculadoraRiscoCidadao.Calcular`, serviço de risco e adapter `OpenAiExplicacaoCidadaClient.GerarAsync`. A pontuação não depende da IA. Não houve alteração desses componentes.

No Mobile, a tela ignorava janela/explicação, o parser devolvia o objeto HTTP inteiro e o risco do cache não era validado. Agora:

- `Risk` modela `janela` e `explicacao`, opcionais somente para registros legados/mock; a resposta real da API deve conter ambas.
- `fetchRisk` faz um único GET pelo cliente HTTP existente, sem body/retry, e converte falhas em erro genérico constante.
- `parseRisk` valida inteiros seguros, pesos/classe, alertas fixos correspondentes sem duplicação, janela UTC de sete dias e explicação LLM/FALLBACK. Texto deve ter1–600 caracteres, sem controles, markup, links ou PII reconhecida; normalização NFC é usada para detectar acentos combinados. Nunca se altera a pontuação para fazer a resposta parecer válida.
- Retorna apenas campos permitidos; texto livre extra, categorias desconhecidas e metadados não chegam ao cache/UI. O risco é revalidado ao restaurar cache e ao renderizar. Cache inválido é desconsiderado, preservando os outros dados do aplicativo.
- O painel exibe explicação, origem e janela, além de score, classe, contagens e alertas. Mantém loading, offline, erro genérico e último resultado seguro. Falhas de catálogos/ocorrências não são usadas como diagnóstico da consulta de risco.
- Mock produz fallback local identificado como demonstração, sem chamada LLM. Cache legado não recebe uma origem LLM inventada. Serviços de cadastro, fotos e status não foram reimplementados.

## Sources → transforms → sinks

```text
Relato HTTP não confiável
 → validação e ProtecaoPii
 → OCORRENCIA protegida + EVENTO_SEGURANCA técnico no mesmo SaveChanges
 → janela UTC, vocabulário permitido, agregação e cálculo local
 → ResumoRiscoAi (números/enums, sem dados individuais)
 → OpenAI somente com habilitação/configuração explícita
 → validação do texto OU fallback
 → GET JSON → validação Mobile → cache validado → dashboard
```

O adapter limita o tempo a cinco segundos, o corpo a32 KiB e o texto a600 caracteres, sem redirecionamentos ou retries. O payload agregado usa store=false. O Mobile mantém timeout HTTP de15 segundos. Credenciais pertencem somente ao backend; nunca em EXPO_PUBLIC_*. A origem LLM numa fixture não comprova inferência real.

[TAINT_ANALYSIS.md](TAINT_ANALYSIS.md) distingue barreiras reais e simulações. Não existe produtor backend de VAZAMENTO_BLOQUEADO. Os logs TAINT e bloqueios do Mobile são somente do simulador. O GET não cria eventos. Não foi implementado rastreador universal de variáveis ou sink inseguro de teste; a demonstração real de bloqueio ainda precisa ser definida com o PM.

## Testes e reprodução

Node disponível:24.21.0. mobile/node_modules ausente; nenhum pacote foi instalado e package-lock.json foi preservado. Da raiz:

```powershell
node .\mobile\tests\run-cyber-isolated.mjs
```

O runner usa type stripping nativo, não type checking. Bloqueia rede real e importa core.test.ts/risk-api.test.ts; requests são substituídos por fetch em memória. Não inicia Expo, API ou navegador. O teste core SQLite cria/remove apenas seus arquivos temporários próprios, sem SQL Server. Com as dependências originais disponíveis, complementar em mobile/ com `npm run typecheck` e `npm test`; não foram executados aqui. Os testes de renderização foram estendidos, mas continuam pendentes de execução por falta de React/Babel.

Resultados intermediários reais:

- Exploração do core na raiz:25 aprovações/1 falha por caminho relativo da fotografia. Nenhum teste antigo foi alterado; o runner usa mobile/ como diretório.
- Primeira rodada do runner:73 aprovações/1 falha na assertiva nova para entrada de um espaço. A implementação rejeitava corretamente; o teste confundia espaço comum com eco do payload. Corrigida a assertiva para exigir a mensagem genérica exata.
- Rodada seguinte:74 aprovações/zero falhas. Após essa rodada foram acrescentados dois casos NFC; conferir o registro final abaixo.
- Rodada após NFC:76 executados/76 aprovados, zero falhas/ignorados/cancelados, exit0, duração Node54,0374ms. Em seguida foram acrescentados casos de bloqueio agregado, teto100 e banco sem eventos.
- **Resultado final:79 executados, 79 aprovados, zero falhas/ignorados/cancelados**, exit0; duração Node51,5316ms. São26 casos core existentes e53 novos do contrato/cliente. Testes React de renderização não estão nesse total; não é execução integral do Mobile nem integração remota real.
- Aviso Node MODULE_TYPELESS_PACKAGE_JSON preservado; não se alterou o tipo de módulo do aplicativo para suprimi-lo.
- Testes backend com filtro ProtecaoPiiTests/PiiApiTests/Risco e exclusão SqlServerWrite, Debug/--no-restore: build exit1, MSB3021/MSB3027, por DLLs da API em uso. Nenhum teste dessa tentativa executou. Não se encerrou processo, limpou arquivo, restaurou pacote ou reutilizou binário sem correspondência comprovada.

## Ambiente e limitações

- Modo padrão é mock; configurar modo API explicitamente conforme [instruções Mobile](../../mobile/README.md#cybersecurity--integração-do-painel).
- Outras telas mantêm incompatibilidades históricas de listagem/categorias/status, fora deste recorte. Uma consulta de risco independente não comprova integração de todo o aplicativo.
- Há três links antigos no README Mobile para docs/INTEGRACAO.md, docs/VALIDACAO.md e docs/ENTREGA.md, arquivos ausentes no repositório inspecionado. São lacunas preexistentes fora desta correção; os novos links de Cybersecurity foram conferidos e resolvem localmente.
- Expo Web exige CORS/proxy aprovado; a API não habilita CORS hoje. Não reduzir a segurança do navegador ou liberar qualquer origem. Program.cs não foi alterado. Cliente nativo não utiliza CORS do navegador.
- Execução visual/typecheck/renderização e dispositivo aguardam dependências originais e ambiente compatível. Os testes puros não os substituem.
- Inferência real exige autorização, configuração segura e conta/modelo disponíveis. Revisar custo/retenção sem imprimir chaves. Usar exclusivamente agregados fictícios; nenhum serviço externo foi acessado nesta etapa.
- Detector heurístico não garante reconhecimento de nomes/RG/ofuscações ou informações médicas fora da lista. Volumes pequenos podem permitir inferências. Contagens além do inteiro seguro JavaScript são rejeitadas, sem arredondamento silencioso.
- Não há comprovação ponta a ponta de evento persistido → provedor real → painel, nem de sink inseguro bloqueado pelo backend. Não apresentar mock/fallback como substituto da integração exigida.

## Vídeo — roteiro futuro, ainda não executado

1. Mostrar modo API e ambiente autorizado com eventos fictícios conhecidos, sem chaves.
2. Demonstrar parsing e auditoria técnica em ambiente isolado aprovado. Novas escritas no SQL Server oficial exigem autorização específica.
3. Explicar sources/transforms/sinks e conferir que o payload externo tem somente agregados, nunca relatos ou Authorization nas capturas.
4. Atualizar o painel mostrando score/classe/contagens/janela/explicação e origem. Inferência real deve ser comprovada por chamada autorizada, não por fixture marcada LLM.
5. Mostrar fallback e indisponibilidade com aviso genérico/último resultado, identificando o que foi simulado.
6. Distinguir eventos PII reais de bloqueios locais simulados; apresentar a pendência de cenário Taint Analysis ao PM.

Não gravar o vídeo como prova de integração completa antes das validações pendentes. Nenhum PDF final, ZIP, commit ou publicação produzido.
