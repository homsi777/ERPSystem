import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './auth/ProtectedRoute.tsx';
import { LoginPage } from './pages/Login.tsx';
import { InventoryPage } from './pages/Inventory.tsx';
import { InventoryMovementsPage } from './pages/InventoryMovements.tsx';
import { HomePage } from './pages/Home.tsx';
import { CustomersPage } from './pages/Customers.tsx';
import { ChinaPage } from './pages/China.tsx';
import { DeliveryPage } from './pages/Delivery.tsx';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<Navigate to="/inventory" replace />} />
        <Route path="/home" element={<HomePage />} />
        <Route path="/inventory" element={<InventoryPage />} />
        <Route path="/inventory/movements" element={<InventoryMovementsPage />} />
        <Route path="/customers" element={<CustomersPage />} />
        <Route path="/customers/:customerId" element={<CustomersPage />} />
        <Route path="/china" element={<ChinaPage />} />
        <Route path="/china/new" element={<ChinaPage />} />
        <Route path="/china/:containerId" element={<ChinaPage />} />
        <Route path="/delivery" element={<DeliveryPage />} />
        <Route path="/delivery/:invoiceId" element={<DeliveryPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/inventory" replace />} />
    </Routes>
  );
}
