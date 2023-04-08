import React, { useState, useEffect } from 'react';
import authService from './api-authorization/AuthorizeService'



const renderAppsTable = (apps) => {
  return (
    <table className='table table-striped' aria-labelledby="tabelLabel">
      <thead>
        <tr>
          <th>Status</th>
          <th>Name</th>
          <th>Type</th>
          <th>Run</th>
        </tr>
      </thead>
      <tbody>
        {apps.map(app =>
          <tr key={app.name}>  
            <td>{app.status}</td>
            <td>{app.name}</td>
            <td>{app.type}</td>
            <td> <a href={`/apps/${app.id}`}>[=&gt;]</a></td>
          </tr>
        )}
      </tbody>
    </table>
  );
}

export const Apps = (props) => {

  const [data, setData] = useState([])
  const [file, setFile] = useState()
  const contents = null === data
    ? <p><em>Loading...</em></p>
    : renderAppsTable(data);

  useEffect( () => {
     authService.getAccessToken().then(
      token => fetch('apk', {headers: !token ? {} : { 'Authorization': `Bearer ${token}` }})
                .then(
                  response => response.json().then(data => setData(data))
                )
    );

  }, [])

const upload = () => {
  if(!file) return;
  console.log(file)
  var data = new FormData()
  data.append('file', file)
  authService.getAccessToken().then(
    token => fetch('apk/upload', {
          method: 'POST',    
          headers: !token ? {} : { 'Authorization': `Bearer ${token}` },
          body: data
        }).then(r => alert('Uploaded'))
  )
}
return (
  <div>
    <input placeholder='Search' />
    <h1 id="tabelLabel">Apps </h1>
    <p>Installed android apps. <span style={{padding: 10, border: '1px solid #151515'}}><input onChange={e => setFile(e.target.files[0] || null)} type='file' placeholder='Upload'/><button onClick={upload}>Upload</button></span></p>
    {contents}
  </div>
);
}
