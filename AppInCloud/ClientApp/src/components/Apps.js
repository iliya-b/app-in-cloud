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

  const [data, setData] = useState(null)
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
return (
  <div>
    <input placeholder='Search' />
    <h1 id="tabelLabel">Apps</h1>
    <p>Installed android apps</p>
    {contents}
  </div>
);
}
