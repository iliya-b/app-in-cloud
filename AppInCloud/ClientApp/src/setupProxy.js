const { createProxyMiddleware } = require('http-proxy-middleware');
const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :`https://localhost:7293`;
  // env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:23036';

const context =  [
  "/weatherforecast",
  "/list_devices",
  "/apk",
  "/appstream",
  "/test",
  "/devices",
  "/api",
  "/hangfire",
  
  "/polled_connections",
  "/infra_config",
  "/_configuration",
  "/.well-known",
  "/Identity",
  "/Account",
  "/lib/",
  "/js/",
  "/css/",
  "/CookieSample.styles.css",
  "/connect", 
  "/ApplyDatabaseMigrations",
  "/_framework"
];

module.exports = function(app) {
  const appProxy = createProxyMiddleware(context, {
    target: target, 
    secure: false,
    headers: {
    },
    ws: true,
    changeOrigin: true
  });

  app.use(appProxy);
};
