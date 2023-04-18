import React, { useState, useEffect, Component } from 'react';
import authService from './api-authorization/AuthorizeService'
import { Apps, DefaultApps } from './Apps';

export class Home extends Component {
  static displayName = Home.name;

  render() {
    return (
      <div>
        <h3>Admin</h3>

        <legend>Devices <button className='btn btn-primary'>Add</button></legend>
        <DeviceList/>

        <DefaultApps />
      </div>
    );
  }
};


export const DeviceList = () => {


  const [data, setData] = useState([])


  useEffect( () => {
     authService.getAccessToken().then(
      token => fetch('api/v1/admin/devices', {headers: !token ? {} : { 'Authorization': `Bearer ${token}` }})
                .then(
                  response => response.json().then(data => setData(data))
                )
    );
                }, []);
    return <table className='table table-bordered'>
      <thead>
        <tr><td>ID</td><td>Users</td><td>Actions </td></tr>
      </thead>
      <tbody>
      {data && data.map(device => <tr key={device.id}><td>{device.id}</td><td>{device.users.join(',')}</td><td>[reset]</td></tr>) }
      </tbody>
    </table>;
}