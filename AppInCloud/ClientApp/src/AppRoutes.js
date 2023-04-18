import ApiAuthorzationRoutes from './components/api-authorization/ApiAuthorizationRoutes';
import { Apps } from "./components/Apps";
import { AppStream,  } from "./components/AppStream";
import { Home } from "./components/Home";

const AppRoutes = [
  {
    path: '/admin',
    element: <Home />
  },
  {
    index: true,
    requireAuth: true,
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
