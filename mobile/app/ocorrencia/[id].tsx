import React, { useState, useEffect } from "react";
import { View, Image, TextInput, Platform } from "react-native";
import { router, useLocalSearchParams } from "expo-router";
import {
  Page,
  Card,
  Txt,
  Button,
  Chip,
  Notice,
  SkeletonCards,
  Empty,
  s,
} from "../../src/components/ui";
import { Connection } from "../../src/components/Connection";
import { Badge } from "../../src/components/OccurrenceCard";
import { useApp } from "../../src/state/AppContext";
import {
  statusLabel,
  type Status,
  type Occurrence,
  type Photo,
} from "../../src/data/types";
import { choosePhoto, deletePhoto } from "../../src/services/photos";
import { dateTime } from "../../src/utils/validation";
import { ServiceError } from "../../src/services/errors";
import { hasMask } from "../../src/services/privacy";
function SavedPhoto({ id, title }: { id: number; title: string }) {
  const app = useApp();
  const [photo, setPhoto] = useState<Photo | null>(null),
    [error, setError] = useState("");
  async function load() {
    setError("");
    try {
      const result = await app.photo(id);
      if (result) setPhoto(result);
      else setError("Imagem não encontrada.");
    } catch {
      setError(
        "Não foi possível carregar a imagem. Conecte-se e tente novamente.",
      );
    }
  }
  useEffect(() => {
    void load();
  }, [id]);
  return (
    <View style={{ gap: 8 }}>
      {photo ? (
        <Image
          source={{ uri: photo.uri }}
          accessibilityLabel={title}
          style={{ height: 220, width: "100%", borderRadius: 16 }}
          resizeMode="contain"
        />
      ) : (
        <Txt muted>Foto #{id}</Txt>
      )}
      {!!error && (
        <Notice
          text={error}
          action="Recarregar foto"
          onAction={() => void load()}
        />
      )}
      <Txt muted size={12}>
        {title}
      </Txt>
    </View>
  );
}
export default function Detail() {
  const params = useLocalSearchParams<{ id: string; created?: string }>(),
    id = Number(params.id),
    app = useApp();
  const [row, setRow] = useState<Occurrence | null>(null),
    [loading, setLoading] = useState(true),
    [error, setError] = useState(""),
    [cached, setCached] = useState(false),
    [uncertain, setUncertain] = useState(false),
    [actor, setActor] = useState(""),
    [status, setStatus] = useState<Status>("EM_ANALISE"),
    [photo, setPhoto] = useState<Photo | null>(null),
    [title, setTitle] = useState(""),
    [notice, setNotice] = useState(""),
    [confirm, setConfirm] = useState(false);
  async function load() {
    setLoading(true);
    setError("");
    try {
      const result = await app.detail(id);
      setRow(result.row);
      setCached(result.cached);
      if (!result.cached) setUncertain(false);
      setStatus(result.row.status === "ABERTA" ? "EM_ANALISE" : "RESOLVIDA");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Não foi possível carregar.");
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    void load();
  }, [id]);
  async function pick(camera = false) {
    try {
      const next = await choosePhoto(camera);
      if (next) {
        if (photo) await deletePhoto(photo);
        setPhoto(next);
      }
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Não foi possível selecionar a foto.",
      );
    }
  }
  async function upload() {
    if (!photo) return;
    setError("");
    try {
      const updated = await app.attachPhoto(id, photo, title, actor);
      setRow(updated);
      await deletePhoto(photo);
      setPhoto(null);
      setTitle("");
      setNotice("Foto anexada com sucesso.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Falha ao anexar.");
      if (e instanceof ServiceError && e.uncertain) setUncertain(true);
    }
  }
  async function change() {
    setError("");
    setConfirm(false);
    try {
      setRow(await app.changeStatus(id, status, actor));
      setNotice("Status atualizado com sucesso.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Falha ao atualizar.");
      if (e instanceof ServiceError && e.uncertain) setUncertain(true);
    }
  }
  const disabled = app.offline || app.busy || !actor || uncertain;
  return (
    <Page
      title={row ? `Protocolo #ZSP-${row.id}` : "Detalhes"}
      subtitle="Acompanhe o cuidado com o bairro"
    >
      <Button
        secondary
        label="Voltar para ocorrências"
        onPress={() => router.replace("/ocorrencias")}
      />
      <Connection />
      {cached && !app.offline && (
        <Notice text="Exibindo o detalhe salvo. Os dados podem estar desatualizados." />
      )}
      {!!error && <Notice tone="error" text={error} />}
      {loading ? (
        <SkeletonCards />
      ) : row ? (
        <>
          {params.created === "1" && (
            <Notice
              tone="success"
              text="Ocorrência registrada com sucesso. Guarde seu protocolo."
            />
          )}
          {!!notice && <Notice tone="success" text={notice} />}
          <Card>
            <Badge status={row.status} />
            <Txt bold size={24}>
              {
                app.snapshot.categorias.find((x) => x.id === row.categoriaId)
                  ?.nome
              }
            </Txt>
            <Txt muted>
              {app.snapshot.bairros.find((x) => x.id === row.bairroId)?.nome}
            </Txt>
            <Txt>{row.descricao}</Txt>
            <Txt muted size={12}>
              Criada em {dateTime(row.criadaEm)}
            </Txt>
            {(row.dadosMascarados || hasMask(row.descricao)) && (
              <Notice text="Dados pessoais detectados foram mascarados" />
            )}
          </Card>
          <Txt bold size={18}>
            Fotos da ocorrência
          </Txt>
          {(row.imagens?.length ?? 0) > 0 ? (
            row.imagens!.map((meta) => (
              <Card key={meta.id}>
                <SavedPhoto id={meta.id} title={meta.titulo} />
                <Txt muted size={12}>
                  Anexada por {meta.executor} · {dateTime(meta.dataUtc)}
                </Txt>
              </Card>
            ))
          ) : row.imagemId ? (
            <SavedPhoto id={row.imagemId} title="Foto da ocorrência" />
          ) : (
            <Txt muted>Nenhuma foto anexada.</Txt>
          )}
          <Card>
            <Txt bold>Responsável pela atualização</Txt>
            <Txt muted size={14}>
              Selecione o executor cadastrado para anexar fotos ou alterar o
              status.
            </Txt>
            <View style={s.wrap}>
              {app.snapshot.executores.map((x) => (
                <Chip
                  key={x.id}
                  label={x.nome}
                  selected={actor === x.id}
                  onPress={() => setActor(x.id)}
                />
              ))}
            </View>
            {app.snapshot.executores.length === 0 && (
              <Notice text="Não foi possível obter os executores cadastrados. Atualize a lista antes de alterar esta ocorrência." />
            )}
          </Card>
          <Card>
            <Txt bold>Anexar foto</Txt>
            <Txt muted size={14}>
              JPEG ou PNG, até 2 MiB. Use uma imagem de teste sem pessoas ou
              dados pessoais.
            </Txt>
            <Button
              secondary
              label="Escolher foto"
              disabled={app.offline || app.busy}
              onPress={() => void pick()}
            />
            {Platform.OS !== "web" && (
              <Button
                secondary
                label="Tirar foto"
                disabled={app.offline || app.busy}
                onPress={() => void pick(true)}
              />
            )}
            {!!photo && (
              <>
                <Image
                  source={{ uri: photo.uri }}
                  accessibilityLabel="Prévia da foto selecionada"
                  style={{ height: 200, borderRadius: 16 }}
                  resizeMode="contain"
                />
                <TextInput
                  value={title}
                  onChangeText={setTitle}
                  maxLength={100}
                  style={s.field}
                  accessibilityLabel="Título da foto"
                  placeholder="Título da foto (opcional)"
                />
                <Button
                  label="Enviar foto"
                  disabled={disabled}
                  busy={app.busy}
                  onPress={() => void upload()}
                />
                <Button
                  secondary
                  label="Remover seleção"
                  disabled={app.busy}
                  onPress={() => {
                    void deletePhoto(photo);
                    setPhoto(null);
                  }}
                />
              </>
            )}
          </Card>
          <Card>
            <Txt bold>Atualizar status</Txt>
            <View style={s.wrap}>
              {Object.entries(statusLabel).map(([value, label]) => (
                <Chip
                  key={value}
                  label={label}
                  selected={status === value}
                  onPress={() => setStatus(value as Status)}
                />
              ))}
            </View>
            {confirm ? (
              <>
                <Notice
                  text={`Confirmar alteração para ${statusLabel[status]} pelo executor ${actor}?`}
                />
                <Button
                  label="Confirmar alteração"
                  disabled={disabled}
                  busy={app.busy}
                  onPress={() => void change()}
                />
                <Button
                  secondary
                  label="Cancelar alteração"
                  onPress={() => setConfirm(false)}
                />
              </>
            ) : (
              <Button
                label="Salvar status"
                disabled={disabled || status === row.status}
                onPress={() => setConfirm(true)}
              />
            )}
          </Card>
          <Txt bold size={18}>
            Histórico de alterações
          </Txt>
          {row.historico.length ? (
            row.historico.map((h, i) => (
              <Card key={`${h.alteradoEm}-${i}`}>
                <Txt bold>
                  {statusLabel[h.statusAnterior]} → {statusLabel[h.statusNovo]}
                </Txt>
                <Txt muted>{h.executor}</Txt>
                <Txt muted size={12}>
                  {dateTime(h.alteradoEm)}
                </Txt>
              </Card>
            ))
          ) : (
            <Txt muted>Ainda não houve alteração de status.</Txt>
          )}
          <Button
            secondary
            label="Atualizar detalhe"
            disabled={app.offline}
            onPress={() => void load()}
          />
        </>
      ) : (
        <Empty
          title="Ocorrência não encontrada."
          text="Confira o protocolo ou tente novamente com conexão."
          action="Tentar novamente"
          onAction={() => void load()}
        />
      )}
    </Page>
  );
}
