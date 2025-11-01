const { createProxyMiddleware } = require("http-proxy-middleware");

module.exports = function (app) {
  // Backend port (automatically chosen if 5055 is busy)
  const backendPort = process.env.BACKEND_PORT || "5055";
  const backendUrl = `http://localhost:${backendPort}`;
  
  console.log(`[Proxy] Backend URL: ${backendUrl}`);  // API proxy
  app.use(
    "/api",
    createProxyMiddleware({
      target: backendUrl,
      changeOrigin: true,
      secure: false,
      logLevel: "debug",
      onError: function (err, req, res) {
        console.log("API Proxy error:", err);
      },
      headers: {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Methods": "GET,PUT,POST,DELETE,OPTIONS",
        "Access-Control-Allow-Headers":
          "Content-Type, Authorization, Content-Length, X-Requested-With",
      },
    })
  );

  // SignalR Hub proxy - CRITICAL for real-time notifications
  app.use(
    "/hubs",
    createProxyMiddleware({
      target: backendUrl,
      changeOrigin: true,
      secure: false,
      logLevel: "debug",
      ws: true, // Enable WebSocket proxy
      onError: function (err, req, res) {
        console.log("SignalR Hub proxy error:", err);
      },
      onProxyReqWs: function (proxyReq, req, socket) {
        console.log("[Proxy] WebSocket request to:", req.url);
      },
    })
  );
};
