import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { decodeJwtPayload, isJwtExpired, getJwtRoles } from "../../utils/jwt";

interface ProtectedRouteProps {
  children: React.ReactElement;
  requiredRole?: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredRole }) => {
  const location = useLocation();
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
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  // Role-based access control
  const roles = getJwtRoles(payload).map((r) => r.toLowerCase());
  
  // If a requiredRole prop is not provided, automatically require 'admin' for /admin routes
  const roleNeeded = (requiredRole || (location.pathname.startsWith("/admin") ? "admin" : undefined))?.toLowerCase();

  if (roleNeeded) {
    // Admin her şeye erişebilir
    const hasAdminRole = roles.includes("admin");
    // Gereken role sahip mi kontrol et
    const hasRequiredRole = roles.includes(roleNeeded);
    
    if (!hasAdminRole && !hasRequiredRole) {
      // Do not clear token; just redirect to unauthorized page
      return <Navigate to="/unauthorized" replace state={{ from: location }} />;
    }
  }

  return children;
};

export default ProtectedRoute;
