import React from "react";
import { View } from "react-native";
import { router } from "expo-router";
import {
  Page,
  Card,
  Txt,
  Button,
  Icon,
  Section,
  SkeletonCards,
  Notice,
  s,
} from "../../src/components/ui";
import { Connection } from "../../src/components/Connection";
import { OccurrenceCard } from "../../src/components/OccurrenceCard";
import { useApp } from "../../src/state/AppContext";
import { theme as t } from "../../src/theme";
import { dataMode } from "../../src/services/config";
export default function Home() {
  const { snapshot, offline, loading, error, refresh } = useApp();
  return (
    <Page title="ZelaSP" subtitle="Jabaquara · São Paulo">
      <Connection />
      <Card style={{ backgroundColor: t.color.dark, padding: 24, gap: 18 }}>
        <Icon name="leaf-outline" color={t.color.lime} size={36} />
        <Txt size={30} bold color={t.color.white}>
          Um bairro melhor começa com você.
        </Txt>
        <Txt color={t.color.white}>
          Registre um problema de zeladoria e acompanhe cada atualização, do
          relato à solução.
        </Txt>
        <Button
          label="Registrar ocorrência"
          icon="add-circle-outline"
          disabled={offline}
          onPress={() => router.push("/nova")}
        />
      </Card>
      {dataMode === "mock" && (
        <Txt muted size={12}>
          Demonstração acadêmica · bairros e registros de teste. Nenhum chamado
          é enviado à prefeitura.
        </Txt>
      )}
      <View style={s.wrap}>
        {(["ABERTA", "EM_ANALISE", "RESOLVIDA"] as const).map((status, i) => (
          <Card key={status} style={{ flexGrow: 1, flexBasis: "28%" }}>
            <Txt bold size={30}>
              {snapshot.rows.filter((x) => x.status === status).length}
            </Txt>
            <Txt muted size={12}>
              {["Abertas", "Em análise", "Resolvidas"][i]}
            </Txt>
          </Card>
        ))}
      </View>
      <Section
        title="Acompanhe o bairro"
        action="Ver todas"
        onAction={() => router.push("/ocorrencias")}
      />
      {!!error && !offline && (
        <Notice
          tone="error"
          text={error}
          action="Tentar novamente"
          onAction={() => void refresh()}
        />
      )}
      {loading ? (
        <SkeletonCards />
      ) : (
        snapshot.rows
          .slice()
          .sort((a, b) => b.criadaEm.localeCompare(a.criadaEm))
          .slice(0, 3)
          .map((item) => <OccurrenceCard key={item.id} item={item} />)
      )}
      <Card>
        <View style={s.row}>
          <Icon name="shield-checkmark-outline" />
          <Txt bold>Privacidade em primeiro lugar</Txt>
        </View>
        <Txt muted>
          Use apenas dados fictícios nesta demonstração. Consulte os alertas
          sobre dados pessoais detectados nos relatos.
        </Txt>
        <Button
          secondary
          label="Consultar Risco Cidadão"
          onPress={() => router.push("/risco")}
        />
      </Card>
    </Page>
  );
}
