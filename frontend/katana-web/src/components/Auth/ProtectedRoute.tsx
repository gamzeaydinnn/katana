import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { decodeJwtPayload, isJwtExpired, getJwtRoles } from "../../utils/jwt";

interface ProtectedRouteProps {
  children: React.ReactElement;
  requiredRole?: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRole,
}) => {
  const location = useLocation();
  const token =
    typeof window !== "undefined"
      ? window.localStorage.getItem("authToken")
      : null;

  const payload = decodeJwtPayload(token);
  const valid = payload !== null && !isJwtExpired(payload);

  if (!valid) {
    try {
      if (typeof window !== "undefined") {
        window.localStorage.removeItem("authToken");
      }
    } catch {
      
    }
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  
  const roles = getJwtRoles(payload).map((r) => r.toLowerCase());

  
  let roleNeeded = requiredRole?.toLowerCase();
  if (!roleNeeded && location.pathname.startsWith("/admin")) {
    
    const hasAdminPanelAccess =
      roles.includes("admin") ||
      roles.includes("manager") ||
      roles.includes("stokyonetici");
    if (!hasAdminPanelAccess) {
      return <Navigate to="/unauthorized" replace state={{ from: location }} />;
    }
    
    return children;
  }

  if (roleNeeded) {
    
    const hasAdminRole = roles.includes("admin");
    
    const hasRequiredRole = roles.includes(roleNeeded);

    if (!hasAdminRole && !hasRequiredRole) {
      
      return <Navigate to="/unauthorized" replace state={{ from: location }} />;
    }
  }

  return children;
};

export default ProtectedRoute;
