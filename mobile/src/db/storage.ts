import * as SQLite from "expo-sqlite";
import { schema, upsert } from "./schema";
let database: Promise<SQLite.SQLiteDatabase> | undefined;
function open() {
  return (database ??= (async () => {
    const db = await SQLite.openDatabaseAsync("zelasp.db");
    await db.execAsync(schema);
    return db;
  })());
}
export async function read<T>(key: string, fallback: T): Promise<T> {
  const row = await (
    await open()
  ).getFirstAsync<{ valor: string }>(
    "SELECT valor FROM app_storage WHERE chave = ?",
    key,
  );
  return row ? (JSON.parse(row.valor) as T) : fallback;
}
export async function write<T>(key: string, value: T) {
  await (await open()).runAsync(upsert, key, JSON.stringify(value));
}
