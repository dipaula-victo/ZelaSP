// Prévia web: IndexedDB. O build Android/iOS resolve storage.ts e usa SQLite.
let database: Promise<IDBDatabase> | undefined;
function open() {
  return (database ??= new Promise((resolve, reject) => {
    const req = indexedDB.open("zelasp", 1);
    req.onupgradeneeded = () => req.result.createObjectStore("app_storage");
    req.onsuccess = () => resolve(req.result);
    req.onerror = () =>
      reject(new Error("Não foi possível abrir o banco local do navegador."));
  }));
}
export async function read<T>(key: string, fallback: T): Promise<T> {
  const db = await open();
  return new Promise((resolve, reject) => {
    const tx = db.transaction("app_storage", "readonly");
    const req = tx.objectStore("app_storage").get(key);
    req.onsuccess = () => resolve(req.result ?? fallback);
    req.onerror = () => reject(req.error);
  });
}
export async function write<T>(key: string, value: T) {
  const db = await open();
  return new Promise<void>((resolve, reject) => {
    const tx = db.transaction("app_storage", "readwrite");
    tx.objectStore("app_storage").put(value, key);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
    tx.onabort = () => reject(tx.error);
  });
}
