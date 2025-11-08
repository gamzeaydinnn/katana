import React from "react";

export const BrowserRouter: React.FC<any> = ({ children }) => (
  <div>{children}</div>
);
export const Routes: React.FC<any> = ({ children }) => <div>{children}</div>;
export const Route: React.FC<any> = ({ element }) => element || null;

export const Navigate: React.FC<any> = ({ to }) => <div>Navigate to {to}</div>;

export const useNavigate = () => {
  return () => {};
};

export const useLocation = () => ({ pathname: "/" });
