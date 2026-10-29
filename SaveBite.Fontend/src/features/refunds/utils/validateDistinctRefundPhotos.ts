// Reject identical file contents before any evidence is uploaded.
export async function validateDistinctRefundPhotos(files: File[]): Promise<void> {
  const hashes = new Set<string>();
  const originals: Uint8Array[] = [];
  for (const file of files) {
    const bytes = new Uint8Array(await file.arrayBuffer());
    if (globalThis.crypto?.subtle) {
      const hash = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", bytes)))
        .map((byte) => byte.toString(16).padStart(2, "0"))
        .join("");
      if (hashes.has(hash))
        throw new Error(
          "Use different photos for each portion. Duplicate photos cannot be reused across items.",
        );
      hashes.add(hash);
    } else {
      // Local HTTP previews may not expose Web Crypto; compare bytes instead.
      if (
        originals.some(
          (old) =>
            old.length === bytes.length && old.every((value, index) => value === bytes[index]),
        )
      )
        throw new Error(
          "Use different photos for each portion. Duplicate photos cannot be reused across items.",
        );
      originals.push(bytes);
    }
  }
}
