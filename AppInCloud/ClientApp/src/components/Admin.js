import React, { useState, useEffect, Component } from 'react';
import { DefaultApps } from './Apps';
import _ from 'lodash'
import { UserList } from './Users';
import { DeviceList } from './Devices';

export const Admin = () =>  {
    return (
      <div>
        <h3>Administration</h3>
        <legend>Devices </legend>
        <DeviceList/>
        <DefaultApps />
        <legend>Users </legend>
        <UserList />
      </div>
    );
}

