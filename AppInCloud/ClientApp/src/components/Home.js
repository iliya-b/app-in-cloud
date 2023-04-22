import React, { useState, useEffect, Component } from 'react';
import authService from './api-authorization/AuthorizeService'
import { DefaultApps } from './Apps';

export class Home extends Component {
  static displayName = Home.name;

  render() {
    return (
      <div>
        <h3>Administration</h3>

        <legend>Devices </legend>
        <DeviceList/>
        <DefaultApps />
      </div>
    );
  }
};


export const DeviceList = () => {
  const [devicesInfo, setDevicesInfo] = useState({
    count: 0,
    list: []
  })
  const [error, setError] = useState("")
  const data = devicesInfo.list;
  const [reloadCounter, setReloadCounter] = useState(0)
  const reload = () => setReloadCounter(r => r+1)
  useEffect( () => {
     authService.fetch('api/v1/admin/devices').then(r => r.json().then(data => {
      setDevicesInfo(data)
      setError(null)
     } ))
  }, [reloadCounter]);

  const handleResponse = r => {
    if(r.status !== 200){
      r.text().then(t => setError(t))
    }else{
      reload()
    }
  }
  const reAmount = () => {
    var number = prompt("Enter new amount:", data.length);
    if (!number) return;
    var number = Number.parseInt(number);
    if(number === data.length) return;
    fetch('api/v1/admin/devices', {
      method: 'post', 
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      body: "N=" + number
    }).then(handleResponse);
  }

  
  const reset = (deviceId) => {
    authService.fetch('api/v1/admin/devices/' + deviceId + '/reset', {
      method: 'post', 
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
    }).then(handleResponse);
  }

  
  const addUser = (deviceId) => {
    var email = prompt("Enter email:");
    if (!email) return;
    authService.fetch('api/v1/admin/devices/' + deviceId + '/assign', {
      method: 'post', 
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      body: "userEmail=" + encodeURI(email)
    }).then(handleResponse);
  }

  
  const removeUser = (deviceId, email) => {
    authService.fetch('api/v1/admin/devices/' + deviceId + '/assign', {
      method: 'post', 
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      body: "unassign=true&userEmail=" + encodeURI(email)
    }).then(handleResponse);
  }

  return <table className='table table-bordered'>
      <thead>
        {devicesInfo.count > 0 && <tr><td colSpan={3}><font color="green">running tasks</font>: {devicesInfo.count}</td></tr>}
        {error  && <tr><td colSpan={3}><font color="red">{error}</font></td></tr>}
        <tr><td>ID</td><td>Users</td>
        <td>Actions 
          <button onClick={reAmount} className='btn btn-sm btn-outline-primary ms-2' title='Change amount'>
            <i className="bi bi-plus-slash-minus"></i>
          </button>
        </td>
</tr>
      </thead>
      <tbody>
      {data && data.map(device => <tr key={device.id}>
        <td>{device.id}</td>
        <td>
          {device.users.map(
              u => <span title='Delete'  key={u} className="badge bg-secondary">
                {u} <i className="bi bi-x-circle " style={{cursor: 'pointer'}} onClick={() => removeUser(device.id, u)} ></i>

                </span>
          )}
          <button onClick={() => addUser(device.id)} className='btn btn-sm btn-outline-primary ms-2'>
            <i className="bi bi-plus-circle"></i>
          </button>
        </td>
        <td>
          <button className='btn btn-sm btn-outline-danger' onClick={() => reset(device.id)}>
            Reset
          </button>
        </td>
        </tr>) }
      </tbody>
  </table>;
}