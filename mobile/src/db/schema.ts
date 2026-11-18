export const schema = `PRAGMA journal_mode = WAL;
CREATE TABLE IF NOT EXISTS app_storage (chave TEXT PRIMARY KEY NOT NULL, valor TEXT NOT NULL);
PRAGMA user_version = 1;`;
export const upsert =
  "INSERT INTO app_storage (chave, valor) VALUES (?, ?) ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor";
