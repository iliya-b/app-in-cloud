import React, { useState, useEffect } from 'react';
import {  useParams } from "react-router-dom";

export const AppStream = (props) => {
  const {id} = useParams();
  const [data, setData] = useState({
    url: null,
    deviceId: null
  })
  const url = data.url
  const deviceId= data.deviceId
  const [error, setError] = useState(null)
  useEffect(() => {
      fetch(`api/v1/appstream?id=${id}`)
                .then(
                  response => {
                      response.json().then(data => {
                        if(response.status == 200){
                          setData(data)
                        }else{
                          setError(data)
                        }
                      })
                  }
                )
  }, [])

  return (
  <div>
    <h1 >App Stream #{id}</h1>
    {url && <iframe onLoad={e => { 
            const w = e.target.contentWindow
            w.WebSocket = new Proxy(w.WebSocket, {
              construct (w, args) {
                const url = args[0].replace('/list_devices', '/list_devices/' + deviceId ).replace('/connect_client', '/connect_client/' + deviceId);
                return new WebSocket(url);
              }
            });
            w.UpdateDeviceList()
       }} height={640} width={360} src={url} title={deviceId}  ></iframe>}
    {error && <font color="red">{error}</font>}
  </div>
);
}
