import * as msal from '@azure/msal-browser';
import { settings } from '../settings';

export class MsalService extends msal.PublicClientApplication {
  /** Login user from cache or by redirecting it */
  public async login(): Promise<boolean> {
    // Handle in case the current request is a redirection from auth
    await this.handleRedirectPromise();

    // All connected accounts
    const allAccounts = this.getAllAccounts();

    // If any connected account (from redirected response or cache)
    if (allAccounts.length > 0) {
      this.setActiveAccount(allAccounts[0]!);
      return true;
    }
    // Else force a login redirection
    else {
      const redirectRequest: msal.RedirectRequest = { scopes: scopes };

      await this.loginRedirect(redirectRequest);
      return false;
    }
  }

  public async getAccessToken(scopes: string[]): Promise<string> {
    const silentRequest = { scopes };

    try {
      const authResult = await this.acquireTokenSilent(silentRequest);
      return authResult.accessToken;
    } catch (error) {
      if (error instanceof msal.InteractionRequiredAuthError) {
        // fallback to interaction when silent call fails
        this.acquireTokenRedirect(silentRequest);
      }
    }

    return '';
  }
}

const msalConfig: msal.Configuration = {
  auth: {
    clientId: settings.auth.entraId.frontClientId,
    authority: `${settings.auth.entraId.instance}/${settings.auth.entraId.tenantId}/`,
    redirectUri: `${window.location.origin}${settings.auth.entraId.callbackPath}`,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: true,
  },
};

const scopes = ['email', `api://${settings.auth.entraId.backClientId}/access_as_user`];

const msalInstance = new MsalService(msalConfig);

export { scopes, msalInstance };
