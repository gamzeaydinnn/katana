import React from "react";
import { Navigate } from "react-router-dom";
import { decodeJwtPayload, isJwtExpired } from "../../utils/jwt";

interface ProtectedRouteProps {
  children: React.ReactElement;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const token =
    typeof window !== "undefined" ? window.localStorage.getItem("authToken") : null;

  const payload = decodeJwtPayload(token);
  const valid = payload !== null && !isJwtExpired(payload);

  if (!valid) {
    try {
      if (typeof window !== "undefined") {
        window.localStorage.removeItem("authToken");
      }
    } catch {
      // ignore storage errors
    }
    return <Navigate to="/login" replace />;
  }

  return children;
};

export default ProtectedRoute;
