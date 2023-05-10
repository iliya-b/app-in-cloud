import React, { useState, useEffect, Component } from 'react';
import authService from './api-authorization/AuthorizeService'
import { DefaultApps } from './Apps';
import _ from 'lodash'
import {Modal} from 'bootstrap'

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


const AddDeviceWindow = ({targets, onClose}) => {

  const ref = React.useRef(null);
  React.useEffect(() => {
    if (ref.current) {
      ref.modal = new Modal(ref.current, { backdrop: true })
      ref.modal.show()
    }
    return () => {
      if (ref.current && ref.modal) {
        ref.modal.dispose()
      }
    }
  }, [ref]);

  const [target, setTarget] = useState(_.keys(targets)[0])
  const [ram, setRam] = useState(1024)
  const [error, setError] = useState("")
  
  const addDevice = () => {
    fetch('api/v1/admin/devices/add', {
      method: 'post', 
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      body: "target=" + target + "&memory=" + ram
    }).then((r) => {
      if(r.status !== 200){
        r.text().then(t => setError(t))
      }else{
        ref.modal.dispose()
        onClose()
      }
    });
  }


  return <div className="modal" role="dialog" ref={ref}>
  <div className="modal-dialog" role="document">
    <div className="modal-content">
      <div className="modal-header">
        <h5 className="modal-title">Create Device</h5>
      </div>
      <div className="modal-body">
        <label className='form-label w-100'>Target OS
          <select className="form-select" aria-label="os target" value={target} onChange={e => setTarget(e.target.value)}>
            {_.map(targets, (label,i) => <option key={i} value={i}>{label}</option>)}
          </select>
        </label>
        <label className="form-label w-100">Memory
          <input value={ram} type="range" min={1024} step={256} max={4096} className="form-range" onChange={e => setRam(e.target.valueAsNumber)} />
          <span>{ram} MB</span>
        </label>

        {target === '_13_x86_64' && <div className="alert alert-info" role="alert">
          {'All devices will be relaunched when using Android >= 13'}
        </div>}
        {error && <div className="alert alert-danger" role="alert">
          {error}
        </div>}
      </div>
      
      <div className="modal-footer">
        <button type="button" className="btn btn-primary" onClick={addDevice}>Create device</button>
        <button type="button" className="btn btn-secondary" data-dismiss="modal" onClick={() => {
          ref.modal.dispose()
          onClose()
        }}>Close</button>
      </div>
    </div>
  </div>
</div>
}

export const DeviceList = () => {
  const [devicesInfo, setDevicesInfo] = useState({
    count: 0,
    list: [],
    targets: {}
  })
  const [error, setError] = useState("")
  const data = devicesInfo.list;
  const [reloadCounter, setReloadCounter] = useState(0)
  const [isCreateWindowVisible, setIsCreateWindowVisible] = useState(false)
  const toggleCreateWindow = () => setIsCreateWindowVisible(r => !r)
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
  
  const reset = (deviceId) => {
    authService.fetch('api/v1/admin/devices/' + deviceId + '/reset', {
      method: 'post', 
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
    }).then(handleResponse);
  }
  const deactivate = (deviceId) => {
    authService.fetch('api/v1/admin/devices/' + deviceId , {
      method: 'delete', 
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

  return <>
  {isCreateWindowVisible && <AddDeviceWindow onClose={toggleCreateWindow} targets={devicesInfo.targets} />}
  <table className='table table-bordered'>
      <thead>
        {devicesInfo.count > 0 && <tr><td colSpan={3}><font color="green">running tasks</font>: {devicesInfo.count}</td></tr>}
        {error  && <tr><td colSpan={3}><font color="red">{error}</font></td></tr>}
        <tr>
          <td>ID</td>
          <td>Target</td>
          <td>Users</td>
          <td>Actions 
            <button onClick={() => toggleCreateWindow()} className='btn btn-sm btn-outline-primary ms-2' title='Change amount'>
              <i className="bi bi-plus"></i>
            </button>
          </td>
        </tr>
      </thead>
      <tbody>
      {data && data.map(device => <tr key={device.id}>
        <td>{device.id}</td>
        <td>{device.target}</td>
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
          <div  className='btn-group  ' role="group">
            <button className='btn btn-sm btn-outline-danger' onClick={() => reset(device.id)}>
              Reset
            </button>
            <button className='btn btn-sm btn-outline-danger' onClick={() => deactivate(device.id)}>
              <i className="bi bi-trash-fill"></i>
            </button>
          </div>
        </td>
        </tr>) }
      </tbody>
  </table></>;
}