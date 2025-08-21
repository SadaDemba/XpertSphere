export const settings = {
  environment: import.meta.env.VITE_APP_ENV as string,

  auth: {
    mode: import.meta.env.VITE_AUTH_MODE as 'jwt' | 'entraid',
    jwt: {
      tokenKey: 'xpertsphere_token', // name used to store token locally
      refreshKey: 'xpertsphere_refresh_token', // name used to store refresh token locally
    },
    entraId: {
      frontClientId: import.meta.env.VITE_ENTRAID_FRONT_CLIENTID as string,
      backClientId: import.meta.env.VITE_ENTRAID_BACK_CLIENTID as string,
      tenantId: import.meta.env.VITE_ENTRAID_TENANTID as string,
      instance: import.meta.env.VITE_ENTRAID_INSTANCE as string,
      callbackPath: import.meta.env.VITE_ENTRAID_CALLBACK_PATH as string,
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
    name: 'XpertSphere',
    version: import.meta.env.VITE_APP_VERSION as string,
  },
};
