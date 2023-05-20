
import React, { useState, useEffect, Component } from 'react';
import _ from 'lodash'
import {Modal} from 'bootstrap'
import { DataTable } from './DataTable';


const AddUserWindow = ({ onClose}) => {

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
  
    const [email, setEmail] = useState("")
    const [password, setPassword] = useState("")
    const [error, setError] = useState("")
    
    const addUser = () => {
      fetch('api/v1/admin/users/create', {
        method: 'post', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
        body: "email=" + encodeURIComponent(email) + "&password=" + encodeURIComponent(password)
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
          <h5 className="modal-title">Create User</h5>
        </div>
        <div className="modal-body">
          <label className="form-label w-100">Email
            <input value={email} type="email" className="form-control" onChange={e => setEmail(e.target.value)} />
          </label>
          <label className="form-label w-100">Password
            <input value={password} type="password" className="form-control" onChange={e => setPassword(e.target.value)} />
          </label>
  
          {error && <div className="alert alert-danger" role="alert">
            {error}
          </div>}
        </div>
        
        <div className="modal-footer">
          <button type="button" className="btn btn-primary" onClick={addUser}>Save</button>
          <button type="button" className="btn btn-secondary" data-dismiss="modal" onClick={() => {
            ref.modal.dispose()
            onClose()
          }}>Close</button>
        </div>
      </div>
    </div>
  </div>
  }
  const UserActions = ({entry, handleResponse}) => {
    const userId = entry.id
  
    const deleteUser = () => {
      fetch('api/v1/admin/users/' + userId , {
        method: 'delete', 
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
      }).then(handleResponse);
    }
  
    return  <div  className='btn-group  ' role="group">
              <button className='btn btn-sm btn-outline-danger' onClick={() => deleteUser()}>
                <i className="bi bi-trash-fill"></i>
              </button>
    </div>
  }
const TopInfo = ({data, handleResponse}) => {
    const toggleRegistration = () => {
        fetch('api/v1/admin/users/settings' , {
          method: 'post', 
          body: 'registrationEnabled=' + !data.registrationEnabled,
          headers: {'Content-Type': 'application/x-www-form-urlencoded'},   
        }).then(handleResponse);
      }
    if(data.registrationEnabled === undefined) { // if not loaded yet
        return <></>
    }
    return <tr style={{cursor: 'pointer'}}><td onClick={toggleRegistration}>Registration {data.registrationEnabled ? <span className="badge bg-success">enabled <i className="bi bi-toggle-on"></i></span> : <span className="badge bg-danger">disabled <i className="bi bi-toggle-off"></i></span>}</td></tr>
}
export const UserList = () => {
    return <>
      <legend>Users</legend>
      <DataTable {...{path: 'admin/users', fields: ['id', 'email'], TopInfo, Actions: UserActions, CreateWindow: AddUserWindow}} />;
  </>
}