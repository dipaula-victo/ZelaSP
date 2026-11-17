import React from "react";
import { router } from "expo-router";
import { Page, Empty } from "../src/components/ui";
export default function NotFound() {
  return (
    <Page>
      <Empty
        title="Página não encontrada"
        text="Volte ao início para continuar."
        action="Ir para início"
        onAction={() => router.replace("/")}
      />
    </Page>
  );
}
