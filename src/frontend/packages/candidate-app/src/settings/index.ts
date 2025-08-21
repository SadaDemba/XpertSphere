export const settings = {
  environment: import.meta.env.VITE_APP_ENV as string,

  auth: {
    mode: 'jwt' as const, // Candidate app uses JWT only
    jwt: {
      tokenKey: 'xpertsphere_candidate_token', // unique token key for candidate app
      refreshKey: 'xpertsphere_candidate_refresh_token', // unique refresh token key for candidate app
    },
  },

  // APIM Configuration
  apim: {
    subscriptionKey: import.meta.env.VITE_APIM_SUBSCRIPTION_KEY as string,
    headerName: 'Ocp-Apim-Subscription-Key',
    enabledEnvironments: ['staging', 'production'],
  },

  // Monolith Api
  webApi: {
    baseUrl: import.meta.env.VITE_WEB_API_BASE_URL as string,
  },

  // Resume Analyzer
  resumeAnalyzer: {
    baseUrl: import.meta.env.VITE_RESUME_ANALYZER_BASE_URL as string,
  },

  // Azure Blob Storage
  storage: {
    baseUrl: import.meta.env.VITE_STORAGE_BASE_URL as string,
  },

  // App config
  app: {
    name: 'XpertSphere Candidate',
    version: import.meta.env.VITE_APP_VERSION as string,
  },
};
