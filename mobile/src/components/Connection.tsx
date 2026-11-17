import React from "react";
import { Notice, Txt } from "./ui";
import { useApp } from "../state/AppContext";
import { dateTime } from "../utils/validation";
export function Connection() {
  const { offline, stale, snapshot } = useApp();
  return (
    <>
      {(offline || stale) && (
        <Notice
          text={
            offline
              ? "Sem conexão — dados podem estar desatualizados"
              : "Não foi possível atualizar. Exibindo a última cópia salva."
          }
        />
      )}
      {!!snapshot.updatedAt && (offline || stale) && (
        <Txt muted size={12}>
          Última atualização: {dateTime(snapshot.updatedAt)}
        </Txt>
      )}
    </>
  );
}
