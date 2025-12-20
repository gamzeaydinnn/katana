module.exports = {
  devServer: {
    allowedHosts: 'all', // Tüm hostlardan erişime izin ver
    client: {
      webSocketURL: {
        hostname: process.env.WDS_SOCKET_HOST || 'localhost',
        port: process.env.WDS_SOCKET_PORT || 3000,
        protocol: 'ws',
      },
    },
  },
};
