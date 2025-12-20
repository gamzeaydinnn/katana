import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";

let connection: HubConnection | null = null;

// Allow overriding backend base URL via `REACT_APP_API_URL` in dev/prod.
// If `REACT_APP_API_URL` is set, use that as the base; otherwise prefer
// a relative path (`/hubs/notifications`) so the React dev-server proxy
// (see package.json "proxy") can forward requests to the backend.
const getHubUrl = () => {
  const base = process.env.REACT_APP_API_URL?.trim();
  if (base && base.length > 0) {
    try {
      // Normalize: remove any trailing slash, and also strip a trailing `/api` or `/api/` if present
      let baseUrl = base.endsWith("/") ? base.slice(0, -1) : base;
      baseUrl = baseUrl.replace(/\/api\/?$/i, "");
      // If the env points to the API root (e.g. http://host:5055 or http://host:5055/api),
      // append the hub path directly so we don't end up with `/api/hubs/...`.
      const hubUrl = `${baseUrl}/hubs/notifications`;
      console.log("[SignalR Service] 🔗 Hub URL from env:", hubUrl);
      return hubUrl;
    } catch (e) {
      console.error("[SignalR Service] ❌ Error parsing REACT_APP_API_URL:", e);
      return "/hubs/notifications";
    }
  }

  // Default: use relative path so CRA dev proxy can forward it.
  console.log(
    "[SignalR Service] 🔗 Using relative hub path: /hubs/notifications"
  );
  return "/hubs/notifications";
};

const HUB_URL = getHubUrl();

export function startConnection() {
  if (connection) {
    const state = connection.state as unknown as string;
    if (state && state !== "Disconnected") {
      return Promise.resolve();
    }

    return connection.start();
  }

  console.log("[SignalR Service] 🆕 Creating new connection...");
  connection = new HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => {
        try {
          const token =
            typeof window !== "undefined"
              ? window.localStorage.getItem("authToken") || ""
              : "";
          console.log(
            "[SignalR Service] 🔑 Token retrieved:",
            token ? "✅ Present" : "❌ Missing"
          );
          return token;
        } catch {
          console.error("[SignalR Service] ❌ Error getting token");
          return "";
        }
      },
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();

  connection.onclose((e?: Error) => {
    console.warn("[SignalR Service] ⚠️ Connection closed:", e);
  });

  connection.onreconnecting((e?: Error) => {
    console.log("[SignalR Service] 🔄 Reconnecting...", e);
  });

  connection.onreconnected((connectionId?: string) => {
    console.log(
      "[SignalR Service] ✅ Reconnected! ConnectionId:",
      connectionId
    );
  });

  console.log("[SignalR Service] 🚀 Starting connection to:", HUB_URL);
  return connection
    .start()
    .then(() => {
      console.log("[SignalR Service] ✅ Connection started successfully");
    })
    .catch((err: any) => {
      console.error("[SignalR Service] ❌ Failed to start connection:", {
        message: err?.message,
        statusCode: err?.statusCode,
        errorType: err?.constructor?.name,
        fullError: err,
      });
      // Don't throw - allow app to continue without SignalR
      console.warn(
        "[SignalR Service] ⚠️ Continuing without SignalR notifications"
      );
    });
}

export function stopConnection() {
  if (!connection) return Promise.resolve();
  const c = connection;
  connection = null;
  return c.stop();
}

export function onPendingCreated(handler: (payload: object) => void) {
  connection?.on("PendingStockAdjustmentCreated", handler);
}

export function offPendingCreated(handler: (payload: object) => void) {
  connection?.off("PendingStockAdjustmentCreated", handler);
}

export function onPendingApproved(handler: (payload: object) => void) {
  connection?.on("PendingStockAdjustmentApproved", handler);
}

export function offPendingApproved(handler: (payload: object) => void) {
  connection?.off("PendingStockAdjustmentApproved", handler);
}

export function onPendingRejected(handler: (payload: object) => void) {
  connection?.on("PendingStockAdjustmentRejected", handler);
}

export function offPendingRejected(handler: (payload: object) => void) {
  connection?.off("PendingStockAdjustmentRejected", handler);
}

export function isConnected() {
  return (
    connection !== null &&
    (connection!.state as unknown as string) === "Connected"
  );
}

// ============ Yeni Bildirim Tipleri ============

export interface ProductNotification {
  id: number;
  eventType: "ProductCreated" | "ProductUpdated";
  productId: number;
  sku: string;
  name: string;
  source: "Katana" | "Manual";
  createdAt: string;
  link?: string;
}

export interface StockMovementNotification {
  id: number;
  eventType: "StockTransferCreated" | "StockAdjustmentCreated";
  movementId?: number;
  transferId?: number;
  adjustmentId?: number;
  documentNo: string;
  quantity: number;
  createdAt: string;
  link?: string;
}

export interface SyncNotification {
  id: number;
  eventType: "StockMovementSynced" | "StockMovementFailed";
  movementId: number;
  movementType: "TRANSFER" | "ADJUSTMENT";
  documentNo: string;
  lucaDocumentId?: number;
  errorMessage?: string;
  syncedAt?: string;
  failedAt?: string;
  link?: string;
}

// ============ Ürün Bildirimleri ============

export function onProductCreated(
  handler: (payload: ProductNotification) => void
) {
  connection?.on("ProductCreated", handler);
}

export function offProductCreated(
  handler: (payload: ProductNotification) => void
) {
  connection?.off("ProductCreated", handler);
}

export function onProductUpdated(
  handler: (payload: ProductNotification) => void
) {
  connection?.on("ProductUpdated", handler);
}

export function offProductUpdated(
  handler: (payload: ProductNotification) => void
) {
  connection?.off("ProductUpdated", handler);
}

// ============ Stok Hareketi Bildirimleri ============

export function onStockTransferCreated(
  handler: (payload: StockMovementNotification) => void
) {
  connection?.on("StockTransferCreated", handler);
}

export function offStockTransferCreated(
  handler: (payload: StockMovementNotification) => void
) {
  connection?.off("StockTransferCreated", handler);
}

export function onStockAdjustmentCreated(
  handler: (payload: StockMovementNotification) => void
) {
  connection?.on("StockAdjustmentCreated", handler);
}

export function offStockAdjustmentCreated(
  handler: (payload: StockMovementNotification) => void
) {
  connection?.off("StockAdjustmentCreated", handler);
}

// ============ Sync Bildirimleri ============

export function onStockMovementSynced(
  handler: (payload: SyncNotification) => void
) {
  connection?.on("StockMovementSynced", handler);
}

export function offStockMovementSynced(
  handler: (payload: SyncNotification) => void
) {
  connection?.off("StockMovementSynced", handler);
}

export function onStockMovementFailed(
  handler: (payload: SyncNotification) => void
) {
  connection?.on("StockMovementFailed", handler);
}

export function offStockMovementFailed(
  handler: (payload: SyncNotification) => void
) {
  connection?.off("StockMovementFailed", handler);
}
