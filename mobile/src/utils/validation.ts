export const MAX_PHOTO_BYTES = 2 * 1024 * 1024;
export function validateDescription(value: string) {
  if (!value.trim()) return "A descrição é obrigatória.";
  if (value.trim().length > 2000) return "Use no máximo 2.000 caracteres.";
  return null;
}
export function validatePhoto(mime: string, size: number) {
  if (size <= 0) return "O arquivo está vazio.";
  if (size > MAX_PHOTO_BYTES) return "A foto precisa ter até 2 MiB.";
  if (!["image/jpeg", "image/png"].includes(mime))
    return "Escolha uma imagem PNG ou JPEG.";
  return null;
}
export function validateImageBytes(bytes: Uint8Array, mime: string) {
  const error = validatePhoto(mime, bytes.length);
  if (error) return error;
  const png = [137, 80, 78, 71, 13, 10, 26, 10].every((v, i) => bytes[i] === v);
  const jpg = bytes[0] === 255 && bytes[1] === 216 && bytes[2] === 255;
  return (mime === "image/png" && png) || (mime === "image/jpeg" && jpg)
    ? null
    : "O conteúdo do arquivo não corresponde a uma imagem PNG ou JPEG.";
}
export function dateLabel(value: string) {
  return new Date(value).toLocaleDateString("pt-BR", {
    day: "2-digit",
    month: "short",
  });
}
export function dateTime(value: string | null) {
  return value
    ? new Date(value).toLocaleString("pt-BR")
    : "Ainda não atualizado";
}
