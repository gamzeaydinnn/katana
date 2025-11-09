import React from "react";
import { render } from "@testing-library/react";
import App from "./App";

// Mock all components that might cause issues
jest.mock("./components/Login/Login", () => () => <div>Login Component</div>);
jest.mock("./components/Dashboard/Dashboard", () => () => (
  <div>Dashboard Component</div>
));
jest.mock("./components/StockManagement/StockManagement", () => () => (
  <div>StockManagement</div>
));
jest.mock("./components/SyncManagement/SyncManagement", () => () => (
  <div>SyncManagement</div>
));
jest.mock("./components/Reports/Reports", () => () => <div>Reports</div>);
jest.mock("./components/Settings/Settings", () => () => <div>Settings</div>);
jest.mock("./components/AdminPanel/AdminPanel", () => () => (
  <div>AdminPanel</div>
));
jest.mock("./components/Luca/BranchSelector", () => () => (
  <div>BranchSelector</div>
));
jest.mock("./components/Layout/Sidebar", () => () => <div>Sidebar</div>);
jest.mock("./components/Layout/Header", () => () => <div>Header</div>);

// Mock ProtectedRoute
jest.mock("./components/Auth/ProtectedRoute", () => ({
  __esModule: true,
  default: ({ children }: any) => <div>{children}</div>,
}));

// Mock authService
jest.mock("./services/authService", () => ({
  loginToLuca: jest.fn().mockResolvedValue(true),
  initializeLucaSession: jest.fn().mockResolvedValue(true),
}));

// Mock react-router-dom
jest.mock("react-router-dom", () => ({
  BrowserRouter: ({ children }: any) => (
    <div data-testid="router">{children}</div>
  ),
  Routes: ({ children }: any) => <div>{children}</div>,
  Route: ({ element }: any) => element || null,
  useNavigate: () => jest.fn(),
  Navigate: ({ to }: any) => <div>Navigate to {to}</div>,
  useLocation: () => ({ pathname: "/", search: "", hash: "", state: null }),
}));

test("renders application", () => {
  const { container } = render(<App />);
  expect(container).toBeTruthy();
});
