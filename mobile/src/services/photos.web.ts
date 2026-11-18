import * as ImagePicker from "expo-image-picker";
import { validateImageBytes } from "../utils/validation";
import type { Photo } from "../data/types";
async function fromBlob(blob: Blob, name: string): Promise<Photo> {
  const bytes = new Uint8Array(await blob.arrayBuffer());
  const error = validateImageBytes(bytes, blob.type);
  if (error) throw new Error(error);
  const uri = await new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(new Error("Não foi possível ler a foto."));
    reader.readAsDataURL(blob);
  });
  return {
    uri,
    nome: name,
    mime: blob.type as Photo["mime"],
    tamanho: blob.size,
  };
}
export async function choosePhoto(camera = false): Promise<Photo | null> {
  const result = await (camera
    ? ImagePicker.launchCameraAsync({ mediaTypes: ["images"], quality: 0.8 })
    : ImagePicker.launchImageLibraryAsync({
        mediaTypes: ["images"],
        quality: 0.8,
      }));
  if (result.canceled) return null;
  const asset = result.assets[0];
  const blob = await (await fetch(asset.uri)).blob();
  return fromBlob(blob, blob.type === "image/png" ? "foto.png" : "foto.jpg");
}
export async function cachePhoto(url: string, id: number) {
  const response = await fetch(url);
  if (!response.ok) throw new Error("Não foi possível carregar a foto.");
  return fromBlob(await response.blob(), `foto-${id}`);
}
export async function deletePhoto(_photo: Photo) {}
export async function retainPhoto(photo: Photo, _id: number) {
  return photo;
}
