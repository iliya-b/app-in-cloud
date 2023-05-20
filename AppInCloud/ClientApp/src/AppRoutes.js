import ApiAuthorzationRoutes from './components/api-authorization/ApiAuthorizationRoutes';
import { Apps } from "./components/Apps";
import { AppStream,  } from "./components/AppStream";
import { Admin } from "./components/Admin";

const AppRoutes = [
  {
    path: '/admin',
    element: <Admin />
  },
  {
    path: '/',
    element: <Apps />
  },
  {
    path: '/apps/:id',
    requireAuth: true,
    element: <AppStream />,

  },

  ...ApiAuthorzationRoutes
];

export default AppRoutes;
