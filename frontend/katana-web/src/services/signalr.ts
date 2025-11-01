import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";

let connection: HubConnection | null = null;

const HUB_URL = "/hubs/notifications"; // proxy will forward in dev

export function startConnection() {
  if (connection) {
    console.log("[SignalR Service] 🔄 Connection already exists, starting...");
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

  connection.onclose((e) => {
    console.warn("[SignalR Service] ⚠️ Connection closed:", e);
  });

  connection.onreconnecting((e) => {
    console.log("[SignalR Service] 🔄 Reconnecting...", e);
  });

  connection.onreconnected((connectionId) => {
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

export function onPendingCreated(handler: (payload: any) => void) {
  console.log(
    "[SignalR Service] 📝 Registering PendingStockAdjustmentCreated handler"
  );
  connection?.on("PendingStockAdjustmentCreated", (payload) => {
    console.log(
      "[SignalR Service] 📨 PendingStockAdjustmentCreated event:",
      payload
    );
    handler(payload);
  });
}

export function offPendingCreated(handler: (payload: any) => void) {
  console.log(
    "[SignalR Service] 🗑️ Removing PendingStockAdjustmentCreated handler"
  );
  connection?.off("PendingStockAdjustmentCreated", handler);
}

export function onPendingApproved(handler: (payload: any) => void) {
  console.log(
    "[SignalR Service] 📝 Registering PendingStockAdjustmentApproved handler"
  );
  connection?.on("PendingStockAdjustmentApproved", (payload) => {
    console.log(
      "[SignalR Service] 📨 PendingStockAdjustmentApproved event:",
      payload
    );
    handler(payload);
  });
}

export function offPendingApproved(handler: (payload: any) => void) {
  console.log(
    "[SignalR Service] 🗑️ Removing PendingStockAdjustmentApproved handler"
  );
  connection?.off("PendingStockAdjustmentApproved", handler);
}

export function isConnected() {
  return connection !== null && connection.state === "Connected";
}
