export const dataMode =
  process.env.EXPO_PUBLIC_DATA_MODE === "api" ? "api" : "mock";
export const apiUrl = (process.env.EXPO_PUBLIC_API_URL ?? "").replace(
  /\/$/,
  "",
);
export const testMode =
  dataMode === "mock" || process.env.EXPO_PUBLIC_TEST_MODE === "true";
