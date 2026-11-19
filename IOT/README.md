# ZelaSP — Monitoramento de Lixeiras (IoT / Physical Computing)

Módulo de hardware simulado responsável por medir o nível de enchimento de lixeiras urbanas e enviar leituras automáticas para a plataforma **ZelaSP** (Global Solution FIAP 2026.2 — Engenharia de Software).

## Contexto na CitizenTech

No distrito do **Jabaquara** (São Paulo), moradores e comerciantes convivem diariamente com pontos viciados de descarte irregular e lixeiras transbordando. Além do registro manual feito pelo cidadão no aplicativo mobile, o módulo de **Physical Computing** automatiza o monitoramento físico das lixeiras (contribuindo com os **ODS 9 e 11** da ONU), enviando telemetria em tempo real para a API central abrir ocorrências preventivas antes do transbordamento.

## Link do projeto no Wokwi

https://wokwi.com/projects/480868029756689409

## Link do vídeo demonstrativo 

https://youtu.be/x6i10w2HIa4

## Hardware e Pinagem (ESP32 no Wokwi)

* **Microcontrolador:** ESP32 DevKit C v4
* **Sensor Ultrassônico (HC-SR04):** `TRIG` no GPIO 5 | `ECHO` no GPIO 18 | Alimentação 5V
* **LED Verde (Capacidade normal $\le$ 90%):** GPIO 26
* **LED Vermelho (Alerta de transbordamento $>$ 90%):** GPIO 27
* **Buzzer (Alerta sonoro local):** GPIO 25

## Regra de Negócio e Contrato de Integração

* **Profundidade útil da lixeira:** `40 cm`
* **Fórmula de cálculo do nível (%):** `(40 - distância_cm) / 40 * 100`
* **Gatilho de ocorrência automática:** Nível estritamente maior que `90%` (`> 90%`)
* **Comunicação e Contingência:** Como o *Private Gateway* do Wokwi é restrito ao plano pago, o firmware publica as leituras via **MQTT** no broker público `broker.hivemq.com` (porta `1883`, tópico `zelasp/RM558447/lixeira/1/leitura`), mantendo também a saída JSON no Monitor Serial como *fallback*.
* **Payload JSON (Contrato `POST /api/leituras`):**

```json
{
  "dispositivoId": 1,
  "nivelPercentual": 75,
  "lidaEm": "2026-11-19T18:00:00Z"
}