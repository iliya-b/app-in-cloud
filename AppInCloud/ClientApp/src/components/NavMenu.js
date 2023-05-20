import React, { Component, useEffect, useState } from 'react';
import { Collapse, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import { LoginMenu } from './api-authorization/LoginMenu';
import authService from './api-authorization/AuthService'
import './NavMenu.css';

export const NavMenu = () =>  {
  const displayName = NavMenu.name;

  const [collapsed, collapse] = useState(true)  
  const [isAdmin, setIsAdmin] = useState(false)  

  useEffect(() => {
    authService.getUser().then(r => setIsAdmin(r.isAdmin))
  }, [])

  const toggleNavbar = () =>  collapse(r => !r)
  
  return (
      <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white border-bottom box-shadow mb-3" container light>
          <NavbarBrand tag={Link} to="/">AppInCloud</NavbarBrand>
          <NavbarToggler onClick={toggleNavbar} className="mr-2" />
          <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!collapsed} navbar>
            <ul className="navbar-nav flex-grow">
              {isAdmin && <NavItem>
                <NavLink tag={Link} className="text-dark" to="/admin/users">Users</NavLink>
              </NavItem>}
              {isAdmin && <NavItem>
                <NavLink tag={Link} className="text-dark" to="/admin/devices">Devices</NavLink>
              </NavItem>}
  
              {isAdmin && <NavItem>
                <NavLink tag={Link} className="text-dark" to="/admin/defaultapps">Default Apps</NavLink>
              </NavItem>}
  
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/">My Apps</NavLink>
              </NavItem>
              
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/devices">My Devices</NavLink>
              </NavItem>
              <LoginMenu>
              </LoginMenu>
            </ul>
          </Collapse>
        </Navbar>
      </header>
    );
}
