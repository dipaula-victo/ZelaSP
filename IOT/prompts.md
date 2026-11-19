# Registro de Uso de IA — Módulo IoT (ZelaSP)

Este arquivo documenta as consultas pontuais feitas com apoio de Inteligência Artificial durante o desenvolvimento e a validação do protótipo no Wokwi[cite: 9, 31].

## 1. Formatação de data UTC (NTP) e payload MQTT no ESP32
* **Prompt utilizado:** *"Como formatar a data e hora atual via NTP em string ISO-8601 UTC (`YYYY-MM-DDTHH:MM:SSZ`) no ESP32 usando `time.h` e montar um JSON simples para publicar via `PubSubClient`?"*
* **Finalidade:** Garantir que o campo `lidaEm` enviado pelo microcontrolador seguisse exatamente o padrão UTC esperado pelo contrato da API (`POST /api/leituras`)[cite: 6].
* **Como foi validado:** O trecho foi adaptado para as variáveis do projeto, compilado no Wokwi e validado acompanhando a saída gerada no Monitor Serial até o relógio sincronizar corretamente.

## 2. Revisão das conexões do circuito (`diagram.json`)
* **Prompt utilizado:** *"Revise este arquivo `diagram.json` do Wokwi para conferir se os pinos de alimentação e sinal do sensor ultrassônico HC-SR04, dois LEDs e um buzzer estão mapeados corretamente no ESP32 DevKit C v4."*
* **Finalidade:** Evitar inversão de pinos (`TRIG`/`ECHO` e `GND`) antes de testar a lógica de alerta local.
* **Como foi validado:** Simulação executada diretamente no Wokwi, alterando a distância do sensor HC-SR04 de `10 cm` (LED verde ativo) para `2 cm` (LED vermelho e buzzer ativos) para confirmar a resposta física do circuito.

## 3. Revisão do README e organização do roteiro de apresentação
* **Prompt utilizado:** *"Com base na estrutura do nosso código IoT e nos requisitos de entrega da disciplina, sugira uma organização objetiva para o `README.md` da pasta `iot/` e um roteiro de até 4 minutos para a demonstração."*
* **Finalidade:** Padronizar a documentação da pasta `iot/` com o restante do repositório da equipe e manter o vídeo demonstrativo dentro do tempo limite da entrega[cite: 31, 48].
* **Como foi validado:** Os textos foram revisados manualmente para refletir fielmente os pinos, o tópico MQTT e o comportamento real gravado no vídeo de demonstração.