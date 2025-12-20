/**
 * LocationSyncLogger Test Suite
 * Unit tests for comprehensive logging functionality
 */

import { LocationSyncLogger } from './LocationSyncLogger';

describe('LocationSyncLogger', () => {
  let logger: LocationSyncLogger;

  beforeEach(() => {
    logger = new LocationSyncLogger();
  });

  describe('Context Creation', () => {
    it('should generate unique correlation IDs', () => {
      const id1 = LocationSyncLogger.generateCorrelationId();
      const id2 = LocationSyncLogger.generateCorrelationId();

      expect(id1).toMatch(/^loc-\d+-[a-z0-9]+$/);
      expect(id1).not.toBe(id2);
    });

    it('should create context with operation name', () => {
      const context = logger.createContext('testOperation');

      expect(context.operationName).toBe('testOperation');
      expect(context.correlationId).toBeDefined();
      expect(context.startTime).toBeGreaterThan(0);
    });
  });

  describe('Location Attempt Logging', () => {
    it('should log location attempt with payload', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test Location' };
      const payload = { kod: 'TEST', tanim: 'Test Location' };

      logger.logLocationAttempt(context, location, payload);

      const logs = logger.getLogs();
      expect(logs.length).toBeGreaterThan(0);

      const logEntry = logs[0];
      expect(logEntry.level).toBe('info');
      expect(logEntry.message).toContain('Attempting sync');
      expect(logEntry.data?.payload).toEqual(payload);
    });

    it('should include correlation ID in logs', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test' };

      logger.logLocationAttempt(context, location, {});

      const logs = logger.getLogs();
      expect(logs[0].correlationId).toBe(context.correlationId);
    });
  });

  describe('Success Logging', () => {
    it('should log successful location sync', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test' };
      const response = { depoId: 100, success: true };

      logger.logLocationSuccess(context, location, response, 250);

      const logs = logger.getLogs();
      const successLog = logs.find((l) => l.level === 'info' && l.message.includes('Successfully'));

      expect(successLog).toBeDefined();
      expect(successLog?.data?.responseData).toEqual(response);
      expect(successLog?.duration).toBe(250);
    });

    it('should include timing information', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test' };

      logger.logLocationSuccess(context, location, {}, 1234);

      const logs = logger.getLogs();
      const log = logs.find((l) => l.message.includes('Successfully'));

      expect(log?.duration).toBe(1234);
    });
  });

  describe('Error Logging', () => {
    it('should log location error with details', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test' };
      const error = new Error('Duplicate location');
      (error as any).response = {
        status: 409,
        data: {
          code: 'DUPLICATE_LOCATION',
          details: { existingId: 99 },
        },
      };

      logger.logLocationError(context, location, error, 150);

      const logs = logger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');

      expect(errorLog).toBeDefined();
      expect(errorLog?.error?.status).toBe(409);
      expect(errorLog?.error?.message).toBe('Duplicate location');
    });

    it('should extract error response body', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test' };
      const error = new Error('API Error');
      (error as any).response = {
        status: 400,
        data: {
          error: 'Validation failed',
          details: { field: 'code', message: 'Code required' },
        },
      };

      logger.logLocationError(context, location, error, 100);

      const logs = logger.getLogs();
      const errorLog = logs[0];

      expect(errorLog.error?.details?.error).toBe('Validation failed');
      expect(errorLog.error?.details?.details).toBeDefined();
    });

    it('should handle error without response object', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'TEST', name: 'Test' };
      const error = new Error('Network timeout');

      logger.logLocationError(context, location, error, 5000);

      const logs = logger.getLogs();
      const errorLog = logs[0];

      expect(errorLog.error?.message).toBe('Network timeout');
      expect(errorLog.error?.status).toBeUndefined();
    });
  });

  describe('Duplicate Detection Logging', () => {
    it('should log duplicate location detection', () => {
      const context = logger.createContext('syncLocation');
      const location = { id: '1', code: 'DUP', name: 'Duplicate' };

      logger.logDuplicateDetected(context, location, 99, 'Existing Location');

      const logs = logger.getLogs();
      const dupLog = logs.find((l) => l.message.includes('Duplicate'));

      expect(dupLog?.level).toBe('warn');
      expect(dupLog?.data?.locationCode).toBe('DUP');
      expect(dupLog?.data?.existingId).toBe(99);
    });
  });

  describe('Batch Operation Logging', () => {
    it('should log batch operation start', () => {
      const context = logger.createContext('syncLocations');

      logger.logBatchStart(context, 10);

      const logs = logger.getLogs();
      const startLog = logs.find((l) => l.message.includes('Starting batch'));

      expect(startLog?.data?.totalLocations).toBe(10);
    });

    it('should log batch operation completion with statistics', () => {
      const context = logger.createContext('syncLocations');

      logger.logBatchComplete(context, {
        successful: 8,
        failed: 2,
        skipped: 0,
        totalTime: 5000,
      });

      const logs = logger.getLogs();
      const completeLog = logs.find((l) => l.message.includes('Batch sync completed'));

      expect(completeLog?.data?.successful).toBe(8);
      expect(completeLog?.data?.failed).toBe(2);
      expect(completeLog?.data?.skipped).toBe(0);
      expect(completeLog?.data?.totalDurationMs).toBe(5000);
      expect(completeLog?.data?.averageDurationMs).toBeGreaterThan(0);
    });
  });

  describe('Request Timing Logging', () => {
    it('should log API request timing', () => {
      const context = logger.createContext('syncLocation');

      logger.logRequestTiming(context, 'POST', '/api/admin/koza/depots/create', 250, 201);

      const logs = logger.getLogs();
      const timingLog = logs.find((l) => l.message.includes('POST'));

      expect(timingLog?.data?.method).toBe('POST');
      expect(timingLog?.data?.endpoint).toBe('/api/admin/koza/depots/create');
      expect(timingLog?.data?.duration).toBe(250);
      expect(timingLog?.data?.statusCode).toBe(201);
    });
  });

  describe('Network Error Logging', () => {
    it('should log network errors with details', () => {
      const context = logger.createContext('syncLocation');
      const error = new Error('ETIMEDOUT');
      const requestDetails = { url: '/api/depots', method: 'POST' };

      logger.logNetworkError(context, error, requestDetails);

      const logs = logger.getLogs();
      const networkLog = logs.find((l) => l.message.includes('Network error'));

      expect(networkLog?.level).toBe('error');
      expect(networkLog?.error?.code).toBe('NETWORK_ERROR');
      expect(networkLog?.data?.requestUrl).toBe('/api/depots');
    });
  });

  describe('Log Retrieval and Export', () => {
    it('should retrieve all logs', () => {
      const context = logger.createContext('test');

      logger.logBatchStart(context, 5);
      logger.logLocationSuccess(context, { id: 1, code: 'TEST' }, {}, 100);

      const logs = logger.getLogs();

      expect(logs.length).toBe(2);
      expect(logs[0].level).toBe('info');
      expect(logs[1].level).toBe('info');
    });

    it('should filter logs by correlation ID', () => {
      const context1 = logger.createContext('operation1');
      const context2 = logger.createContext('operation2');

      logger.logBatchStart(context1, 1);
      logger.logBatchStart(context2, 2);

      const logs1 = logger.getLogsByCorrelationId(context1.correlationId);
      const logs2 = logger.getLogsByCorrelationId(context2.correlationId);

      expect(logs1.length).toBe(1);
      expect(logs2.length).toBe(1);
      expect(logs1[0].data?.totalLocations).toBe(1);
      expect(logs2[0].data?.totalLocations).toBe(2);
    });

    it('should export logs as JSON', () => {
      const context = logger.createContext('test');

      logger.logBatchStart(context, 3);

      const json = logger.exportLogs();
      const parsed = JSON.parse(json);

      expect(Array.isArray(parsed)).toBe(true);
      expect(parsed[0].level).toBe('info');
      expect(parsed[0].correlationId).toBe(context.correlationId);
    });

    it('should clear all logs', () => {
      const context = logger.createContext('test');

      logger.logBatchStart(context, 1);
      expect(logger.getLogs().length).toBe(1);

      logger.clearLogs();
      expect(logger.getLogs().length).toBe(0);
    });
  });

  describe('Statistics and Summary', () => {
    it('should provide summary statistics', () => {
      const context = logger.createContext('test');

      logger.logBatchStart(context, 5);
      logger.logLocationSuccess(context, { id: 1, code: 'A' }, {}, 100);
      logger.logLocationError(context, { id: 2, code: 'B' }, new Error('Error'), 200);

      const summary = logger.getSummary();

      expect(summary.totalLogs).toBe(3);
      expect(summary.errorCount).toBe(1);
      expect(summary.warningCount).toBe(0);
      expect(summary.uniqueCorrelationIds).toBe(1);
      expect(summary.avgDuration).toBeGreaterThan(0);
    });

    it('should handle empty logs in summary', () => {
      const summary = logger.getSummary();

      expect(summary.totalLogs).toBe(0);
      expect(summary.errorCount).toBe(0);
      expect(summary.avgDuration).toBe(0);
    });
  });

  describe('Log Size Management', () => {
    it('should maintain max log limit', () => {
      const context = logger.createContext('test');

      // Add logs exceeding maxLogs (1000)
      for (let i = 0; i < 1050; i++) {
        logger.logBatchStart(context, i);
      }

      const logs = logger.getLogs();
      expect(logs.length).toBeLessThanOrEqual(1000);
    });

    it('should keep most recent logs when limit exceeded', () => {
      const context = logger.createContext('test');

      // Add logs and check that oldest are removed
      for (let i = 0; i < 1010; i++) {
        logger.logBatchStart(context, i);
      }

      const logs = logger.getLogs();
      const lastLog = logs[logs.length - 1];

      // Last log should have highest number
      expect(lastLog.data?.totalLocations).toBeGreaterThan(990);
    });
  });
});
