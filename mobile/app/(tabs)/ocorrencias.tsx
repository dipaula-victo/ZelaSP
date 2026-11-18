import React, { useState } from "react";
import { TextInput, View } from "react-native";
import { router } from "expo-router";
import {
  Page,
  Txt,
  Button,
  Chip,
  Empty,
  Notice,
  SkeletonCards,
  s,
} from "../../src/components/ui";
import { OccurrenceCard } from "../../src/components/OccurrenceCard";
import { Connection } from "../../src/components/Connection";
import { useApp } from "../../src/state/AppContext";
import { statusLabel, type Status } from "../../src/data/types";
export default function List() {
  const { snapshot, loading, error, offline, refresh } = useApp();
  const [query, setQuery] = useState(""),
    [status, setStatus] = useState<Status | null>(null),
    [bairro, setBairro] = useState<number | null>(null),
    [category, setCategory] = useState<number | null>(null),
    [mine, setMine] = useState(false),
    [filters, setFilters] = useState(false);
  const norm = (v: string) =>
    v
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();
  const rows = snapshot.rows
    .filter(
      (x) =>
        (!mine || snapshot.myIds.includes(x.id)) &&
        (!status || x.status === status) &&
        (!bairro || x.bairroId === bairro) &&
        (!category || x.categoriaId === category) &&
        norm(
          `${x.descricao} ZSP-${x.id} ${snapshot.categorias.find((c) => c.id === x.categoriaId)?.nome}`,
        ).includes(norm(query)),
    )
    .sort((a, b) => b.criadaEm.localeCompare(a.criadaEm));
  return (
    <Page title="Ocorrências" subtitle="Veja o que está acontecendo no bairro">
      <Connection />
      <Button
        label="Nova ocorrência"
        icon="add-outline"
        disabled={offline}
        onPress={() => router.push("/nova")}
      />
      <TextInput
        style={s.field}
        value={query}
        onChangeText={setQuery}
        accessibilityLabel="Buscar ocorrências"
        placeholder="Buscar por relato, categoria ou protocolo"
      />
      <View style={s.wrap}>
        <Chip label="Todas" selected={!mine} onPress={() => setMine(false)} />
        <Chip
          label="Criadas neste aparelho"
          selected={mine}
          onPress={() => setMine(true)}
        />
        <Chip
          label="Filtros"
          selected={filters}
          onPress={() => setFilters(!filters)}
        />
      </View>
      {filters && (
        <>
          <Txt bold>Status</Txt>
          <View style={s.wrap}>
            <Chip
              label="Todos os status"
              selected={!status}
              onPress={() => setStatus(null)}
            />
            {Object.entries(statusLabel).map(([id, label]) => (
              <Chip
                key={id}
                label={label}
                selected={status === id}
                onPress={() => setStatus(id as Status)}
              />
            ))}
          </View>
          <Txt bold>Bairro</Txt>
          <View style={s.wrap}>
            <Chip
              label="Todos os bairros"
              selected={!bairro}
              onPress={() => setBairro(null)}
            />
            {snapshot.bairros.map((x) => (
              <Chip
                key={x.id}
                label={x.nome}
                selected={bairro === x.id}
                onPress={() => setBairro(x.id)}
              />
            ))}
          </View>
          <Txt bold>Categoria</Txt>
          <View style={s.wrap}>
            <Chip
              label="Todas as categorias"
              selected={!category}
              onPress={() => setCategory(null)}
            />
            {snapshot.categorias.map((x) => (
              <Chip
                key={x.id}
                label={x.nome}
                selected={category === x.id}
                onPress={() => setCategory(x.id)}
              />
            ))}
          </View>
        </>
      )}
      {!!error && !offline && <Notice text={error} tone="error" />}
      <Button
        secondary
        label="Atualizar lista"
        busy={loading}
        disabled={offline}
        onPress={() => void refresh()}
      />
      {loading ? (
        <SkeletonCards />
      ) : rows.length ? (
        rows.map((item) => <OccurrenceCard key={item.id} item={item} />)
      ) : (
        <Empty
          title="Nenhuma ocorrência encontrada para este filtro."
          text="Altere os filtros ou registre uma ocorrência quando estiver conectado."
        />
      )}
    </Page>
  );
}
