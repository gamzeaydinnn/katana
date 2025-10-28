import React, { createContext, useCallback, useContext, useMemo, useState } from "react";
import { Backdrop, CircularProgress, Snackbar, Alert, AlertColor } from "@mui/material";

type ToastOptions = {
  message: string;
  severity?: AlertColor;
  durationMs?: number;
};

type FeedbackContextType = {
  showToast: (opts: ToastOptions | string) => void;
  setLoading: (loading: boolean) => void;
};

const FeedbackContext = createContext<FeedbackContextType | undefined>(undefined);

export const useFeedback = (): FeedbackContextType => {
  const ctx = useContext(FeedbackContext);
  if (!ctx) throw new Error("useFeedback must be used within FeedbackProvider");
  return ctx;
};

export const FeedbackProvider: React.FC<React.PropsWithChildren<{}>> = ({ children }) => {
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [toast, setToast] = useState<{ message: string; severity: AlertColor; durationMs: number } | null>(null);

  const showToast = useCallback((opts: ToastOptions | string) => {
    const normalized = typeof opts === "string" ? { message: opts } : opts;
    setToast({
      message: normalized.message,
      severity: normalized.severity ?? "info",
      durationMs: normalized.durationMs ?? 3000,
    });
    setOpen(true);
  }, []);

  const value = useMemo(() => ({ showToast, setLoading }), [showToast]);

  return (
    <FeedbackContext.Provider value={value}>
      {children}
      <Backdrop open={loading} sx={{ zIndex: (t) => t.zIndex.modal + 1, color: "#fff" }}>
        <CircularProgress color="inherit" thickness={4} />
      </Backdrop>
      <Snackbar
        open={open}
        autoHideDuration={toast?.durationMs}
        onClose={() => setOpen(false)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert onClose={() => setOpen(false)} severity={toast?.severity ?? "info"} variant="filled" sx={{ width: "100%" }}>
          {toast?.message}
        </Alert>
      </Snackbar>
    </FeedbackContext.Provider>
  );
};

