import React from "react";
import { useApp } from "../../src/state/AppContext";
import { Tabs } from "expo-router";
import { Icon, type IconName } from "../../src/components/ui";
import { theme as t } from "../../src/theme";
export default function TabLayout() {
  const screens: [string, string, IconName][] = [
    ["index", "Início", "home-outline"],
    ["ocorrencias", "Ocorrências", "reader-outline"],
    ["risco", "Risco", "shield-checkmark-outline"],
    ["ajustes", "Ajustes", "options-outline"],
  ];
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: t.color.primary,
        tabBarInactiveTintColor: t.color.muted,
        tabBarStyle: {
          backgroundColor: t.color.surface,
          borderTopColor: t.color.border,
        },
        tabBarLabelStyle: { fontSize: t.font.caption, fontWeight: "600" },
        tabBarItemStyle: { paddingVertical: t.space.xxs },
      }}
    >
      {screens.map(([name, title, icon]) => (
        <Tabs.Screen
          key={name}
          name={name}
          options={{
            title,
            tabBarBadge: undefined,
            tabBarBadgeStyle: {
              backgroundColor: t.color.orangeBg,
              color: t.color.orange,
            },
            tabBarAccessibilityLabel: title,
            tabBarIcon: ({ color }) => (
              <Icon
                name={icon}
                color={typeof color === "string" ? color : t.color.primary}
              />
            ),
          }}
        />
      ))}
    </Tabs>
  );
}
