# Dados — SP156 (Prefeitura de São Paulo)

## Fonte
- **Conjunto:** Dados do SP156 — solicitações de serviços, arquivo do 3º trimestre de 2026
- **Portal:** Dados Abertos da Prefeitura de São Paulo — https://dados.prefeitura.sp.gov.br/dataset/dados-do-sp156
- **Arquivo usado:** `arquivo-final-3-trim-26.csv`
- **Dicionário:** *Glossário da Base de Dados do SP156 – 2020 em diante* (atualizado em 10/08/2026)
- **Baixado em:** 13/11/2026
- **Período:** pedidos abertos de 01/07/2026 a 30/09/2026 (última finalização registrada: 08/10/2026)
- **Dados reais e públicos.** Nenhum dado simulado foi usado.

## Tamanho e recorte
| Etapa | Linhas |
|---|---|
| Base completa | 484.232 (15 colunas) |
| Tema "Lixo e limpeza" | 57.776 (11,9% da base) |
| Sem duplicadas exatas (3.464 removidas) | 54.312 |
| Finalizados com tempo válido | 51.388 |
| Usados no modelo (abertos há ≥ 30 dias da data de corte) | 39.378 |
| Treino / Teste | 31.523 / 7.855 |

## Colunas
Data de Abertura, Data de Finalização, Meio de Comunicação, Tema, Assunto, Serviço, Status, Órgão, Logradouro, Número, CEP, Latitude, Longitude, Prefeitura Operacional, Distrito.

Usadas como variáveis do modelo: Serviço, Assunto, Meio de Comunicação, Distrito, Prefeitura Operacional, Latitude, Longitude e dia da semana de abertura. As datas de abertura e finalização servem para calcular o tempo de atendimento (alvo), e Logradouro e Número, para agrupar pedidos do mesmo endereço na divisão treino/teste. Status e Órgão **não** entram no modelo (retrato do dia da extração, risco de vazamento).

## Particularidades da base (diferenças em relação ao dicionário)
- Datas em **MM/DD/AAAA**, formato não documentado; o dicionário também fala em hora, mas a base só traz a data.
- Colunas **Latitude** e **Longitude** presentes, embora o dicionário as descreva apenas na aba de pesquisa de satisfação.
- A coluna "Subprefeitura" do dicionário aparece como **"Prefeitura Operacional"**.
- Meios de comunicação diferentes dos listados no dicionário (a base usa PORTAL, CRM, MOBILE, INTEGRACAO).
- Status com grafia diferente (`Finalizada`, `Aberto`, `Em Andamento`) e o dicionário cita "quatro status" mas lista cinco.
- **Sem número de protocolo:** linhas 100% idênticas foram tratadas como o mesmo pedido.
- Cerca de 37% das linhas da base completa não têm endereço (temas sem local, como Educação e Finanças).

## Como reproduzir
1. Baixe o CSV do portal (o arquivo tem cerca de 100 MB e **não** está versionado neste repositório).
2. Abra `ml/notebook_zelasp_ml.ipynb` no Google Colab.
3. Execute todas as células e envie o CSV quando o notebook pedir (ou preencha `CAMINHO_CSV`).
4. Instale as dependências, se rodar localmente: `pip install -r requirements.txt`.
