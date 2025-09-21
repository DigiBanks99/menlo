// Production environment configuration
export const environment = {
  production: true,
  apiBaseUrl: '/api', // Will be proxied by Azure Static Web Apps or Cloudflare
  appName: 'Menlo Home Management',
  version: '1.0.0',
  enableDebugMode: false,
  enableAnalytics: true,
  logLevel: 'error'
};
