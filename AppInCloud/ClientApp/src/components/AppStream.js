import React, { useState, useEffect } from 'react';
import authService from './api-authorization/AuthorizeService'
import {  useParams } from "react-router-dom";


export const AppStream = (props) => {
  const {id} = useParams();

  return (
  <div>
    <h1 id="tabelLabel">App Stream #{id}</h1>
    <canvas></canvas>
  </div>
);
}
