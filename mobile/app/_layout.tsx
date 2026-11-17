import React from "react";
import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { ActivityIndicator, View } from "react-native";
import { AppProvider, useApp } from "../src/state/AppContext";
import { Txt, s } from "../src/components/ui";
import { theme as t } from "../src/theme";
export { ErrorBoundary } from "expo-router";
function Content() {
  const { ready, fatal } = useApp();
  if (fatal)
    return (
      <View style={s.center}>
        <Txt bold>Não foi possível abrir o banco local</Txt>
        <Txt>{fatal}</Txt>
        <Txt muted>
          Reabra o aplicativo para tentar novamente. Seus dados não foram
          apagados.
        </Txt>
      </View>
    );
  if (!ready)
    return (
      <View style={s.center}>
        <ActivityIndicator color={t.color.primary} />
        <Txt>Preparando seu bairro…</Txt>
      </View>
    );
  return (
    <Stack
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: t.color.background },
      }}
    >
      <Stack.Screen name="(tabs)" />
      <Stack.Screen name="nova" />
      <Stack.Screen name="ocorrencia/[id]" />
    </Stack>
  );
}
export default function Root() {
  return (
    <SafeAreaProvider>
      <AppProvider>
        <StatusBar style="dark" />
        <Content />
      </AppProvider>
    </SafeAreaProvider>
  );
}
