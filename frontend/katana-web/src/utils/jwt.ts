export type JwtPayload = {
  exp?: number;
  [key: string]: unknown;
};

const decodeBase64Url = (segment: string): string => {
  let base64 = segment.replace(/-/g, "+").replace(/_/g, "/");
  const pad = base64.length % 4;
  if (pad) {
    base64 += "=".repeat(4 - pad);
  }

  if (typeof window !== "undefined" && typeof window.atob === "function") {
    return window.atob(base64);
  }

  const globalBuffer =
    typeof globalThis !== "undefined"
      ? (globalThis as Record<string, any>).Buffer
      : undefined;

  if (globalBuffer && typeof globalBuffer.from === "function") {
    return globalBuffer.from(base64, "base64").toString("utf-8");
  }

  throw new Error("Base64 decoding is not supported in this environment.");
};

export const decodeJwtPayload = (
  token?: string | null
): JwtPayload | null => {
  if (!token || typeof token !== "string") return null;
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const decoded = decodeBase64Url(parts[1]);
    return JSON.parse(decoded);
  } catch {
    return null;
  }
};

export const isJwtExpired = (payload: JwtPayload | null): boolean => {
  if (!payload || typeof payload.exp !== "number") return false;
  const nowSeconds = Math.floor(Date.now() / 1000);
  return payload.exp <= nowSeconds;
};

export const tryGetJwtUsername = (
  payload: JwtPayload | null
): string | null => {
  if (!payload) return null;
  const candidates = [
    "preferred_username",
    "unique_name",
    "name",
    "sub",
    "email",
  ];
  for (const key of candidates) {
    const value = payload[key];
    if (typeof value === "string" && value.trim() !== "") {
      return value;
    }
  }
  return null;
};
