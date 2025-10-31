import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";

let connection: HubConnection | null = null;

const HUB_URL = "/hubs/notifications"; // proxy will forward in dev

export function startConnection() {
  if (connection) return connection.start();
  connection = new HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => {
        try {
          return typeof window !== "undefined"
            ? window.localStorage.getItem("authToken") || ""
            : "";
        } catch {
          return "";
        }
      },
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();

  connection.onclose((e) => {
    console.warn("SignalR connection closed", e);
  });

  return connection.start();
}

export function stopConnection() {
  if (!connection) return Promise.resolve();
  const c = connection;
  connection = null;
  return c.stop();
}

export function onPendingCreated(handler: (payload: any) => void) {
  connection?.on("PendingStockAdjustmentCreated", handler);
}

export function offPendingCreated(handler: (payload: any) => void) {
  connection?.off("PendingStockAdjustmentCreated", handler);
}

export function onPendingApproved(handler: (payload: any) => void) {
  connection?.on("PendingStockAdjustmentApproved", handler);
}

export function offPendingApproved(handler: (payload: any) => void) {
  connection?.off("PendingStockAdjustmentApproved", handler);
}

export function isConnected() {
  return connection !== null && connection.state === "Connected";
}
