import ApiAuthorzationRoutes from './components/api-authorization/ApiAuthorizationRoutes';
import { Apps } from "./components/Apps";
import { AppStream,  } from "./components/AppStream";
import { DefaultApps } from "./components/Apps";
import { UserList } from "./components/Users";
import { DeviceList } from "./components/Devices";

const AppRoutes = [
  {
    path: '/admin/users',
    element: <UserList />
  },{
    path: '/admin/devices',
    element: <DeviceList key='admin-devices' role='admin' />
  },{
    path: '/admin/defaultapps',
    element: <DefaultApps />
  },
  {
    path: '/',
    element: <Apps />
  },
  {
    path: '/my-devices',
    element: <DeviceList key='user-devices' role='user' />
  },
  {
    path: '/apps/:id',
    requireAuth: true,
    element: <AppStream />,

  },

  ...ApiAuthorzationRoutes
];

export default AppRoutes;
