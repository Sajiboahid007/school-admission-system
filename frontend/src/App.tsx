import { Routes, Route } from 'react-router-dom';
import NavBar from './components/NavBar';
import StudentIdBar from './components/StudentIdBar';
import ClassListPage from './pages/ClassListPage';
import CreateClassPage from './pages/CreateClassPage';
import HoldPage from './pages/HoldPage';
import MyHoldsPage from './pages/MyHoldsPage';
import NotFoundPage from './pages/NotFoundPage';

export default function App() {
  return (
    <div className="app">
      <header className="header">
        <h1>School Admission</h1>
        <StudentIdBar />
        <NavBar />
      </header>
      <main className="main">
        <Routes>
          <Route path="/" element={<ClassListPage />} />
          <Route path="/admin/classes/new" element={<CreateClassPage />} />
          <Route path="/holds/:holdId" element={<HoldPage />} />
          <Route path="/my-holds" element={<MyHoldsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
    </div>
  );
}
