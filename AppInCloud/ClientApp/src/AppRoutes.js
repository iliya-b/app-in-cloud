import ApiAuthorzationRoutes from './components/api-authorization/ApiAuthorizationRoutes';
import { Counter } from "./components/Counter";
import { Apps } from "./components/Apps";
import { AppStream,  } from "./components/AppStream";
import { Home } from "./components/Home";

const AppRoutes = [
  {
    index: true,
    element: <Home />
  },
  {
    path: '/counter',
    element: <Counter />
  },
  {
    path: '/apps',
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
