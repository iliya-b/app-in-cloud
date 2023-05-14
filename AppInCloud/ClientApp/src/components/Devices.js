
import React, { useState, useEffect, Component } from 'react';
import _ from 'lodash'
import {Modal} from 'bootstrap'
import { DataTable } from './DataTable';

const AddDeviceWindow = ({data, onClose}) => {

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
  
    const [target, setTarget] = useState(_.keys(data.targets)[0])
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
              {_.map(data.targets, (label,i) => <option key={i} value={i}>{label}</option>)}
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
  const DeviceActions = ({entry, handleResponse}) => {
    const deviceId = entry.id
    const reset = () => {
      fetch('api/v1/admin/devices/' + deviceId + '/reset', {
        method: 'post', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      }).then(handleResponse);
    }
    const deactivate = () => {
      fetch('api/v1/admin/devices/' + deviceId , {
        method: 'delete', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      }).then(handleResponse);
    }
    const switchDevice = () => {
      fetch('api/v1/admin/devices/' + deviceId + '/switch' , {
        method: 'post', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      }).then(handleResponse);
    }
    return  <div  className='btn-group  ' role="group">
              <button className='btn btn-sm btn-outline-danger' onClick={() => reset()}>
                Reset
              </button>
              {entry.isActive && entry.status === "enable" && <button title='device is on' className='btn btn-sm btn-outline-success' onClick={() => switchDevice()}>
                ON
              </button>}
              {entry.isActive && entry.status === "disable" && <button disabled title='device is turning off' className='btn btn-sm btn-outline-success'>
                TURNING OFF...
              </button>}
              {!entry.isActive && entry.status === "disable" && <button title='device is off' className='btn btn-sm btn-outline-danger' onClick={() => switchDevice()}>
                OFF
              </button>}
              {!entry.isActive && entry.status === "enable" && <button title='device is turning on' className='btn btn-sm btn-outline-danger' disabled>
                TURNING ON...
              </button>}
              <button className='btn btn-sm btn-outline-danger' onClick={() => deactivate()}>
                <i className="bi bi-trash-fill"></i>
              </button>
    </div>
  }
  
  const DeviceUsersFieldRenderer = ({entry, handleResponse}) => {
    const deviceId = entry.id
    const addUser = () => {
      var email = prompt("Enter email:");
      if (!email) return;
      fetch('api/v1/admin/devices/' + deviceId + '/assign', {
        method: 'post', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
        body: "userEmail=" + encodeURI(email)
      }).then(handleResponse);
    }
  
    
    const removeUser = (email) => {
      fetch('api/v1/admin/devices/' + deviceId + '/assign', {
        method: 'post', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
        body: "unassign=true&userEmail=" + encodeURI(email)
      }).then(handleResponse);
    }
  
    return <td key={'users'}>
      {entry.users.map(
        u => <span title='Delete'  key={u} className="badge bg-secondary">
          {u} <i className="bi bi-x-circle " style={{cursor: 'pointer'}} onClick={() => removeUser(u)} ></i>  
        </span>
      )}
    <button onClick={() => addUser()} className='btn btn-sm btn-outline-primary ms-2'>
      <i className="bi bi-plus-circle"></i>
    </button>
    </td>
  }
  
  export const DeviceList = () => {
    const TopInfo = ({data}) => data.count > 0 ? <tr><td colSpan={3}><font color="green">running tasks</font>: {data.count}</td></tr> :  <></>
  
    return <DataTable {...{path: 'admin/devices', fields: ['id', 'target', 'users'], TopInfo, Actions: DeviceActions, customFieldRenderers: {
      users: DeviceUsersFieldRenderer
    }, CreateWindow: AddDeviceWindow}} />;
  }
  
  
  