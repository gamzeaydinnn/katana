const { createProxyMiddleware } = require("http-proxy-middleware");

module.exports = function (app) {
  // API proxy
  app.use(
    "/api",
    createProxyMiddleware({
      target: "http://localhost:5055",
      changeOrigin: true,
      secure: false,
      logLevel: "debug",
      onError: function (err, req, res) {
        console.log("Proxy error:", err);
      },
      headers: {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Methods": "GET,PUT,POST,DELETE,OPTIONS",
        "Access-Control-Allow-Headers":
          "Content-Type, Authorization, Content-Length, X-Requested-With",
      },
    })
  );

  // SignalR Hub proxy - CRITICAL FIX
  app.use(
    "/hubs",
    createProxyMiddleware({
      target: "http://localhost:5055",
      changeOrigin: true,
      secure: false,
      logLevel: "debug",
      ws: true, // Enable WebSocket proxy
      onError: function (err, req, res) {
        console.log("SignalR Hub proxy error:", err);
      },
    })
  );
};
