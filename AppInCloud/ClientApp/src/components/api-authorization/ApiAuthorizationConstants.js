export const ApplicationName = 'AppInCloud';

export const QueryParameterNames = {
  ReturnUrl: 'returnUrl',
  Message: 'message'
};

export const LogoutActions = {
  LogoutCallback: 'logout-callback',
  Logout: 'Login',
  LoggedOut: 'logged-out'
};

export const LoginActions = {
  Login: 'Login',
  LoginCallback: 'login-callback',
  LoginFailed: 'login-failed',
  Profile: 'profile',
  Register: 'Register'
};

const prefix = '/Account';

export const ApplicationPaths = {
  DefaultLoginRedirectPath: '/',
  ApiAuthorizationClientConfigurationUrl: `_configuration/${ApplicationName}`,
  ApiAuthorizationPrefix: prefix,
  Login: `${prefix}/${LoginActions.Login}`,
  LoginFailed: `${prefix}/${LoginActions.LoginFailed}`,
  LoginCallback: `${prefix}/${LoginActions.LoginCallback}`,
  Register: `${prefix}/${LoginActions.Register}`,
  Profile: `${prefix}/${LoginActions.Profile}`,
  LogOut: `${prefix}/${LogoutActions.Logout}`,
  LoggedOut: `${prefix}/${LogoutActions.LoggedOut}`,
  LogOutCallback: `${prefix}/${LogoutActions.LogoutCallback}`,
  IdentityRegisterPath: 'Account/Register',
  IdentityManagePath: 'Account/Manage'
};
