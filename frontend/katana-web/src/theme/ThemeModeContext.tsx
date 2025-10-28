import React, { createContext, useContext } from "react";

export type ThemeModeContextType = {
  mode: "light" | "dark";
  toggleMode: () => void;
  setMode: (m: "light" | "dark") => void;
};

export const ThemeModeContext = createContext<ThemeModeContextType | undefined>(undefined);

export const useThemeMode = (): ThemeModeContextType => {
  const ctx = useContext(ThemeModeContext);
  if (!ctx) throw new Error("useThemeMode must be used within ThemeModeContext.Provider");
  return ctx;
};

