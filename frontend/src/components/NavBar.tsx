import { NavLink } from 'react-router-dom';

export default function NavBar() {
  return (
    <nav className="nav">
      <NavLink to="/" end>
        Class তালিকা
      </NavLink>
      <NavLink to="/my-holds">আমার আবেদন</NavLink>
      <NavLink to="/admin/classes/new">নতুন Class (Admin)</NavLink>
    </nav>
  );
}
