# Revisão estática do relato fictício

Estado: **INSPEÇÃO ESTÁTICA, SEM EXECUTAR DETECTOR OU API**. A quantidade de eventos não foi comprovada por execução nesta fase.

Fontes: `amostras/relato-pii.template.txt`, expectativa em `amostras/relato-pii-protegido.esperado.txt` e regras versionadas de `Application/ProtecaoPii.cs`. Substituição de `{MARCADOR}` por qualquer marcador válido tem comprimento fixo de 45 caracteres. Após trim, entrada planejada: **173 caracteres**; expectativa protegida: **186 caracteres**, ambos inferiores a 4000. O marcador de exemplo usado somente no cálculo de comprimento não identifica execução de banco.

| Trecho sintético | Categoria prevista | Inspeção estática |
|---|---|---|
| 12345678909 | CPF | Onze dígitos. Para base 123456789, soma ponderada 210/resto 1 dá primeiro verificador 0; segunda soma 255/resto 2 dá 9. Não é sequência repetida. Validade matemática não comprova inexistência: o número pode estar atribuído a uma pessoa. Não houve conferência de existência/titularidade; não o apresentar como exclusivamente fictício. |
| (11) 91234-0000 | TELEFONE | DDD 11 presente na lista, nove dígitos iniciando 9, formato convencional; exemplo sem pessoa identificada; não foi verificado se o número está em uso. |
| demo@example.test | EMAIL | Formato ASCII reconhecido, domínio reservado para exemplo; nenhuma consulta externa foi feita. |
| senha: SegredoFicticio123; | SENHA | Marcador previsto, um valor exclusivamente fictício, terminado por ponto e vírgula. Não absorve o termo de saúde seguinte. |
| diabetes. | SAUDE | Um termo da lista, palavra completa; não descreve uma pessoa. |

Não há outro termo de saúde no relato. CPF e telefone estão separados por pontuação; e-mail não contém termo de saúde ou número de contato. O GUID N de 32 caracteres é um token alfanumérico inteiro: limites do detector impedem tratar substrings numéricas internas como números isolados; o prefixo TASK007-DEMO não é marcador de senha. Não há sobreposição intencional entre as cinco regiões.

Previsão: cinco trechos selecionados, cinco categorias distintas e cinco EVENTO_SEGURANCA (PII_DETECTADO, API_OCORRENCIAS, BANCO, MASCARADO). Isso deriva da leitura do código, **não de uma chamada ao motor, teste ou persistência**. Uma divergência futura exige parar; não ajustar os eventos diretamente, alterar data/status ou reenviar POST para "completar" a contagem.

A transição de status e os uploads não devem gerar eventos adicionais; A e C não contêm PII planejada. Cinco detecções somam 30 pontos brutos novos, não necessariamente o total global. A referência protegida é escrita manualmente como expectativa; nunca renomeá-la como resposta observada.

## Correlação operacional — pendente de validação real

EVENTO_SEGURANCA não possui FK para OCORRENCIA. A atribuição dos cinco eventos é operacional, nunca garantida pelo schema. Capturar os IDs antes da criação de B e confrontar com o snapshot posterior, em janela exclusiva sem outros escritores. Exigir exatamente cinco IDs novos e conferir categorias/metadados; nenhum ID do baseline pode ser atribuído ao lote.

A igualdade exata entre timestamps persistidos depende de validação real. O SQL 03 apresenta os horários e interrompe diante de diferença; igualdade sozinha não prova associação. Qualquer divergência exige interrupção e revisão manual. Nenhum evento pode ser atribuído arbitrariamente, nem timestamp alterado para satisfazer verificações. Ver procedimento em sql/PROCEDIMENTO_EXECUCAO.md.
