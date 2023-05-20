import React, { useState, useEffect } from 'react';
import authService from './api-authorization/AuthService'
import 'bootstrap-icons/font/bootstrap-icons.css';
import styles from '../styles/Apps.module.css' 

const AppsTable = ({apps, deleteAction}) => {
  return (
    <table className='table table-striped'>
      <thead>
        <tr>
          <th>Status</th>
          <th>Name</th>
          <th>Type</th>
          <th>Run</th>
        </tr>
      </thead>
      <tbody>
        {apps.length === 0 && <tr><td colSpan={4}>No apps installed</td></tr> }
        {apps.map(app =>
          <tr key={app.name}>  
            <td>{app.status}</td>
            <td>{app.packageName}</td>
            <td>{app.type}</td>
            <td>
              <div className='btn-group  ' role="group">
                <a href={`/apps/${app.id}`}  className="btn btn-outline-secondary text-success"><i className="bi bi-play-circle-fill"></i></a>
                <button onClick={() => deleteAction(app.id)} type="button" className="btn btn-outline-secondary text-danger"><i className="bi bi-trash-fill"></i></button>
              </div>
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}

const GeneralAppList = ({role}) => {
  const [data, setData] = useState([])
  const [file, setFile] = useState()
  const [reloadCounter, setReloadCounter] = useState(0)
  const [status, setStatus] = useState('ready')
  const reload = () => setReloadCounter(r => r+1)

  const listPath = role === 'admin' ? '/api/v1/admin/defaultapps' : '/api/v1/apk'
  const uploadPath = role === 'admin' ? '/api/v1/admin/upload' : '/api/v1/apk/upload'
  const deletePath = role === 'admin' ? '/api/v1/admin/defaultapps/' : '/api/v1/apk/'
  const deleteAction = (id) => authService.fetch(deletePath + id, {method: 'delete'}).then(r => reload())

  const contents = null === data
  ? <p><em>Loading...</em></p>
  : <AppsTable apps={data} {...{deleteAction}}/>;

  useEffect( () => {
    authService.fetch(listPath).then(response => response.json().then(data => setData(data)))
  }, [reloadCounter])

const upload = () => {
  if(!file) return;
  var data = new FormData()
  data.append('file', file)
  setStatus('uploading')
  authService.fetch(uploadPath, {
          method: 'POST',    
          body: data
        }).then(r => {
          reload()
          file.value = ''
          setStatus('ready')
        })
}
return (
  <div>
    <input placeholder='Search' />
    <legend>
      {role === 'admin' && "Default pre-installed Apps"} 
      {role === 'user' && "Your Apps"} 
      </legend>
    <p>
      <span style={{padding: 15, border: '1px solid #151515'}}>
        <input onChange={e => setFile(e.target.files[0] || null)} type='file' placeholder='Upload'/>
        <button className='btn btn-primary' onClick={upload} disabled={status==='uploading'}>
          Upload {status === 'uploading' && '...'}
        </button>
      </span>
    </p>
    {contents}
  </div>
);
}

export const Apps = props => <GeneralAppList role={'user'} /> 
export const DefaultApps = props => <GeneralAppList role={'admin'} /> 
