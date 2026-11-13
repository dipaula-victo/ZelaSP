# RELATÓRIO DE MACHINE LEARNING — ZelaSP
**Global Solution FIAP 2026.2 · CitizenTech · ODS 11 (Cidades e Comunidades Sustentáveis)**
Equipe: Djalma Moreira de Andrade Filho - RM 555530 | Felipe Paes de Barros Muller Carioba - RM 558447
 | Lucas Rodrigues de Queiroz - RM 556323  | Victor Hugo de Paula - RM 554787
 ## 1. Problema e pergunta de ML
 | O ZelaSP é um app de zeladoria urbana: moradores registram e acompanham ocorrências como lixeira cheia e descarte irregular. Esta entrega de ML é independente do app e usa dados reais do SP156 para perguntar:

> **Dado um pedido de limpeza urbana aberto no SP156, é possível prever se ele vai demorar mais que o normal para ser atendido?**

É um problema de **classificação binária supervisionada**. Ele se liga à meta 11.6 do ODS 11 (gestão de resíduos municipais): saber quando o atendimento tende a demorar ajuda moradores a acompanhar o pedido e a gestão a priorizar.

**Definição de atraso (`atraso = 1`):** pedido finalizado cujo tempo de atendimento, em dias, é maior que o percentil 75 dos pedidos do mesmo serviço. O limite é calculado só com o treino. Não é um prazo oficial, pois a base não traz prazos.

## 2. Dados
- **Fonte:** Dados do SP156, Portal de Dados Abertos da Prefeitura de SP (link e detalhes em `dados/README.md`).
- **Período:** pedidos abertos de 01/07/2026 a 30/09/2026; baixado em 13/11/2026.
- **Tamanho:** 484.232 linhas e 15 colunas. O tema "Lixo e limpeza" tem 57.776 pedidos (11,9%); sem 3.464 duplicadas exatas (6,0%), 54.312.
- **Tempo de atendimento** (finalizados, em dias): mediana 1, percentil 75 = 2, percentil 90 = 5, máximo 96. A base só traz a data, sem hora.
- **Limpeza:** datas lidas em `MM/DD/AAAA` (formato não documentado), status padronizados, textos sem acento, coordenadas fora de São Paulo descartadas, duplicadas removidas. Há outras diferenças entre o dicionário e a base, listadas em `dados/README.md`.

**Distrito do projeto: Jabaquara.** É o distrito com mais pedidos (2.238) e tem cauda longa de espera: mediana de 0 dia, mas percentil 90 de 16 dias. Isso deixa 340 pedidos no teste, o que dá segurança para olhar resultados por distrito. **Ressalva:** a base mede quem pediu, não onde o problema existe, e não traz população para normalizar o volume.

## 3. Modelo e preparação
**Modelo: Random Forest.** Os dados são tabulares, com categorias (serviço, distrito, canal) e números (coordenadas). Árvores lidam bem com essa mistura e com relações não lineares, dispensam padronizar escalas e a floresta reduz o overfitting de uma árvore isolada. Tem poucos hiperparâmetros e mostra a importância das variáveis. Descartamos LSTM (não há sequência por pessoa), MLP (pede mais dados e é pouco interpretável) e KMeans+PCA (não supervisionado).

**Variáveis:** serviço, assunto, meio de comunicação, distrito, prefeitura operacional, latitude, longitude e dia da semana de abertura.

**Quem entra no modelo:** 51.388 pedidos finalizados com tempo válido; destes, 39.378 abertos há pelo menos 30 dias antes da última finalização da base. Sem essa janela, só os pedidos rápidos de fim de setembro estariam finalizados e o modelo veria uma realidade mais otimista.

**Divisão treino/teste (80/20, semente 42):** por **endereço**, pois pedidos do mesmo endereço costumam fechar juntos. Treino: 31.523; teste: 7.855. Atrasos: 21,1% no treino e 21,5% no teste.

**Checagem de vazamento (todas passaram):** (1) nenhuma coluna proibida (finalização, tempo, status, órgão) entre as variáveis; (2) nenhum endereço nos dois lados; (3) nenhuma linha completa repetida entre treino e teste; (4) limite de atraso calculado só no treino; (5) nenhuma variável sozinha passa de 0,70 de AUC (a maior é o dia da semana, com 0,58).

## 4. Treino e hiperparâmetros
Busca em grade com **18 combinações de 3 hiperparâmetros**: nº de árvores (100, 300), profundidade máxima (8, 16, sem limite) e mínimo de pedidos por folha (1, 10, 30). **Validação cruzada de 5 dobras só no treino**, também agrupada por endereço, com F1 como métrica.

| Combinação (árvores / profundidade / folha) | F1 treino (dobras) | F1 validação (dobras) |
|---|---|---|
| **300 / sem limite / 10 (melhor)** | 0,497 | **0,409** |
| 300 / 16 / 10 | 0,438 | 0,400 |
| 300 / 8 / 1 | 0,421 | 0,393 |
| 100 / 16 / 1 | 0,590 | 0,387 |
| 100 / sem limite / 1 | 0,954 | 0,308 |

Árvores sem limite e com folhas de 1 pedido decoram o treino (F1 de 0,95), mas pioram na validação. O mínimo de 10 pedidos por folha foi o que mais ajudou.

## 5. Resultados em treino e teste
| Métrica | Treino | Teste | Sorteio (comparação) |
|---|---|---|---|
| Acurácia | 0,707 | 0,657 | 0,668 |
| Precisão | 0,390 | 0,329 | 0,221 |
| Recall | 0,679 | 0,571 | 0,215 |
| F1 | 0,495 | 0,417 | 0,218 |
| AUC | 0,777 | 0,680 | 0,503 |

*Gráficos (barras de treino x teste, matriz de confusão, curva ROC, validação cruzada e importância das variáveis): seções 6 e 7 do notebook.*

- A **acurácia engana**: "sempre prever no prazo" acertaria 78,5% e não detectaria nenhum atraso. Por isso usamos F1, recall e AUC.
- O modelo supera o sorteio: F1 de 0,42 contra 0,22, e AUC de 0,68 contra 0,50. Acerta cerca de 57% dos atrasos (recall), mas apenas um terço dos seus alertas são atrasos de verdade (precisão 0,33).
- **Overfitting moderado:** F1 cai de 0,495 (treino) para 0,417 (teste), uma diferença de 0,08, e o AUC cai de 0,777 para 0,680. O F1 de validação cruzada (0,409) é próximo ao do teste (0,417), o que indica que a escolha de hiperparâmetros não inflou o resultado.
- **Importância das variáveis:** o dia da semana foi o mais importante (queda de 0,075 no F1 ao embaralhar), seguido de prefeitura operacional (0,026), assunto (0,016) e meio de comunicação (0,015). Não investigamos por que o dia da semana pesa tanto.
- **Por distrito:** o modelo marca distritos inteiros como de risco (Sacomã: 92% de alertas para 39,7% de atrasos reais; Tremembé: 96% para 36,7%; Belém: 0% para 15,6%). Em Jabaquara, 42,9% de alertas para 22,6% de atrasos reais. Ou seja, o modelo exagera o risco em muitos distritos e não deve ser lido como estimativa da taxa de atraso.

## 6. Sensibilidade: o peso do calendário
Na primeira versão, o modelo incluía mês e dia do mês de abertura e chegou a AUC de 0,93. O diagnóstico mostrou que dez dias de abertura (02 a 09/07 e 01 e 02/08) concentram 12,4% dos pedidos e 52,7% dos atrasos, com taxa de atraso de cerca de 90% nesses dias, contra cerca de 5 a 20% nos demais. As finalizações também se concentram: 24/07 teve 2.765 (quase o dobro da segunda data, 05/08, com 1.458). A base não informa a causa. Nesses períodos o modelo aprendia **quando** o pedido foi aberto, não **onde ou como**. Por isso o modelo principal **não usa** mês nem dia do mês.

| Cenário (mesmos hiperparâmetros) | F1 | AUC |
|---|---|---|
| A. com calendário | 0,714 | 0,912 |
| B. sem calendário (**modelo principal**) | 0,417 | 0,680 |
| C. sem calendário e sem os dez dias atípicos | 0,503 | 0,770 |

Tirar os dias atípicos melhora o resultado (AUC 0,77), o que sugere que esses episódios acrescentam ruído e escondem o padrão por tipo de serviço, local e canal. O cenário C é só sensibilidade: os dias foram escolhidos olhando o resultado e os conjuntos de teste não são idênticos.

## 7. O que isso significa para o cidadão
- Em 7.855 pedidos que o modelo nunca tinha visto, **22% demoraram mais que o normal** para o mesmo tipo de serviço.
- Só com o tipo de serviço, o canal, o local e o dia da semana, **dá para prever a demora de forma modesta**, melhor que um sorteio, mas com erros: a cada 100 pedidos que atrasaram, o modelo avisou 57; a cada 100 avisos, 33 eram atrasos reais.
- **A demora dependeu muito de quando o pedido foi aberto**: com o calendário, o AUC sobe de 0,68 para 0,91. Neste trimestre, parte dos atrasos aparece em poucos períodos, e a base não diz por quê.
- Em Jabaquara, 22,6% dos pedidos do teste atrasaram, valor próximo à média da cidade, com alguns pedidos esperando muito mais (percentil 90 de 16 dias).
- **Uso responsável:** o modelo serve, no máximo, para apoiar o acompanhamento de pedidos. Não deve ser usado para negar ou adiar atendimento a ninguém.

## 8. Limitações
- Um único trimestre e uma única cidade; o resultado pode não valer para outros períodos.
- **Viés de registro:** o SP156 reflete quem pede, não onde há mais lixo; bairros com menos registros não têm necessariamente menos problemas. Sem população na base, o volume não foi normalizado.
- "Atraso" é relativo (mais lento que 3 em cada 4 pedidos do mesmo serviço) e curto: a mediana é de cerca de 1 dia. "Finalizada" pode significar fechamento do chamado e não a limpeza feita na rua.
- Sem número de protocolo, linhas idênticas foram tratadas como um único pedido.
- Pedidos ainda abertos (5,3% do tema) ficam fora do modelo, e o tempo é medido em dias inteiros.
- A base "Avaliação dos canais" (satisfação) não foi usada.

## 9. Reprodutibilidade
Notebook `ml/notebook_zelasp_ml.ipynb` (executado no Google Colab), semente fixa 42, dependências em `ml/requirements.txt` e dados descritos em `ml/dados/README.md`.
