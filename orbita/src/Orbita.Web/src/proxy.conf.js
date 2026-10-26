// Aspire inyecta la URL de la API; sin Aspire se usa el puerto https de launchSettings.
const target =
  process.env['services__api__https__0'] ??
  process.env['services__api__http__0'] ??
  'https://localhost:7001';

module.exports = {
  '/api': {
    target,
    secure: false,
    changeOrigin: true,
  },
};
