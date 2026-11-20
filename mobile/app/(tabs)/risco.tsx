import React, { useState, useCallback, useRef } from "react";
import { View } from "react-native";
import { useFocusEffect } from "expo-router";
import {
  Page,
  Card,
  Txt,
  Button,
  Icon,
  Notice,
  SkeletonCards,
  s,
} from "../../src/components/ui";
import { useApp } from "../../src/state/AppContext";
import { RISK_UNAVAILABLE } from "../../src/services/errors";
import { dataMode } from "../../src/services/config";
import { dateTime } from "../../src/utils/validation";
import { parseCachedRisk } from "../../src/services/contract";
import { theme as t } from "../../src/theme";
export default function Risk() {
  const app = useApp(),
    [loading, setLoading] = useState(false),
    [error, setError] = useState("");
  // Cached state is untrusted too; never render its arbitrary fields/text directly.
  const risk = parseCachedRisk(app.snapshot.risk);
  const updatedAt = typeof app.snapshot.riskUpdatedAt === "string" &&
    Number.isFinite(Date.parse(app.snapshot.riskUpdatedAt))
    ? new Date(app.snapshot.riskUpdatedAt).toISOString() : null;
  const request = useRef(0);
  async function load() {
    const ticket = ++request.current;
    setLoading(true);
    setError("");
    try {
      if (app.offline) throw new Error("offline");
      await app.refreshRisk();
    } catch {
      if (ticket === request.current) setError(RISK_UNAVAILABLE);
    } finally {
      if (ticket === request.current) setLoading(false);
    }
  }
  useFocusEffect(
    useCallback(() => {
      void load();
      return () => {
        request.current++;
        app.cancelRisk();
      };
    }, [app.offline, app.settings.fail, app.settings.emptyRisk]),
  );
  return (
    <Page
      title="Risco Cidadão"
      subtitle="Entenda como seus dados são protegidos"
    >
      {app.offline && <Notice text="Sem conexão — o último indicador disponível pode estar desatualizado." />}
      {dataMode === "mock" && (
        <Txt muted size={12}>
          Demonstração com eventos de teste. Os indicadores são recalculados a
          cada relato.
        </Txt>
      )}
      {!!error && <Notice text={error} tone="error" />}
      {!!error && !!risk && (
        <Notice text="Exibindo o último resultado validado. Os indicadores podem estar desatualizados." />
      )}
      {loading ? (
        <SkeletonCards />
      ) : risk ? (
        <>
          <Card style={{ backgroundColor: t.color.dark, padding: 24 }}>
            <Icon
              name="shield-checkmark-outline"
              color={t.color.lime}
              size={36}
            />
            <Txt color={t.color.white} bold size={18}>
              Exposição nos últimos 7 dias
            </Txt>
            <View style={s.row}>
              <Txt size={48} bold color={t.color.white}>
                {risk.pontuacao}
              </Txt>
              <Txt color={t.color.white}>
                / 100 · {risk.nivel === "MEDIO" ? "MÉDIO" : risk.nivel}
              </Txt>
            </View>
            <View
              accessible
              accessibilityLabel={`Pontuação ${risk.pontuacao} de 100`}
              style={{
                height: 10,
                backgroundColor: t.color.muted,
                borderRadius: 8,
                overflow: "hidden",
              }}
            >
              <View
                style={{
                  width: `${risk.pontuacao}%`,
                  height: 10,
                  backgroundColor: t.color.lime,
                }}
              />
            </View>
            <Txt color={t.color.white} size={14}>
              Mede dados detectados nos relatos do sistema. Não representa
              perigo no bairro.
            </Txt>
          </Card>
          <View style={s.wrap}>
            {Object.entries(risk.deteccoes).map(([type, count]) => (
              <Card key={type} style={{ flexGrow: 1, flexBasis: "28%" }}>
                <Txt bold size={28}>
                  {count}
                </Txt>
                <Txt muted size={12}>
                  {type === "saude" ? "SAÚDE" : type.toUpperCase()}
                </Txt>
              </Card>
            ))}
          </View>
          <Card>
            <Txt bold>Explicação cidadã</Txt>
            {risk.explicacao ? (
              <>
                <Txt muted size={12}>
                  {dataMode === "mock"
                    ? "Origem: fallback de demonstração — sem chamada ao LLM"
                    : risk.explicacao.origem === "LLM"
                      ? "Origem: LLM — explicação retornada pela API"
                      : "Origem: fallback determinístico da API"}
                </Txt>
                <Txt>{risk.explicacao.texto}</Txt>
              </>
            ) : (
              <Txt muted>Este registro antigo não contém explicação. Atualize os indicadores.</Txt>
            )}
            {risk.janela && (
              <Txt muted size={12}>
                Janela UTC: {risk.janela.inicioUtc} até {risk.janela.fimUtc}
              </Txt>
            )}
          </Card>
          <Card>
            <Txt bold>
              {risk.vazamentosBloqueados} vazamento(s) bloqueado(s)
            </Txt>
            <Txt muted size={14}>
              Tentativas impedidas de enviar dados para um local inseguro.
            </Txt>
          </Card>
          {risk.alertas.length ? (
            risk.alertas.map((alert) => (
              <Notice key={alert.tipo} text={alert.texto} />
            ))
          ) : (
            <Notice text="Nenhuma detecção de dados pessoais nos últimos 7 dias" />
          )}
          {!!updatedAt && (
            <Txt muted size={12}>
              Atualizado em {dateTime(updatedAt!)}
            </Txt>
          )}
        </>
      ) : (
        !error && (
          <Notice text="Carregue os indicadores para consultar o risco." />
        )
      )}
      <Button
        label="Atualizar indicadores"
        busy={loading}
        disabled={app.offline}
        onPress={() => void load()}
      />
      <Card>
        <Txt bold>Como a pontuação funciona</Txt>
        <Txt muted size={14}>
          CPF: 6 · Telefone: 3 · E-mail: 3 · Senha: 10 · Saúde: 8. Cada
          vazamento bloqueado soma 15 pontos, com limite de 100. Baixo: 0–19;
          médio: 20–49; alto: 50–100.
        </Txt>
      </Card>
    </Page>
  );
}
