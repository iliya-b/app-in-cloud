import { UserManager, WebStorageStateStore } from 'oidc-client';
import { ApplicationPaths, ApplicationName } from './ApiAuthorizationConstants';

export class AuthService {
  _callbacks = [];
  _nextSubscriptionId = 0;
  _user = null;
  _isAuthenticated = false;

  // By default pop ups are disabled because they don't work properly on Edge.
  // If you want to enable pop up authentication simply set this flag to false.
  _popUpDisabled = true;

  async isAuthenticated() {
    const user = await this.getUser();
    return !!user;
  }

  async getUser() {
    if (this._user && this._user.profile) {
      return this._user.profile;
    }

    // await this.ensureUserManagerInitialized();
    // const user1 = await this.userManager.getUser();
    const user = await (await this.fetch("/api/v1/user", {})).json()
    return user;
  }

  async getAccessToken() {
    await this.ensureUserManagerInitialized();
    const user = await this.userManager.getUser();
    return user && user.access_token;
  }

  async fetch(){
    const args = arguments
    // const token = await authService.getAccessToken();
    args[1] = args[1] === undefined ? {headers: {}} : args[1];
    args[1].headers = args[1].headers === undefined ? {} : args[1].headers;
    // args[1].headers.Authorization = `Bearer ${token}`;
    return fetch.apply(null, args);
  }
  

  // We try to sign out the user in two different ways:
  // 1) We try to do a sign-out using a PopUp Window. This might fail if there is a
  //    Pop-Up blocker or the user has disabled PopUps.
  // 2) If the method above fails, we redirect the browser to the IdP to perform a traditional
  //    post logout redirect flow.
  async signOut(state) {
    await this.ensureUserManagerInitialized();
    try {
      if (this._popUpDisabled) {
        throw new Error('Popup disabled. Change \'AuthorizeService.js:AuthorizeService._popupDisabled\' to false to enable it.')
      }

      await this.userManager.signoutPopup(this.createArguments());
      this.updateState(undefined);
      return this.success(state);
    } catch (popupSignOutError) {
      console.log("Popup signout error: ", popupSignOutError);
      try {
        await this.userManager.signoutRedirect(this.createArguments(state));
        return this.redirect();
      } catch (redirectSignOutError) {
        console.log("Redirect signout error: ", redirectSignOutError);
        return this.error(redirectSignOutError);
      }
    }
  }

  async completeSignOut(url) {
    await this.ensureUserManagerInitialized();
    try {
      const response = await this.userManager.signoutCallback(url);
      this.updateState(null);
      return this.success(response && response.data);
    } catch (error) {
      console.log(`There was an error trying to log out '${error}'.`);
      return this.error(error);
    }
  }

  updateState(user) {
    this._user = user;
    this._isAuthenticated = !!this._user;
    this.notifySubscribers();
  }

  subscribe(callback) {
    this._callbacks.push({ callback, subscription: this._nextSubscriptionId++ });
    return this._nextSubscriptionId - 1;
  }

  unsubscribe(subscriptionId) {
    const subscriptionIndex = this._callbacks
      .map((element, index) => element.subscription === subscriptionId ? { found: true, index } : { found: false })
      .filter(element => element.found === true);
    if (subscriptionIndex.length !== 1) {
      throw new Error(`Found an invalid number of subscriptions ${subscriptionIndex.length}`);
    }

    this._callbacks.splice(subscriptionIndex[0].index, 1);
  }

  notifySubscribers() {
    for (let i = 0; i < this._callbacks.length; i++) {
      const callback = this._callbacks[i].callback;
      callback();
    }
  }

  createArguments(state) {
    return { useReplaceToNavigate: true, data: state };
  }

  error(message) {
    return { status: AuthenticationResultStatus.Fail, message };
  }

  success(state) {
    return { status: AuthenticationResultStatus.Success, state };
  }

  redirect() {
    return { status: AuthenticationResultStatus.Redirect };
  }

  async ensureUserManagerInitialized() {
    if (this.userManager !== undefined) {
      return;
    }

    let response = await fetch(ApplicationPaths.ApiAuthorizationClientConfigurationUrl);
    if (!response.ok) {
      throw new Error(`Could not load settings for '${ApplicationName}'`);
    }

    let settings = await response.json();
    settings.automaticSilentRenew = true;
    settings.includeIdTokenInSilentRenew = true;
    settings.userStore = new WebStorageStateStore({
      prefix: ApplicationName
    });

    this.userManager = new UserManager(settings);

    this.userManager.events.addUserSignedOut(async () => {
      await this.userManager.removeUser();
      this.updateState(undefined);
    });
  }

  static get instance() { return authService }
}

const authService = new AuthService();

export default authService;

export const AuthenticationResultStatus = {
  Redirect: 'redirect',
  Success: 'success',
  Fail: 'fail'
};
