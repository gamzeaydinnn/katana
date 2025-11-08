import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";

let connection: HubConnection | null = null;

const HUB_URL = "/hubs/notifications"; // proxy will forward in dev

export function startConnection() {
  if (connection) {
    // If already connected/connecting, do nothing (idempotent)
    const state = (connection.state as unknown) as string;
    if (state && state !== "Disconnected") {
      return Promise.resolve();
    }
    // Only start when fully disconnected
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
    .catch((err) => {
      console.error("[SignalR Service] ❌ Failed to start connection:", err);
      throw err;
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

// Reserved for future use: backend may emit a rejected event
export function onPendingRejected(handler: (payload: object) => void) {
  connection?.on("PendingStockAdjustmentRejected", handler);
}

export function offPendingRejected(handler: (payload: object) => void) {
  connection?.off("PendingStockAdjustmentRejected", handler);
}

export function isConnected() {
  return connection !== null && ((connection!.state as unknown) as string) === "Connected";
}
