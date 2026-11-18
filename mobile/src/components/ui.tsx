import React from "react";
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
  type ViewStyle,
  type StyleProp,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import Ionicons from "@expo/vector-icons/Ionicons";
import { theme as t } from "../theme";
export type IconName = React.ComponentProps<typeof Ionicons>["name"];
export function Icon({
  name,
  color = t.color.primary,
  size = t.size.icon,
}: {
  name: IconName;
  color?: string;
  size?: number;
}) {
  return <Ionicons name={name} size={size} color={color} accessible={false} />;
}
export function Txt({
  children,
  muted = false,
  size = t.font.body,
  bold = false,
  color,
  style,
}: {
  children: React.ReactNode;
  muted?: boolean;
  size?: number;
  bold?: boolean;
  color?: string;
  style?: StyleProp<import("react-native").TextStyle>;
}) {
  return (
    <Text
      style={[
        {
          fontSize: size,
          lineHeight: Math.round(size * 1.45),
          color: color ?? (muted ? t.color.muted : t.color.ink),
          fontWeight: bold ? "700" : "400",
        },
        style,
      ]}
    >
      {children}
    </Text>
  );
}
export function Page({
  children,
  title,
  subtitle,
  action,
}: {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
  action?: React.ReactNode;
}) {
  return (
    <SafeAreaView edges={["top", "left", "right"]} style={s.fill}>
      <ScrollView
        keyboardShouldPersistTaps="handled"
        contentContainerStyle={s.page}
      >
        <View style={s.content}>
          {!!title && (
            <View style={s.header}>
              <View style={s.flex}>
                <Txt size={t.font.heading} bold>
                  {title}
                </Txt>
                {!!subtitle && (
                  <Txt muted size={t.font.small}>
                    {subtitle}
                  </Txt>
                )}
              </View>
              {action}
            </View>
          )}
          {children}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
export function Card({
  children,
  style,
}: {
  children: React.ReactNode;
  style?: StyleProp<ViewStyle>;
}) {
  return <View style={[s.card, style]}>{children}</View>;
}
export function Button({
  label,
  onPress,
  icon,
  secondary = false,
  disabled = false,
  busy = false,
}: {
  label: string;
  onPress: () => void;
  icon?: IconName;
  secondary?: boolean;
  disabled?: boolean;
  busy?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ disabled: disabled || busy, busy }}
      disabled={disabled || busy}
      onPress={onPress}
      style={({ pressed }) => [
        s.button,
        secondary && s.secondary,
        (disabled || busy) && s.disabled,
        pressed && s.pressed,
      ]}
    >
      {busy ? (
        <ActivityIndicator
          color={secondary ? t.color.primary : t.color.white}
        />
      ) : (
        !!icon && (
          <Icon
            name={icon}
            color={secondary ? t.color.primary : t.color.white}
          />
        )
      )}
      <Txt bold color={secondary ? t.color.primary : t.color.white}>
        {label}
      </Txt>
    </Pressable>
  );
}
export function Notice({
  text,
  tone = "info",
  action,
  onAction,
}: {
  text: string;
  tone?: "info" | "error" | "success";
  action?: string;
  onAction?: () => void;
}) {
  return (
    <View
      accessibilityLiveRegion="polite"
      accessibilityRole={tone === "error" ? "alert" : undefined}
      style={[
        s.notice,
        { backgroundColor: tone === "error" ? t.color.redBg : t.color.soft },
      ]}
    >
      <View style={s.row}>
        <Icon
          name={
            tone === "error"
              ? "alert-circle-outline"
              : tone === "success"
                ? "checkmark-circle-outline"
                : "information-circle-outline"
          }
          color={tone === "error" ? t.color.red : t.color.primary}
        />
        <View style={s.flex}>
          <Txt
            size={t.font.small}
            color={tone === "error" ? t.color.red : t.color.ink}
          >
            {text}
          </Txt>
        </View>
      </View>
      {!!action && onAction && (
        <Button secondary label={action} onPress={onAction} />
      )}
    </View>
  );
}
export function Empty({
  title,
  text,
  icon = "leaf-outline",
  action,
  onAction,
}: {
  title: string;
  text: string;
  icon?: IconName;
  action?: string;
  onAction?: () => void;
}) {
  return (
    <Card style={s.empty}>
      <View style={s.emptyIcon}>
        <Icon name={icon} size={t.space.xl} />
      </View>
      <Txt bold size={t.font.subtitle}>
        {title}
      </Txt>
      <Txt muted style={{ textAlign: "center" }}>
        {text}
      </Txt>
      {!!action && onAction && <Button label={action} onPress={onAction} />}
    </Card>
  );
}
export function Chip({
  label,
  selected,
  onPress,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ selected }}
      onPress={onPress}
      style={[s.chip, selected && s.chipSelected]}
    >
      <Txt
        size={t.font.small}
        bold
        color={selected ? t.color.white : t.color.ink}
      >
        {label}
      </Txt>
    </Pressable>
  );
}
export function Section({
  title,
  action,
  onAction,
}: {
  title: string;
  action?: string;
  onAction?: () => void;
}) {
  return (
    <View style={s.section}>
      <Txt size={t.font.subtitle} bold>
        {title}
      </Txt>
      {!!action && (
        <Pressable
          onPress={onAction}
          accessibilityRole="button"
          accessibilityLabel={action}
          style={s.link}
        >
          <Txt color={t.color.primary} bold size={t.font.small}>
            {action}
          </Txt>
          <Icon name="arrow-forward" size={t.font.body} />
        </Pressable>
      )}
    </View>
  );
}
export function SkeletonCards() {
  return (
    <View
      accessibilityRole="progressbar"
      accessibilityLabel="Carregando ocorrências"
      style={{ gap: t.space.md }}
    >
      {[0, 1].map((index) => (
        <View key={index} style={[s.card, { gap: t.space.md }]}>
          <View style={s.row}>
            <View
              style={{
                width: t.size.touch,
                height: t.size.touch,
                borderRadius: t.radius.medium,
                backgroundColor: t.color.soft,
              }}
            />
            <View style={s.flex}>
              <View
                style={{
                  height: t.font.body,
                  width: "65%",
                  backgroundColor: t.color.soft,
                  borderRadius: t.radius.small,
                }}
              />
              <View
                style={{
                  height: t.font.caption,
                  width: "40%",
                  marginTop: t.space.xs,
                  backgroundColor: t.color.soft,
                  borderRadius: t.radius.small,
                }}
              />
            </View>
          </View>
          <View
            style={{
              height: t.font.small,
              width: "90%",
              backgroundColor: t.color.soft,
              borderRadius: t.radius.small,
            }}
          />
        </View>
      ))}
    </View>
  );
}
export function StepProgress({
  completed,
  total = 4,
}: {
  completed: number;
  total?: number;
}) {
  return (
    <View
      accessibilityLabel={`${completed} de ${total} etapas preenchidas`}
      style={{ gap: t.space.xs }}
    >
      <View style={s.row}>
        {Array.from({ length: total }, (_, i) => (
          <View
            key={i}
            style={{
              flex: 1,
              height: t.space.xxs,
              borderRadius: t.radius.pill,
              backgroundColor: i < completed ? t.color.primary : t.color.track,
            }}
          />
        ))}
      </View>
      <Txt size={t.font.caption} muted>
        {completed} de {total} etapas preenchidas
      </Txt>
    </View>
  );
}
export function CategoryTile({
  label,
  selected,
  onPress,
  icon = "trash-outline",
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
  icon?: IconName;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ selected }}
      onPress={onPress}
      style={[
        s.categoryTile,
        selected && {
          backgroundColor: t.color.soft,
          borderColor: t.color.primary,
        },
      ]}
    >
      <View style={[s.row, { justifyContent: "space-between" }]}>
        <Icon name={icon} />
        <Icon
          name={selected ? "checkmark-circle" : "ellipse-outline"}
          color={selected ? t.color.primary : t.color.border}
        />
      </View>
      <Txt bold size={t.font.small}>
        {label}
      </Txt>
    </Pressable>
  );
}
export const s = StyleSheet.create({
  modalBackdrop: {
    flex: 1,
    backgroundColor: t.color.overlay,
    alignItems: "center",
    justifyContent: "center",
    padding: t.space.lg,
  },
  segmented: {
    flexDirection: "row",
    gap: t.space.xxs,
    padding: t.space.xxs,
    borderRadius: t.radius.medium,
    backgroundColor: t.color.soft,
  },
  segment: {
    flex: 1,
    minHeight: t.size.touch,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: t.radius.small,
    paddingHorizontal: t.space.xs,
  },
  segmentActive: { backgroundColor: t.color.primary },
  filterButton: {
    minHeight: t.size.touch,
    flexDirection: "row",
    gap: t.space.xs,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: t.color.surface,
    borderRadius: t.radius.medium,
    paddingHorizontal: t.space.sm,
    borderWidth: 1,
    borderColor: t.color.border,
  },
  iconButton: {
    minWidth: t.size.touch,
    minHeight: t.size.touch,
    alignItems: "center",
    justifyContent: "center",
  },
  categoryTile: {
    flexGrow: 1,
    flexBasis: "30%",
    minWidth: t.size.tileMin,
    minHeight: t.size.tileHeight,
    padding: t.space.sm,
    gap: t.space.sm,
    borderRadius: t.radius.medium,
    borderWidth: 1,
    borderColor: t.color.border,
    backgroundColor: t.color.surface,
  },
  fill: { flex: 1, backgroundColor: t.color.background },
  page: { flexGrow: 1, padding: t.space.lg, paddingBottom: t.space.xxl },
  content: {
    width: "100%",
    maxWidth: t.size.content,
    alignSelf: "center",
    gap: t.space.md,
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    gap: t.space.md,
    marginBottom: t.space.xs,
  },
  flex: { flex: 1 },
  row: { flexDirection: "row", alignItems: "center", gap: t.space.sm },
  wrap: { flexDirection: "row", flexWrap: "wrap", gap: t.space.xs },
  card: {
    padding: t.space.md,
    backgroundColor: t.color.surface,
    borderRadius: t.radius.large,
    borderWidth: 1,
    borderColor: t.color.border,
    gap: t.space.sm,
  },
  button: {
    minHeight: t.size.touch,
    backgroundColor: t.color.primary,
    paddingHorizontal: t.space.md,
    paddingVertical: t.space.sm,
    borderRadius: t.radius.medium,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: t.space.xs,
  },
  secondary: { backgroundColor: t.color.soft },
  disabled: { opacity: 0.5 },
  pressed: { opacity: 0.8 },
  notice: {
    padding: t.space.md,
    borderRadius: t.radius.medium,
    gap: t.space.sm,
  },
  empty: { alignItems: "center", paddingVertical: t.space.xl, gap: t.space.md },
  emptyIcon: {
    width: t.space.huge,
    height: t.space.huge,
    borderRadius: t.radius.medium,
    backgroundColor: t.color.soft,
    alignItems: "center",
    justifyContent: "center",
  },
  chip: {
    minHeight: t.size.touch,
    paddingHorizontal: t.space.md,
    paddingVertical: t.space.sm,
    borderWidth: 1,
    borderColor: t.color.border,
    borderRadius: t.radius.pill,
    backgroundColor: t.color.surface,
    justifyContent: "center",
  },
  chipSelected: {
    backgroundColor: t.color.primary,
    borderColor: t.color.primary,
  },
  section: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: t.space.xs,
    marginTop: t.space.xs,
  },
  link: {
    flexDirection: "row",
    alignItems: "center",
    gap: t.space.xs,
    minHeight: t.size.touch,
  },
  field: {
    backgroundColor: t.color.surface,
    borderWidth: 1,
    borderColor: t.color.border,
    borderRadius: t.radius.medium,
    padding: t.space.md,
    fontSize: t.font.body,
    color: t.color.ink,
    minHeight: t.size.touch,
  },
  label: { gap: t.space.xs },
  divider: { height: 1, backgroundColor: t.color.border },
  center: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    padding: t.space.lg,
    gap: t.space.md,
  },
});
