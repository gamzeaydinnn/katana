/**
 * Global Error Logger
 * Tüm browser console errors'larını capture etme
 */

interface LogEntry {
  timestamp: string;
  level: 'error' | 'warning' | 'info' | 'debug';
  message: string;
  source?: string;
  lineno?: number;
  colno?: number;
  stack?: string;
  context?: any;
}

let errorLogs: LogEntry[] = [];

const getTimestamp = () => new Date().toISOString();

/**
 * Hataları localStorage ve console'a logla
 */
export const logError = (entry: LogEntry) => {
  const fullEntry = {
    ...entry,
    timestamp: entry.timestamp || getTimestamp(),
  };

  errorLogs.push(fullEntry);

  // LocalStorage'a kaydet (son 50 hata)
  try {
    const stored = localStorage.getItem('app_error_logs');
    const logs: LogEntry[] = stored ? JSON.parse(stored) : [];
    logs.push(fullEntry);
    if (logs.length > 50) logs.shift();
    localStorage.setItem('app_error_logs', JSON.stringify(logs));
  } catch (e) {
    console.error('[ErrorLogger] Failed to save to localStorage:', e);
  }

  // Console'a da yaz
  const prefix = `[${fullEntry.level.toUpperCase()}]`;
  console.log(
    `${prefix} ${fullEntry.timestamp} - ${fullEntry.message}`,
    fullEntry.context
  );
};

/**
 * Global error handler setup
 */
export const setupGlobalErrorHandlers = () => {
  // Uncaught errors
  window.addEventListener('error', (event: ErrorEvent) => {
    logError({
      timestamp: getTimestamp(),
      level: 'error',
      message: event.message,
      source: event.filename,
      lineno: event.lineno,
      colno: event.colno,
      stack: event.error?.stack,
      context: {
        type: 'uncaughtError',
        error: event.error?.toString(),
      },
    });
  });

  // Unhandled promise rejections
  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    logError({
      timestamp: getTimestamp(),
      level: 'error',
      message: 'Unhandled Promise Rejection',
      stack: event.reason?.stack,
      context: {
        type: 'unhandledRejection',
        reason: event.reason?.toString(),
      },
    });
  });

  // Console.error override
  const originalError = console.error;
  console.error = (...args: any[]) => {
    logError({
      timestamp: getTimestamp(),
      level: 'error',
      message: args[0]?.toString() || 'Console error',
      context: args.slice(1),
    });
    originalError.apply(console, args);
  };

  // Console.warn override
  const originalWarn = console.warn;
  console.warn = (...args: any[]) => {
    logError({
      timestamp: getTimestamp(),
      level: 'warning',
      message: args[0]?.toString() || 'Console warning',
      context: args.slice(1),
    });
    originalWarn.apply(console, args);
  };
};

/**
 * Tüm hataları al
 */
export const getAllErrors = (): LogEntry[] => {
  try {
    const stored = localStorage.getItem('app_error_logs');
    return stored ? JSON.parse(stored) : errorLogs;
  } catch {
    return errorLogs;
  }
};

/**
 * Hataları temizle
 */
export const clearErrors = () => {
  errorLogs = [];
  try {
    localStorage.removeItem('app_error_logs');
  } catch {}
};

/**
 * Hataları dosyaya indir
 */
export const downloadErrorLog = () => {
  const logs = getAllErrors();
  const content = JSON.stringify(logs, null, 2);
  const blob = new Blob([content], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `error-log-${new Date().toISOString().split('T')[0]}.json`;
  a.click();
  URL.revokeObjectURL(url);
};
