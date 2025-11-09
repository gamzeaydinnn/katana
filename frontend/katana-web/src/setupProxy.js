const { createProxyMiddleware } = require("http-proxy-middleware");

module.exports = function (app) {
  // Backend port (automatically chosen if 5055 is busy)
  const backendPort = process.env.BACKEND_PORT || "5055";
  const backendUrl = `http://localhost:${backendPort}`;

  console.log(`[Proxy] Backend URL: ${backendUrl}`);

  app.use(
    "/api",
    createProxyMiddleware({
      target: backendUrl,
      changeOrigin: true,
      secure: false,
      logLevel: "warn",
      onError: (err, req, res) => {
        console.error("[API Proxy Error]", err.message);
        res.writeHead(500, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: "Proxy error", details: err.message }));
      },
    })
  );

  app.use(
    "/hubs",
    createProxyMiddleware({
      target: backendUrl,
      changeOrigin: true,
      secure: false,
      logLevel: "warn",
      ws: true,
      onError: (err, req, res) => {
        console.error("[SignalR Proxy Error]", err.message);
      },
    })
  );
};
