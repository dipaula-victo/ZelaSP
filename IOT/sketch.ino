#include <WiFi.h>
#include <PubSubClient.h>
#include <time.h>

// ====== CONFIGURAÇÃO ======
const char* WIFI_SSID = "Wokwi-GUEST";   // rede Wi-Fi simulada do Wokwi
const char* WIFI_PASS = "";
const char* MQTT_HOST = "broker.hivemq.com";
const int   MQTT_PORT = 1883;

const char* MQTT_TOPIC = "zelasp/RM558447/lixeira/1/leitura";

const int   DISPOSITIVO_ID = 1;           // "Lixeira Teste 01" (carga fictícia)
const float PROFUNDIDADE_CM = 40.0;       // profundidade útil da lixeira
const float LIMITE_PERCENTUAL = 90.0;     // acima disso = ocorrência automática
const unsigned long INTERVALO_MS = 3000;  // lê e envia a cada 3 segundos

// ====== PINOS ======
const int PIN_TRIG = 5;
const int PIN_ECHO = 18;
const int PIN_LED_VERDE = 26;
const int PIN_LED_VERMELHO = 27;
const int PIN_BUZZER = 25;

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);
unsigned long ultimoEnvio = 0;

// Mede a distância (cm) entre o sensor (na tampa) e o lixo.
float medirDistanciaCm() {
  digitalWrite(PIN_TRIG, LOW);
  delayMicroseconds(2);
  digitalWrite(PIN_TRIG, HIGH);
  delayMicroseconds(10);
  digitalWrite(PIN_TRIG, LOW);
  long duracao = pulseIn(PIN_ECHO, HIGH, 30000);
  if (duracao == 0) return -1;            // sem leitura válida
  return duracao * 0.0343 / 2.0;
}

// Fórmula do contrato: nível (%) = (40 - distância) / 40 * 100
float calcularNivel(float distanciaCm) {
  if (distanciaCm < 0) return -1;
  if (distanciaCm > PROFUNDIDADE_CM) distanciaCm = PROFUNDIDADE_CM;
  return (PROFUNDIDADE_CM - distanciaCm) / PROFUNDIDADE_CM * 100.0;
}

// Data/hora em UTC, no formato ISO usado pela API.
String agoraISO() {
  struct tm t;
  if (!getLocalTime(&t, 2000)) return "1970-01-01T00:00:00Z";
  char buf[25];
  strftime(buf, sizeof(buf), "%Y-%m-%dT%H:%M:%SZ", &t);
  return String(buf);
}

void conectarWifi() {
  Serial.print("Conectando ao Wi-Fi");
  WiFi.begin(WIFI_SSID, WIFI_PASS, 6);
  unsigned long inicio = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - inicio < 10000) {
    delay(300);
    Serial.print(".");
  }
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println(" conectado!");
    configTime(0, 0, "pool.ntp.org");     // sincroniza o relógio (UTC)
  } else {
    Serial.println(" falhou (seguindo só com o monitor serial).");
  }
}

void conectarMqtt() {
  if (WiFi.status() != WL_CONNECTED || mqtt.connected()) return;
  String id = "zelasp-esp32-" + String(random(0xFFFF), HEX);
  Serial.print("Conectando ao broker MQTT... ");
  if (mqtt.connect(id.c_str())) Serial.println("ok");
  else Serial.println("falhou, tento de novo no próximo ciclo.");
}

void setup() {
  Serial.begin(115200);
  pinMode(PIN_TRIG, OUTPUT);
  pinMode(PIN_ECHO, INPUT);
  pinMode(PIN_LED_VERDE, OUTPUT);
  pinMode(PIN_LED_VERMELHO, OUTPUT);
  pinMode(PIN_BUZZER, OUTPUT);

  Serial.println("=== ZelaSP - Sensor de lixeira ===");
  conectarWifi();
  mqtt.setServer(MQTT_HOST, MQTT_PORT);
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) conectarWifi();
  conectarMqtt();
  mqtt.loop();

  if (millis() - ultimoEnvio < INTERVALO_MS) return;
  ultimoEnvio = millis();

  float distancia = medirDistanciaCm();
  float nivel = calcularNivel(distancia);
  if (nivel < 0) {
    Serial.println("Leitura inválida do sensor.");
    return;
  }

  // Alerta local: acima de 90% (estritamente maior, como no contrato)
  bool cheia = nivel > LIMITE_PERCENTUAL;
  digitalWrite(PIN_LED_VERMELHO, cheia ? HIGH : LOW);
  digitalWrite(PIN_LED_VERDE, cheia ? LOW : HIGH);
  if (cheia) {
    tone(PIN_BUZZER, 1000, 300);
    Serial.println("[ALERTA] Lixeira cheia: a API abriria uma ocorrencia automatica.");
  }

  // Monta o JSON no formato do contrato (POST /api/leituras)
  char payload[128];
  snprintf(payload, sizeof(payload),
           "{\"dispositivoId\":%d,\"nivelPercentual\":%d,\"lidaEm\":\"%s\"}",
           DISPOSITIVO_ID, (int)round(nivel), agoraISO().c_str());

  Serial.print("Distancia: ");
  Serial.print(distancia, 1);
  Serial.println(" cm");
  Serial.println(payload);                // fallback: JSON no monitor serial

  if (mqtt.connected()) {
    mqtt.publish(MQTT_TOPIC, payload);
    Serial.println("-> publicado no MQTT");
  }
}