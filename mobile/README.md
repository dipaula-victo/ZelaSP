# ZelaSP — Aplicativo do cidadão

## Cybersecurity — integração do painel

A tela Risco Cidadão usa `gateway.getRisk()` e, em modo API, o cliente `fetchRisk()` para consultar GET /api/risco-cidadao. Exibe pontuação, nível, contagens, janela UTC e explicação com origem LLM/FALLBACK, mantendo os alertas fixos. Valida resposta e cache, descarta campos livres extras e usa erros genéricos. Em modo mock, identifica o fallback como demonstração sem LLM. Testes simulados não comprovam inferência externa ou execução em dispositivo.

Configuração de exemplo somente para o processo de desenvolvimento, sem credenciais:

```powershell
$env:EXPO_PUBLIC_DATA_MODE = 'api'
$env:EXPO_PUBLIC_API_URL = 'http://127.0.0.1:5188'
$env:EXPO_PUBLIC_TEST_MODE = 'false'
```

O endereço deve ser acessível pelo cliente: loopback serve para o mesmo computador; no emulador Android padrão, o host pode ser acessado por10.0.2.2; aparelho físico precisa do IP local e de um listener/rede previamente aprovados. Não confundir localhost do aparelho com o computador. Depois de configurar, reiniciar o Expo existente sob controle do responsável para aplicar o bundle; nenhum processo foi iniciado nesta etapa. Não colocar chave OpenAI em EXPO_PUBLIC_*: essas variáveis são públicas no bundle.

Expo Web utiliza CORS restrito a Development, origem http://127.0.0.1:8081 e GET /api/risco-cidadao. A demonstração real usou a API em http://127.0.0.1:5188, sem escrita SQL e com FALLBACK; outras chamadas foram bloqueadas no navegador para isolar o recorte. Não liberar origens curingas ou credenciais cross-origin. Essa prova não resolve os contratos das outras telas.

Validação sem instalar pacotes, em Node24 ou superior, a partir da raiz do repositório:

```powershell
node .\mobile\tests\run-cyber-isolated.mjs
```

O runner substitui fetch e testa o core e o cliente/contrato com respostas em memória. Não faz type checking nem renderiza o app. React/Babel/typecheck e testes de renderização precisam das dependências originais. Resultados, falhas e roteiro do vídeo em [Cybersecurity — fluxo e validação](../backend/docs/CYBERSECURITY_FLUXO_E_VALIDACAO.md). Não houve chamada real ao LLM. Os bloqueios TAINT produzidos pelo mock não são eventos do backend SQL Server.

## Ficha da CitizenTech

| Campo | Definição |
|---|---|
| Nome | ZelaSP |
| Cidade / território | São Paulo / Jabaquara (distrito) |
| Cidadão beneficiado | Morador, pedestre ou comerciante afetado por descarte irregular, calçadas bloqueadas e lixeiras cheias |
| Problema | Falta de uma forma simples de registrar e acompanhar o tratamento de problemas de zeladoria do bairro |
| Funcionamento | Relato com categoria, bairro e descrição; foto opcional anexada pelo protocolo; consulta de status, histórico e Risco Cidadão |
| Partes | Mobile: interface; C#/SOA: API e transações; Cyber: proteção de dados; IoT: sensor; OS: infraestrutura; IA: modelo SP156; QA: testes |
| Tecnologias deste módulo | React Native, Expo SDK 58, TypeScript, Expo Router, SQLite, ImagePicker; IndexedDB somente na prévia web |
| Ecossistema | Moradores, comerciantes, subprefeitura e limpeza urbana como atores do problema; nenhuma integração ou parceria oficial implementada |
| Indicador proposto | Tempo médio entre registro e resolução, em dias; não houve medição de impacto real |
| ODS | 11, meta 11.6: resíduos e ambiente urbano; 9: infraestrutura e sensores; 10: acesso ao acompanhamento de solicitações |
| Observação e escuta | O documento do grupo usa pesquisa secundária de relatos públicos. Não foram realizadas entrevistas por este módulo. As fontes primárias e os números citados pelo grupo precisam acompanhar a documentação de pesquisa; não são apresentados aqui como validados |
| Dados | Três ocorrências e quatro eventos de segurança fictícios; bairros “Bairro Teste Centro” e “Bairro Teste Norte” são fixtures, não bairros reais do Jabaquara. SP156 pertence à pesquisa/modelo de IA |
| Repositório | https://github.com/dipaula-victo/ZelaSP |

## Integrantes

| Integrante | RM |
|---|---|
| Djalma Moreira de Andrade Filho | 555530 |
| Felipe Paes de Barros Muller Carioba | 558447 |
| Lucas Rodrigues de Queiroz | 556323 |
| Victor Hugo de Paula | 554787 |

## Executar

Pré-requisito: Node.js 22.13 ou superior (validado com Node 24.22.1), npm e ambiente Expo compatível com SDK 58.

```sh
npm ci
npx expo start --clear
```

Abra o QR no cliente Expo compatível ou pressione `w` para a prévia web. Se seu Expo Go não suportar SDK 58, use um development build compatível; não altere versões isoladas de React/RN. Para compilação nativa local, use o ambiente Android/iOS apropriado. A prévia web não comprova câmera, permissões ou SQLite do dispositivo.

O `.env` é opcional: o padrão é mock. Para configurar, copie `.env.example` para `.env`. Reinicie o Expo com `--clear` ao mudar variáveis. Elas são públicas no bundle; nunca coloque segredos nelas.

## Jornada

1. Início: resumo dos protocolos, relato novo e acesso ao Risco Cidadão.
2. Novo relato: categoria, bairro e descrição obrigatórios. A foto não é obrigatória. Limite de UX de 2.000 caracteres.
3. Protocolo: descrição tratada, fotos, executor, alteração confirmada de status e histórico.
4. Ocorrências: busca, filtros combinados e opção de ver registros criados neste aparelho.
5. Risco Cidadão: pontuação, nível, contagens e alertas dos últimos sete dias.
6. Sobre e ajustes: contexto, equipe e cenários controlados de teste.

Não há login. Selecionar um executor não autentica a pessoa nem cria um controle de autorização.

## Banco e offline

Android/iOS: SQLite (`zelasp.db`), tabela `app_storage`, chave/valor JSON com UPSERT parametrizado. Guarda o estado do simulador, cópia de consultas, opções, protocolos criados no aparelho e indicador. Fotos nativas são copiadas para a pasta de documentos do app; a tabela referencia seu URI. Web: IndexedDB com o mesmo contrato, incluindo fotos como data URL.

Sem conexão, o app exibe apenas os dados já consultados e o aviso “Sem conexão — dados podem estar desatualizados”. Cadastro, foto e mudança de status ficam bloqueados. Não existe fila de envio, rascunho persistente nem repetição automática de POST. A versão R03 usa um banco novo; não importa rascunhos brutos das versões anteriores.

No modo API, a proteção definitiva cabe ao backend. O mobile não salva a descrição crua antes da resposta e recusa ocorrências que retornem dados pessoais reconhecidos sem mascaramento. No modo mock, o simulador trata o texto antes de gravá-lo. Não há OCR ou detecção de dados em fotos: use exclusivamente imagens de teste sem pessoas ou documentos.

## Dados e testes de demonstração

- Protocolos 1/2/3: aberta/em análise/resolvida, históricos com 0/1/2 alterações.
- Registro 2: “Descarte de entulho na calçada”, com imagem ilustrativa fictícia.
- Risco inicial: 25, MÉDIO, CPF=2, telefone=1, senha=1, bloqueios=0.
- Novo relato usando o CPF de teste `11144477735`: texto `[CPF OCULTADO]`, aviso de mascaramento, risco=31 e CPF=3.
- Executor de QA: `VictorHugo_RM554787`.
- O botão de restauração recompõe a seed e remove os registros fictícios da sessão após confirmação.

## Verificar

```sh
npm run typecheck
npm test
npx expo export --platform all --output-dir dist-all
```

34 testes passaram na R03. A exportação é um bundle JavaScript/Hermes, não um APK/IPA assinado e não comprova execução em dispositivo.

## Prints e vídeo

| Início | Ocorrências | Novo relato |
|---|---|---|
| <img width="384" height="833" alt="01-inicio" src="https://github.com/user-attachments/assets/81a3b4b2-4509-4adf-96ba-807d38ce2072" /> | <img width="384" height="833" alt="02-ocorrencias" src="https://github.com/user-attachments/assets/a61e7a23-6ef9-46b7-abba-668a965a18e8" /> | <img width="383" height="832" alt="03-novo-relato" src="https://github.com/user-attachments/assets/fbe27f05-4b01-4174-be29-0b918e43d8a3" />

| Risco Cidadão | Sobre e ajustes |
|---|---|
| <img width="383" height="834" alt="04-risco" src="https://github.com/user-attachments/assets/141eefac-10e4-40c0-82a8-8c3e86ad9ed9" /> | <img width="383" height="834" alt="05-ajustes" src="https://github.com/user-attachments/assets/01e9f828-a837-4c02-ba7d-cd2981694f2f" />


## Vídeo

▶️ **Demonstração do app (até 3 min)**: https://www.youtube.com/watch?v=sm76ImEubtk


## Integração e entrega

O contrato da API está em [backend/docs/API_CONTRACT.md](../backend/docs/API_CONTRACT.md).

A instrução da atividade enviada informa entrega por um integrante até **19/11, às 23h55**; o cabeçalho mostra 23h59. Adote 23h55 como prazo conservador. Repositório público ou com acesso liberado ao professor; commits posteriores ao prazo não contam.
