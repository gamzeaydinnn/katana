import React from "react";
import { render } from "@testing-library/react";
import App from "./App";

// Mock all components that might cause issues
jest.mock("./components/Login/Login", () => () => <div>Login Component</div>);
jest.mock("./components/Dashboard/Dashboard", () => () => (
  <div>Dashboard Component</div>
));

// Mock react-router-dom with a minimal implementation so tests don't require the real package
jest.mock("react-router-dom", () => ({
  BrowserRouter: ({ children }: any) => (
    <div data-testid="router">{children}</div>
  ),
  Routes: ({ children }: any) => <div>{children}</div>,
  Route: ({ element }: any) => element || null,
  useNavigate: () => () => {},
}));

test("renders application", () => {
  const { container } = render(<App />);
  expect(container).toBeTruthy();
});
