import React, { useState, useEffect } from 'react';
import {  useParams } from "react-router-dom";
import authService from './api-authorization/AuthorizeService'

export const AppStream = (props) => {
  const {id} = useParams();
  const [url, setUrl] = useState()
  useEffect(() => {
    authService.getAccessToken().then(
      token => fetch(`appstream?id=${id}`, {headers: !token ? {} : { 'Authorization': `Bearer ${token}` }})
                .then(
                  response => response.text().then(data => setUrl(data))
                )
    );
  }, [])

  return (
  <div>
    <h1 id="tabelLabel">App Stream #{id}</h1>
    {url && <iframe height={640} width={360} src={url} title="cvd" class=""></iframe>}
  </div>
);
}
