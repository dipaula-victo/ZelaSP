import React, { useState } from "react";
import { View, Switch } from "react-native";
import { Page, Card, Txt, Button, Notice, s } from "../../src/components/ui";
import { useApp } from "../../src/state/AppContext";
import { dataMode, testMode } from "../../src/services/config";
import type { Settings } from "../../src/data/types";
export default function SettingsPage() {
  const app = useApp(),
    [confirm, setConfirm] = useState(false),
    [error, setError] = useState("");
  const options: [keyof Settings, string, string][] = [
    ["offline", "Sem conexão", "Consulta somente aos dados já salvos."],
    [
      "fail",
      "Falha do serviço",
      "Simula indisponibilidade e permite testar nova tentativa.",
    ],
    ["empty", "Lista vazia", "Retorna uma consulta sem ocorrências."],
    ["emptyRisk", "Risco sem eventos", "Consulta de risco sem detecções."],
    [
      "rollback",
      "Falha na alteração de status",
      "Nenhuma alteração é persistida no simulador.",
    ],
    [
      "unsafeLog",
      "Tentativa de log inseguro",
      "Gera evento de bloqueio ao detectar dados no relato.",
    ],
    [
      "trace",
      "Rastrear fluxo",
      "Exibe somente tipo e destino; nunca o valor original.",
    ],
  ];
  async function update(key: keyof Settings, value: boolean) {
    setError("");
    try {
      await app.setSettings({ ...app.settings, [key]: value });
    } catch {
      setError("Não foi possível salvar o ajuste.");
    }
  }
  return (
    <Page title="Sobre e ajustes" subtitle="ZelaSP · Global Solution 2026.2">
      <Card>
        <Txt bold size={22}>
          Zeladoria próxima do cidadão
        </Txt>
        <Txt>
          Projeto acadêmico para o distrito do Jabaquara, em São Paulo. Registre
          problemas, acompanhe protocolos e consulte o Risco Cidadão.
        </Txt>
        <Txt muted>
          Modo atual:{" "}
          {dataMode === "mock"
            ? "demonstração com dados fictícios"
            : "API do projeto"}
          .
        </Txt>
        <Txt muted>
          Sem vínculo com a prefeitura. O app não possui login; a seleção de
          executor não autentica identidade.
        </Txt>
      </Card>
      {testMode && (
        <>
          <Txt bold size={18}>
            Cenários de teste
          </Txt>
          {options.map(([key, label, description]) => (
            <Card key={key}>
              <View style={s.row}>
                <View style={s.flex}>
                  <Txt bold>{label}</Txt>
                  <Txt muted size={14}>
                    {description}
                  </Txt>
                </View>
                <Switch
                  accessibilityLabel={label}
                  value={app.settings[key]}
                  disabled={app.loading || app.busy}
                  onValueChange={(value) => void update(key, value)}
                />
              </View>
            </Card>
          ))}
        </>
      )}
      {!!error && <Notice tone="error" text={error} />}
      {dataMode === "mock" &&
        (confirm ? (
          <Card>
            <Txt>
              Restaurar os três registros e os quatro eventos iniciais? Os
              relatos de teste criados neste aparelho serão removidos.
            </Txt>
            <Button
              label="Confirmar restauração"
              onPress={() => {
                void app
                  .reset()
                  .then(() => setConfirm(false))
                  .catch(() => setError("Falha ao restaurar."));
              }}
            />
            <Button
              secondary
              label="Cancelar"
              onPress={() => setConfirm(false)}
            />
          </Card>
        ) : (
          <Button
            secondary
            label="Restaurar demonstração"
            onPress={() => setConfirm(true)}
          />
        ))}
      <Card>
        <Txt bold>Equipe</Txt>
        <Txt>Djalma Moreira de Andrade Filho · RM 555530</Txt>
        <Txt>Felipe Paes de Barros Muller Carioba · RM 558447</Txt>
        <Txt>Lucas Rodrigues de Queiroz · RM 556323</Txt>
        <Txt>Victor Hugo de Paula · RM 554787</Txt>
      </Card>
      <Txt muted size={12}>
        Versão 1.2.0 · SQLite no Android/iOS · IndexedDB na prévia web
      </Txt>
    </Page>
  );
}
