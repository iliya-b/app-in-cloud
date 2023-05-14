import React, { useState, useEffect, Component } from 'react';
import { DefaultApps } from './Apps';
import _ from 'lodash'
import {Modal} from 'bootstrap'


export const DataTable = ({ fields, customFieldRenderers, path, Actions, TopInfo, CreateWindow }) => {


    const createAction = () => {

    }
    const updateAction = () => {

    }


    const [data, setData] = useState({
        count: 0,
        list: [],
    })
    const [isCreateWindowShown, setIsCreateWindowShown] = useState(false) 
    const toggleCreateWindow = () => {
      setIsCreateWindowShown(r => !r)
      reload()
    }
    const [error, setError] = useState("")

    
    const [reloadCounter, setReloadCounter] = useState(0)
    const reload = () => setReloadCounter(r => r+1)
    useEffect( () => {
        fetch('api/v1/' + path).then(r => r.json().then(data => {
            setData(data)
            setError(null)
        } ))
    }, [reloadCounter]);

    const handleResponse = r => {
        if(r.status !== 200){
            r.json().then(data => {
              setError(data.errors && _.join(_.map(data.errors, v => v.length && v[0]), ','))
            }).catch(t => {
              // todo logging
            });
        }else{
            reload()
        }
    }

    const DefaultRenderer = ({entry, field}) => <td key={field}>{entry[field]}</td>
    return <> 
    {isCreateWindowShown && <CreateWindow onClose={toggleCreateWindow} data={data} />}
    <table className='table table-bordered'>
    <thead>
      <TopInfo {...{handleResponse, data}} />
      {error ? <tr><td colSpan={3}><font color="red">{error}</font></td></tr> : <></>} 
      <tr>
        {fields.map(field => <td key={field}>{field}</td>)}
        <td key={'actions'}>Actions

          <button onClick={() => toggleCreateWindow()} className='btn btn-sm btn-outline-primary ms-2' title='Create'>
            <i className="bi bi-plus"></i>
          </button>
        </td>
      </tr>
    </thead>
    <tbody>
    {data.list && data.list.map(entry => <tr key={entry.id}>
        {fields.map(field => {
          const Renderer = _.has(customFieldRenderers, field) ? customFieldRenderers[field] : DefaultRenderer
          return <Renderer key={field} {...{entry, field, handleResponse}} />
        })}
        <td key={'actions'}>
            <Actions {...{handleResponse, entry}} />
        </td>
      </tr>) }
    </tbody>
</table></>;

}