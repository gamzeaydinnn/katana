import React from "react";
import { Navigate } from "react-router-dom";

interface ProtectedRouteProps {
  children: React.ReactElement;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const token = typeof window !== "undefined" ? localStorage.getItem("authToken") : null;

  // Require a plausible JWT (three segments) and not expired
  const isValidJwt = (t: string | null) => {
    if (!t || typeof t !== "string") return false;
    const parts = t.split(".");
    if (parts.length !== 3) return false;
    try {
      const payload = JSON.parse(atob(parts[1].replace(/-/g, "+").replace(/_/g, "/")));
      if (payload && typeof payload.exp === "number") {
        const nowSeconds = Math.floor(Date.now() / 1000);
        return payload.exp > nowSeconds;
      }
      // If no exp claim, accept shape but consider it valid for routing
      return true;
    } catch {
      return false;
    }
  };

  if (!isValidJwt(token)) {
    try { localStorage.removeItem("authToken"); } catch {}
    return <Navigate to="/login" replace />;
  }

  return children;
};

export default ProtectedRoute;
