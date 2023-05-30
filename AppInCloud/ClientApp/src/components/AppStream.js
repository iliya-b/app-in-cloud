import React, { useState, useEffect } from 'react';
import {  useParams } from "react-router-dom";

export const AppStream = (props) => {
  const {id} = useParams();
  const [url, setUrl] = useState()
  const [error, setError] = useState(null)
  useEffect(() => {
      fetch(`api/v1/appstream?id=${id}`)
                .then(
                  response => {
                      response.text().then(data => {
                        if(response.status == 200){
                          setUrl(data)
                        }else{
                          setError(data)
                        }
                      })
                  }
                )
  }, [])
  // onLoad={e => 
  //   setTimeout(() => {
  //     e.target.contentWindow['connect_1'].click()
  //     } , 2500) }
  return (
  <div>
    <h1 >App Stream #{id}</h1>
    {url && <iframe height={640} width={360} src={url} title="cvd"  ></iframe>}
    {error && <font color="red">{error}</font>}
  </div>
);
}
