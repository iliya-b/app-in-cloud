import React, { Component, useEffect, useState } from 'react';
import { Collapse, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import { LoginMenu } from './api-authorization/LoginMenu';
import authService from './api-authorization/AuthorizeService'
import './NavMenu.css';

export const NavMenu = () =>  {
  const displayName = NavMenu.name;

  const [collapsed, collapse] = useState(true)  
  const [isAdmin, setIsAdmin] = useState(false)  

  useEffect(() => {
    authService.getUser().then(r => r.role === "Admin" && setIsAdmin(true))
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
                <NavLink tag={Link} className="text-dark" to="/admin">Admin</NavLink>
              </NavItem>}
  
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/">Apps</NavLink>
              </NavItem>
              <LoginMenu>
              </LoginMenu>
            </ul>
          </Collapse>
        </Navbar>
      </header>
    );
}
