import * as ImagePicker from "expo-image-picker";
import { File, Directory, Paths } from "expo-file-system";
import { validateImageBytes } from "../utils/validation";
import type { Photo } from "../data/types";
export async function choosePhoto(camera = false): Promise<Photo | null> {
  if (camera) {
    const p = await ImagePicker.requestCameraPermissionsAsync();
    if (!p.granted)
      throw new Error(
        "Permita o acesso à câmera ou escolha uma foto da galeria.",
      );
  }
  const result = await (camera
    ? ImagePicker.launchCameraAsync({ mediaTypes: ["images"], quality: 0.8 })
    : ImagePicker.launchImageLibraryAsync({
        mediaTypes: ["images"],
        quality: 0.8,
      }));
  if (result.canceled) return null;
  const asset = result.assets[0];
  const original = new File(asset.uri);
  const mime = asset.mimeType ?? original.type;
  const bytes = await original.bytes();
  const error = validateImageBytes(bytes, mime);
  if (error) throw new Error(error);
  const dir = new Directory(Paths.cache, "zelasp-picking");
  dir.create({ idempotent: true, intermediates: true });
  const name = `foto-${Date.now()}.${mime === "image/png" ? "png" : "jpg"}`;
  const destination = new File(dir, name);
  original.copy(destination);
  return {
    uri: destination.uri,
    nome: name,
    mime: mime as Photo["mime"],
    tamanho: bytes.length,
  };
}
export async function cachePhoto(url: string, id: number): Promise<Photo> {
  const dir = new Directory(Paths.document, "zelasp-photos");
  dir.create({ idempotent: true, intermediates: true });
  const destination = new File(dir, `imagem-${id}`);
  const file = await File.downloadFileAsync(url, destination, {
    idempotent: true,
  });
  const bytes = await file.bytes();
  const mime = bytes[0] === 137 ? "image/png" : "image/jpeg";
  const error = validateImageBytes(bytes, mime);
  if (error) {
    file.delete();
    throw new Error(error);
  }
  return { uri: file.uri, nome: `foto-${id}`, mime, tamanho: bytes.length };
}
export async function retainPhoto(photo: Photo, id: number): Promise<Photo> {
  const dir = new Directory(Paths.document, "zelasp-photos");
  dir.create({ idempotent: true, intermediates: true });
  const destination = new File(dir, `imagem-${id}`);
  if (photo.uri !== destination.uri) new File(photo.uri).copy(destination);
  return { ...photo, uri: destination.uri };
}
export async function deletePhoto(photo: Photo) {
  try {
    const file = new File(photo.uri);
    if (file.exists) file.delete();
  } catch {}
}
