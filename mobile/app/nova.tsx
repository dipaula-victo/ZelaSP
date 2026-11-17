import React, { useState } from "react";
import { TextInput, View } from "react-native";
import { router } from "expo-router";
import {
  Page,
  Txt,
  Button,
  Chip,
  CategoryTile,
  Notice,
  StepProgress,
  s,
} from "../src/components/ui";
import { Connection } from "../src/components/Connection";
import { useApp } from "../src/state/AppContext";
import { validateDescription } from "../src/utils/validation";
import { ServiceError } from "../src/services/errors";
export default function New() {
  const { snapshot, create, offline, busy } = useApp();
  const [bairro, setBairro] = useState(0),
    [category, setCategory] = useState(0),
    [text, setText] = useState(""),
    [error, setError] = useState(""),
    [uncertain, setUncertain] = useState(false);
  async function submit() {
    setError("");
    const invalid = validateDescription(text);
    if (invalid || !bairro || !category) {
      setError(invalid || "Selecione uma categoria e um bairro.");
      return;
    }
    try {
      const row = await create({
        bairroId: bairro,
        categoriaId: category,
        descricao: text,
        origem: "APP",
      });
      setText("");
      router.replace({
        pathname: "/ocorrencia/[id]",
        params: { id: row.id, created: "1" },
      });
    } catch (e) {
      setError(e instanceof Error ? e.message : "Não foi possível enviar.");
      if (e instanceof ServiceError && e.uncertain) {
        setText("");
        setUncertain(true);
      }
    }
  }
  return (
    <Page title="Novo relato" subtitle="Conte o que precisa de cuidado">
      <Button
        secondary
        label="Voltar para ocorrências"
        onPress={() => router.replace("/ocorrencias")}
      />
      <Connection />
      <StepProgress
        total={3}
        completed={
          Number(!!category) + Number(!!bairro) + Number(!!text.trim())
        }
      />
      <Txt bold>1. Qual é o problema?</Txt>
      <View style={s.wrap}>
        {snapshot.categorias.map((x) => (
          <CategoryTile
            key={x.id}
            label={x.nome}
            selected={category === x.id}
            onPress={() => setCategory(x.id)}
            icon={
              x.id === 4
                ? "bulb-outline"
                : x.id === 3
                  ? "warning-outline"
                  : "trash-outline"
            }
          />
        ))}
      </View>
      <Txt bold>2. Onde aconteceu?</Txt>
      <View style={s.wrap}>
        {snapshot.bairros.map((x) => (
          <Chip
            key={x.id}
            label={x.nome}
            selected={bairro === x.id}
            onPress={() => setBairro(x.id)}
          />
        ))}
      </View>
      <Txt bold>3. Descreva a situação</Txt>
      <TextInput
        style={[s.field, { minHeight: 150, textAlignVertical: "top" }]}
        multiline
        maxLength={2000}
        value={text}
        onChangeText={setText}
        editable={!busy && !uncertain}
        accessibilityLabel="Descrição"
        placeholder="Ex.: lixeira cheia perto da praça, na Rua Exemplo."
      />
      <Txt muted size={12}>
        {text.length}/2000 · Não informe dados pessoais. O relato não é salvo
        como rascunho.
      </Txt>
      <Notice text="A foto é opcional e pode ser anexada após o cadastro, na tela do protocolo." />
      {!!error && <Notice text={error} tone="error" />}
      <Button
        label="Enviar relato"
        busy={busy}
        disabled={offline || uncertain}
        onPress={() => void submit()}
      />
      {uncertain && (
        <Button
          secondary
          label="Conferir ocorrências"
          onPress={() => router.replace("/ocorrencias")}
        />
      )}
    </Page>
  );
}
