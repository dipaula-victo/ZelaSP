import React from "react";
import { Pressable, View } from "react-native";
import { router } from "expo-router";
import { Txt, Icon, s } from "./ui";
import { theme as t } from "../theme";
import { statusLabel, type Status, type Occurrence } from "../data/types";
import { useApp } from "../state/AppContext";
import { dateLabel } from "../utils/validation";
export function Badge({ status }: { status: Status }) {
  return (
    <View
      style={{
        alignSelf: "flex-start",
        backgroundColor:
          status === "ABERTA"
            ? t.color.orangeBg
            : status === "EM_ANALISE"
              ? t.color.blueBg
              : t.color.soft,
        paddingHorizontal: 12,
        paddingVertical: 6,
        borderRadius: 30,
      }}
    >
      <Txt
        bold
        size={12}
        color={
          status === "ABERTA"
            ? t.color.orange
            : status === "EM_ANALISE"
              ? t.color.blue
              : t.color.primary
        }
      >
        {statusLabel[status]}
      </Txt>
    </View>
  );
}
export function OccurrenceCard({ item }: { item: Occurrence }) {
  const { snapshot } = useApp();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`Protocolo ${item.id}. ${snapshot.categorias.find((c) => c.id === item.categoriaId)?.nome}. Ver detalhes`}
      onPress={() =>
        router.push({ pathname: "/ocorrencia/[id]", params: { id: item.id } })
      }
      style={({ pressed }) => [s.card, pressed && s.pressed]}
    >
      <View
        style={[s.row, { justifyContent: "space-between", flexWrap: "wrap" }]}
      >
        <Txt muted size={12}>
          #ZSP-{item.id} · {dateLabel(item.criadaEm)}
        </Txt>
        <Badge status={item.status} />
      </View>
      <Txt bold size={18}>
        {snapshot.categorias.find((c) => c.id === item.categoriaId)?.nome ??
          "Ocorrência"}
      </Txt>
      <View style={s.row}>
        <Icon name="location-outline" size={16} />
        <Txt muted size={14}>
          {snapshot.bairros.find((b) => b.id === item.bairroId)?.nome ??
            "Bairro"}
        </Txt>
      </View>
      <Txt>{item.descricao}</Txt>
      <View style={[s.row, { justifyContent: "space-between" }]}>
        <Txt muted size={12}>
          {item.origem === "SENSOR"
            ? "Monitoramento por sensor"
            : "Relato do cidadão"}
        </Txt>
        <Icon name="arrow-forward" />
      </View>
    </Pressable>
  );
}
