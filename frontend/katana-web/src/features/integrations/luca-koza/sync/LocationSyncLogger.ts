/**
 * Location Sync Logger
 * Comprehensive logging for location synchronization operations
 * Includes correlation ID, timing, and detailed error tracking
 */

export interface LogContext {
  correlationId: string;
  operationName: string;
  startTime: number;
}

export interface LocationLogEntry {
  timestamp: string;
  correlationId: string;
  operation: string;
  level: 'info' | 'warn' | 'error' | 'debug';
  message: string;
  data?: Record<string, any>;
  duration?: number;
  error?: {
    code?: string;
    status?: number;
    message: string;
    details?: any;
  };
}

/**
 * Location Sync Logger Class
 * Provides structured logging for location synchronization operations
 */
export class LocationSyncLogger {
  private logs: LocationLogEntry[] = [];
  private readonly maxLogs = 1000; // Keep last 1000 logs in memory

  /**
   * Generate unique correlation ID
   */
  static generateCorrelationId(): string {
    return `loc-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Create logging context for an operation
   */
  createContext(operationName: string): LogContext {
    return {
      correlationId: LocationSyncLogger.generateCorrelationId(),
      operationName,
      startTime: Date.now(),
    };
  }

  /**
   * Log location before sending to API
   */
  logLocationAttempt(
    context: LogContext,
    location: any,
    payload: any
  ): void {
    const message = `[Location Sync] Attempting sync for location: ${location.code || location.id}`;
    
    this._log(context, 'info', message, {
      locationId: location.id,
      locationCode: location.code,
      locationName: location.name,
      payloadSize: JSON.stringify(payload).length,
      payload: payload,
    });

    console.log(message, {
      code: location.code,
      name: location.name,
      payload,
    });
  }

  /**
   * Log successful location sync
   */
  logLocationSuccess(
    context: LogContext,
    location: any,
    response: any,
    duration: number
  ): void {
    const message = `[Location Sync] Successfully synced location: ${location.code || location.id}`;

    this._log(context, 'info', message, {
      locationId: location.id,
      locationCode: location.code,
      responseData: response,
      duration,
    });

    console.log(message, {
      code: location.code,
      responseData: response,
      durationMs: duration,
    });
  }

  /**
   * Log location sync failure
   */
  logLocationError(
    context: LogContext,
    location: any,
    error: any,
    duration: number
  ): void {
    const errorBody = this._extractErrorBody(error);
    const message = `[Location Sync] Failed to sync location: ${location.code || location.id}`;

    this._log(context, 'error', message, {
      locationId: location.id,
      locationCode: location.code,
      duration,
    }, {
      code: error?.response?.status || error?.status || 'UNKNOWN',
      status: error?.response?.status,
      message: error?.message || 'Unknown error',
      details: errorBody,
    });

    console.error(message, {
      code: location.code,
      status: error?.response?.status,
      error: errorBody,
      durationMs: duration,
    });
  }

  /**
   * Log batch operation start
   */
  logBatchStart(context: LogContext, totalCount: number): void {
    const message = `[Location Sync] Starting batch sync of ${totalCount} locations`;

    this._log(context, 'info', message, {
      totalLocations: totalCount,
    });

    console.log(message, { count: totalCount });
  }

  /**
   * Log batch operation completion
   */
  logBatchComplete(
    context: LogContext,
    results: {
      successful: number;
      failed: number;
      skipped: number;
      totalTime: number;
    }
  ): void {
    const message = `[Location Sync] Batch sync completed`;

    this._log(context, 'info', message, {
      successful: results.successful,
      failed: results.failed,
      skipped: results.skipped,
      totalDurationMs: results.totalTime,
      averageDurationMs: results.totalTime / (results.successful + results.failed) || 0,
    });

    console.log(message, {
      successful: results.successful,
      failed: results.failed,
      skipped: results.skipped,
      totalTimeMs: results.totalTime,
    });
  }

  /**
   * Log duplicate location detection
   */
  logDuplicateDetected(
    context: LogContext,
    location: any,
    existingId: any,
    existingName: string
  ): void {
    const message = `[Location Sync] Duplicate location code detected`;

    this._log(context, 'warn', message, {
      locationCode: location.code,
      existingId,
      existingName,
      action: 'skipped',
    });

    console.warn(message, {
      code: location.code,
      existingId,
      existingName,
    });
  }

  /**
   * Log API request timing
   */
  logRequestTiming(
    context: LogContext,
    method: string,
    endpoint: string,
    duration: number,
    statusCode?: number
  ): void {
    const message = `[Location Sync] ${method} ${endpoint}`;

    this._log(context, 'debug', message, {
      method,
      endpoint,
      duration,
      statusCode,
    });

    console.debug(message, {
      duration: `${duration}ms`,
      status: statusCode,
    });
  }

  /**
   * Log network error with details
   */
  logNetworkError(
    context: LogContext,
    error: any,
    requestDetails: any
  ): void {
    const message = `[Location Sync] Network error occurred`;

    this._log(context, 'error', message, {
      requestUrl: requestDetails.url,
      requestMethod: requestDetails.method,
    }, {
      code: 'NETWORK_ERROR',
      message: error?.message || 'Network request failed',
      details: {
        errorType: error?.constructor?.name,
        errorMessage: error?.message,
        stack: error?.stack?.split('\n').slice(0, 3).join('\n'),
      },
    });

    console.error(message, {
      url: requestDetails.url,
      method: requestDetails.method,
      error: error?.message,
    });
  }

  /**
   * Extract error body from response
   */
  private _extractErrorBody(error: any): any {
    try {
      // Try to get error response body
      if (error?.response?.data) {
        return error.response.data;
      }
      if (error?.message) {
        return { message: error.message };
      }
      return error;
    } catch {
      return { message: 'Unable to extract error details' };
    }
  }

  /**
   * Internal logging method
   */
  private _log(
    context: LogContext,
    level: 'info' | 'warn' | 'error' | 'debug',
    message: string,
    data?: Record<string, any>,
    error?: any
  ): void {
    const duration = Date.now() - context.startTime;

    const entry: LocationLogEntry = {
      timestamp: new Date().toISOString(),
      correlationId: context.correlationId,
      operation: context.operationName,
      level,
      message,
      data,
      duration,
      ...(error && { error }),
    };

    this.logs.push(entry);

    // Keep only recent logs
    if (this.logs.length > this.maxLogs) {
      this.logs = this.logs.slice(-this.maxLogs);
    }
  }

  /**
   * Get all logs
   */
  getLogs(): LocationLogEntry[] {
    return [...this.logs];
  }

  /**
   * Get logs for specific correlation ID
   */
  getLogsByCorrelationId(correlationId: string): LocationLogEntry[] {
    return this.logs.filter((log) => log.correlationId === correlationId);
  }

  /**
   * Export logs as JSON
   */
  exportLogs(): string {
    return JSON.stringify(this.logs, null, 2);
  }

  /**
   * Clear all logs
   */
  clearLogs(): void {
    this.logs = [];
  }

  /**
   * Get summary statistics
   */
  getSummary(): {
    totalLogs: number;
    errorCount: number;
    warningCount: number;
    avgDuration: number;
    uniqueCorrelationIds: number;
  } {
    return {
      totalLogs: this.logs.length,
      errorCount: this.logs.filter((l) => l.level === 'error').length,
      warningCount: this.logs.filter((l) => l.level === 'warn').length,
      avgDuration: this.logs.reduce((sum, l) => sum + (l.duration || 0), 0) / this.logs.length || 0,
      uniqueCorrelationIds: new Set(this.logs.map((l) => l.correlationId)).size,
    };
  }
}

// Global singleton instance
export const locationSyncLogger = new LocationSyncLogger();

export default locationSyncLogger;
