/**
 * LocationSyncService Test Suite
 * Unit tests for location synchronization operations
 * 
 * Test cases:
 * 1. Successful location creation
 * 2. Duplicate location handling
 * 3. Invalid payload rejection
 * 4. Network error handling
 * 5. Katana API 400 error parsing
 * 6. Batch sync with mixed success/failure
 */

import { LocationSyncService } from './LocationSync';
import { locationSyncLogger } from './LocationSyncLogger';

// Mock interfaces matching real types
interface MockKatanaLocation {
  id: string | number;
  code: string;
  name: string;
  addressId?: string;
  isActive?: boolean;
}

interface MockDepoService {
  getirVeyaOlustur: jest.Mock;
}

// Mock depoService
let mockDepoService: MockDepoService;

// Mock mappers
jest.mock('../cards/DepoMapper', () => ({
  filterActiveLocations: (locations: MockKatanaLocation[]) =>
    locations.filter((l) => l.isActive !== false),
  mapKatanaLocationToKozaDepo: (location: MockKatanaLocation, kategoriKod: string) => ({
    kod: location.code,
    tanim: location.name,
    kategoriKod,
    addressId: location.addressId,
  }),
}));

jest.mock('../cards/DepoService', () => ({
  depoService: {
    getirVeyaOlustur: jest.fn(),
  },
}));

describe('LocationSyncService', () => {
  let service: LocationSyncService;

  beforeEach(() => {
    jest.clearAllMocks();
    locationSyncLogger.clearLogs();

    service = new LocationSyncService({
      defaultKategoriKod: 'GENEL',
      skipInactive: true,
    });

    // Get mock reference
    const DepoService = require('../cards/DepoService');
    mockDepoService = DepoService.depoService;
  });

  // ============================================================================
  // TEST 1: Successful Location Creation
  // ============================================================================
  describe('Test 1: Successful location creation', () => {
    it('should successfully create a new location', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '1',
        code: 'DEP001',
        name: 'Main Depot',
        isActive: true,
      };

      mockDepoService.getirVeyaOlustur.mockResolvedValue({
        depoId: 100,
        success: true,
      });

      const result = await service.syncLocation(mockLocation);

      expect(result.status).toBe('created');
      expect(result.kozaDepoId).toBe(100);
      expect(result.katanaId).toBe('1');
      expect(result.kozaKod).toBe('DEP001');
      expect(mockDepoService.getirVeyaOlustur).toHaveBeenCalledTimes(1);
    });

    it('should log successful creation with timing', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '1',
        code: 'DEP001',
        name: 'Main Depot',
      };

      mockDepoService.getirVeyaOlustur.mockResolvedValue({
        depoId: 100,
        success: true,
      });

      await service.syncLocation(mockLocation);

      const logs = locationSyncLogger.getLogs();
      const successLog = logs.find((l) => l.level === 'info' && l.message.includes('Successfully'));

      expect(successLog).toBeDefined();
      expect(successLog?.data?.duration).toBeLessThan(5000);
    });

    it('should handle existing location (already synced)', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '2',
        code: 'DEP002',
        name: 'Secondary Depot',
      };

      mockDepoService.getirVeyaOlustur.mockResolvedValue({
        depoId: 101,
        success: true,
      });

      const result = await service.syncLocation(mockLocation);

      expect(result.status).toBe('existing');
      expect(result.kozaDepoId).toBe(101);
    });
  });

  // ============================================================================
  // TEST 2: Duplicate Location Handling
  // ============================================================================
  describe('Test 2: Duplicate location handling', () => {
    it('should handle duplicate location code (409 Conflict)', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '3',
        code: 'DEP_DUP',
        name: 'Duplicate Depot',
      };

      const duplicateError = new Error('DUPLICATE_LOCATION');
      (duplicateError as any).response = {
        status: 409,
        data: {
          error: 'Depo kodu zaten mevcut',
          code: 'DUPLICATE_LOCATION',
          details: {
            locationCode: 'DEP_DUP',
            existingId: 99,
            existingName: 'Old Duplicate',
          },
        },
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(duplicateError);

      const result = await service.syncLocation(mockLocation);

      expect(result.status).toBe('error');
      expect(result.error).toBeDefined();

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');
      expect(errorLog?.error?.status).toBe(409);
    });

    it('should log duplicate detection with existing location details', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '4',
        code: 'DEP_DUP2',
        name: 'Another Duplicate',
      };

      const duplicateError = new Error('DUPLICATE_LOCATION');
      (duplicateError as any).response = {
        status: 409,
        data: {
          code: 'DUPLICATE_LOCATION',
          details: { existingId: 50, existingName: 'Existing' },
        },
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(duplicateError);

      await service.syncLocation(mockLocation);

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');

      expect(errorLog).toBeDefined();
      expect(errorLog?.error?.status).toBe(409);
      expect(errorLog?.error?.code).toBe('409');
    });
  });

  // ============================================================================
  // TEST 3: Invalid Payload Rejection
  // ============================================================================
  describe('Test 3: Invalid payload rejection', () => {
    it('should reject location with missing required fields', async () => {
      const invalidLocation: any = {
        id: '5',
        // Missing code
        name: 'Invalid Depot',
      };

      const validationError = new Error('Validation failed: code is required');
      (validationError as any).response = {
        status: 400,
        data: {
          error: 'Validation failed',
          code: 'VALIDATION_ERROR',
          details: {
            field: 'code',
            message: 'Code is required',
          },
        },
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(validationError);

      const result = await service.syncLocation(invalidLocation);

      expect(result.status).toBe('error');

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');
      expect(errorLog?.error?.status).toBe(400);
    });

    it('should log validation error details', async () => {
      const invalidLocation: any = {
        id: '6',
        code: '',
        name: 'Invalid',
      };

      const validationError = new Error('Empty code');
      (validationError as any).response = {
        status: 400,
        data: {
          error: 'Invalid payload',
          details: { field: 'code', message: 'Code cannot be empty' },
        },
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(validationError);

      await service.syncLocation(invalidLocation);

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');

      expect(errorLog?.error?.status).toBe(400);
      expect(errorLog?.error?.details).toBeDefined();
    });
  });

  // ============================================================================
  // TEST 4: Network Error Handling
  // ============================================================================
  describe('Test 4: Network error handling', () => {
    it('should handle network timeout', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '7',
        code: 'DEP007',
        name: 'Timeout Test',
      };

      const networkError = new Error('ETIMEDOUT: Connection timeout');
      (networkError as any).code = 'ETIMEDOUT';

      mockDepoService.getirVeyaOlustur.mockRejectedValue(networkError);

      const result = await service.syncLocation(mockLocation);

      expect(result.status).toBe('error');
      expect(result.error).toContain('Connection timeout');
    });

    it('should handle network connection refused', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '8',
        code: 'DEP008',
        name: 'Connection Refused',
      };

      const networkError = new Error('ECONNREFUSED: Connection refused');
      (networkError as any).code = 'ECONNREFUSED';

      mockDepoService.getirVeyaOlustur.mockRejectedValue(networkError);

      const result = await service.syncLocation(mockLocation);

      expect(result.status).toBe('error');
    });

    it('should log network error with details', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '9',
        code: 'DEP009',
        name: 'Network Test',
      };

      const networkError = new Error('Network unreachable');
      mockDepoService.getirVeyaOlustur.mockRejectedValue(networkError);

      await service.syncLocation(mockLocation);

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');

      expect(errorLog).toBeDefined();
      expect(errorLog?.duration).toBeLessThan(5000);
    });
  });

  // ============================================================================
  // TEST 5: Katana API 400 Error Parsing
  // ============================================================================
  describe('Test 5: Katana API 400 error parsing', () => {
    it('should parse and extract 400 error response', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '10',
        code: 'BAD_REQUEST',
        name: 'Bad Request Test',
      };

      const apiError = new Error('Bad Request');
      (apiError as any).response = {
        status: 400,
        data: {
          error: 'Geçersiz istek',
          code: 'INVALID_REQUEST',
          details: {
            field: 'kod',
            message: 'Kod formatı geçersiz',
          },
        },
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(apiError);

      await service.syncLocation(mockLocation);

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');

      expect(errorLog?.error?.status).toBe(400);
      expect(errorLog?.error?.message).toContain('Bad Request');
    });

    it('should handle malformed error response', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '11',
        code: 'MALFORMED_ERROR',
        name: 'Malformed Error',
      };

      const malformedError = new Error('Unknown error');
      (malformedError as any).response = {
        status: 400,
        data: null, // Malformed response
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(malformedError);

      const result = await service.syncLocation(mockLocation);

      expect(result.status).toBe('error');
      expect(result.error).toBeDefined();
    });

    it('should log full error body for debugging', async () => {
      const mockLocation: MockKatanaLocation = {
        id: '12',
        code: 'DEP012',
        name: 'Full Error Body',
      };

      const fullError = new Error('API Error');
      (fullError as any).response = {
        status: 400,
        data: {
          error: 'Validation failed',
          code: 'VALIDATION_ERROR',
          details: {
            errors: [
              { field: 'kod', message: 'Kod gerekli' },
              { field: 'tanim', message: 'Tanım gerekli' },
            ],
          },
        },
      };

      mockDepoService.getirVeyaOlustur.mockRejectedValue(fullError);

      await service.syncLocation(mockLocation);

      const logs = locationSyncLogger.getLogs();
      const errorLog = logs.find((l) => l.level === 'error');

      expect(errorLog?.error?.details).toBeDefined();
    });
  });

  // ============================================================================
  // TEST 6: Batch Sync with Mixed Success/Failure
  // ============================================================================
  describe('Test 6: Batch sync with mixed success/failure', () => {
    it('should sync multiple locations with mixed results', async () => {
      const locations: MockKatanaLocation[] = [
        { id: '1', code: 'BATCH_001', name: 'Success 1', isActive: true },
        { id: '2', code: 'BATCH_002', name: 'Success 2', isActive: true },
        { id: '3', code: 'BATCH_003', name: 'Duplicate', isActive: true },
        { id: '4', code: 'BATCH_004', name: 'Network Error', isActive: true },
        { id: '5', code: 'BATCH_005', name: 'Inactive', isActive: false },
      ];

      // Setup mock responses
      mockDepoService.getirVeyaOlustur
        .mockResolvedValueOnce({ depoId: 1, success: true })
        .mockResolvedValueOnce({ depoId: 2, success: true })
        .mockRejectedValueOnce(
          Object.assign(new Error('Duplicate'), {
            response: {
              status: 409,
              data: { code: 'DUPLICATE_LOCATION' },
            },
          })
        )
        .mockRejectedValueOnce(new Error('Network error'));

      const results = await service.syncLocations(locations);

      // Should skip inactive location
      expect(mockDepoService.getirVeyaOlustur).toHaveBeenCalledTimes(4);

      // Check results
      expect(results[0].status).toBe('created');
      expect(results[1].status).toBe('created');
      expect(results[2].status).toBe('error');
      expect(results[3].status).toBe('error');
    });

    it('should log batch operation with summary', async () => {
      const locations: MockKatanaLocation[] = [
        { id: '1', code: 'BATCH_A', name: 'Success', isActive: true },
        { id: '2', code: 'BATCH_B', name: 'Success', isActive: true },
        { id: '3', code: 'BATCH_C', name: 'Failed', isActive: true },
      ];

      mockDepoService.getirVeyaOlustur
        .mockResolvedValueOnce({ depoId: 1 })
        .mockResolvedValueOnce({ depoId: 2 })
        .mockRejectedValueOnce(new Error('Error'));

      await service.syncLocations(locations);

      const logs = locationSyncLogger.getLogs();
      const batchStartLog = logs.find((l) => l.message.includes('Starting batch'));
      const batchCompleteLog = logs.find((l) => l.message.includes('Batch sync completed'));

      expect(batchStartLog).toBeDefined();
      expect(batchStartLog?.data?.totalLocations).toBe(3);

      expect(batchCompleteLog).toBeDefined();
      expect(batchCompleteLog?.data?.successful).toBe(2);
      expect(batchCompleteLog?.data?.failed).toBe(1);
    });

    it('should track correlation ID across batch operations', async () => {
      const locations: MockKatanaLocation[] = [
        { id: '1', code: 'CORR_001', name: 'Test 1', isActive: true },
        { id: '2', code: 'CORR_002', name: 'Test 2', isActive: true },
      ];

      mockDepoService.getirVeyaOlustur
        .mockResolvedValueOnce({ depoId: 1 })
        .mockResolvedValueOnce({ depoId: 2 });

      await service.syncLocations(locations);

      const logs = locationSyncLogger.getLogs();
      const correlationIds = new Set(logs.map((l) => l.correlationId));

      // All logs should have same correlation ID for single batch operation
      expect(correlationIds.size).toBe(1);
    });

    it('should calculate timing statistics for batch', async () => {
      const locations: MockKatanaLocation[] = [
        { id: '1', code: 'TIME_001', name: 'Test 1', isActive: true },
        { id: '2', code: 'TIME_002', name: 'Test 2', isActive: true },
      ];

      mockDepoService.getirVeyaOlustur
        .mockResolvedValueOnce({ depoId: 1 })
        .mockResolvedValueOnce({ depoId: 2 });

      await service.syncLocations(locations);

      const logs = locationSyncLogger.getLogs();
      const batchCompleteLog = logs.find((l) => l.message.includes('Batch sync completed'));

      expect(batchCompleteLog?.data?.totalDurationMs).toBeGreaterThan(0);
      expect(batchCompleteLog?.data?.averageDurationMs).toBeGreaterThan(0);
    });
  });

  // ============================================================================
  // Additional Tests: Logger Functionality
  // ============================================================================
  describe('Logger functionality', () => {
    it('should export logs as JSON', async () => {
      const location: MockKatanaLocation = {
        id: '1',
        code: 'LOG_TEST',
        name: 'Logging Test',
      };

      mockDepoService.getirVeyaOlustur.mockResolvedValue({ depoId: 1 });

      await service.syncLocation(location);

      const logsJson = locationSyncLogger.exportLogs();
      const parsed = JSON.parse(logsJson);

      expect(Array.isArray(parsed)).toBe(true);
      expect(parsed.length).toBeGreaterThan(0);
    });

    it('should provide summary statistics', async () => {
      const location: MockKatanaLocation = {
        id: '1',
        code: 'STAT_TEST',
        name: 'Stats Test',
      };

      mockDepoService.getirVeyaOlustur.mockResolvedValue({ depoId: 1 });

      await service.syncLocation(location);

      const summary = locationSyncLogger.getSummary();

      expect(summary.totalLogs).toBeGreaterThan(0);
      expect(summary.errorCount).toBe(0);
      expect(summary.warningCount).toBe(0);
      expect(summary.uniqueCorrelationIds).toBeGreaterThan(0);
    });
  });
});
