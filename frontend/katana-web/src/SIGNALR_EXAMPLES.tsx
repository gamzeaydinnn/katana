/**
 * SignalR Connection Error Handling Examples
 * Demonstrates how to use the advanced SignalR connection management
 */

// ============================================================================
// EXAMPLE 1: Using useSignalRConnection Hook
// ============================================================================

import { Alert } from '@mui/material';
import { SignalRStatusIndicator } from './components/SignalRStatusIndicator';
import { useSignalRConnection } from './hooks/useSignalRConnection';

function NotificationsComponent() {
  const {
    isConnected,
    status,
    error,
    retryCount,
    isPolling,
    reconnect,
  } = useSignalRConnection({
    maxRetries: 3,
    initialDelayMs: 1000,
    maxDelayMs: 30000,
    fallbackToPolling: true,
    pollingIntervalMs: 30000,
    showNotifications: true,
    onStatusChange: (newStatus) => {
      console.log('SignalR status changed:', newStatus);
    },
  });

  return (
    <div>
      {/* Status Indicator */}
      <SignalRStatusIndicator
        status={status}
        error={error}
        retryCount={retryCount}
        isPolling={isPolling}
        onClick={reconnect}
      />

      {/* Content */}
      {!isConnected && isPolling && (
        <Alert severity="info">
          Polling modunda çalışıyor. Gerçek zamanlı güncellemeler yavaş olabilir.
        </Alert>
      )}

      {status === 'error' && (
        <Alert severity="error">
          Bağlantı hatası: {error?.message}
          <button onClick={reconnect}>Yeniden Bağlan</button>
        </Alert>
      )}

      {isConnected && (
        <p>Gerçek zamanlı güncellemeler aktif</p>
      )}
    </div>
  );
}

// ============================================================================
// EXAMPLE 2: Advanced Configuration
// ============================================================================

// Placeholder functions for example purposes
const trackConnectionStatus = (status: string) => {
  console.log('Status tracked:', status);
};

const StatusBar = ({ status, onRetry }: { status: string; onRetry: () => void }) => (
  <div>Status: {status} <button onClick={onRetry}>Retry</button></div>
);

function AdvancedSignalRUsage() {
  const { status, reconnect } = useSignalRConnection({
    enabled: true,
    maxRetries: 5,                    // 5 deneme
    initialDelayMs: 1500,             // 1.5 saniye başlangıç
    maxDelayMs: 60000,                // 60 saniye maksimum
    fallbackToPolling: true,          // Polling'e geri dön
    pollingIntervalMs: 45000,         // 45 saniye polling
    showNotifications: true,          // Bildirim göster
    onStatusChange: (status) => {
      // Custom status tracking
      trackConnectionStatus(status);
    },
  });

  return (
    <div>
      {/* Status UI */}
      <StatusBar status={status} onRetry={reconnect} />

      {/* Your component content */}
    </div>
  );
}

// ============================================================================
// EXAMPLE 3: Multiple Hooks in Single Component
// ============================================================================

const Header = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
const WarningBanner = ({ children }: { children: React.ReactNode }) => (
  <Alert severity="warning">{children}</Alert>
);
const MainContent = () => <div>Main Content</div>;

function ComplexDashboard() {
  const signalR = useSignalRConnection({
    maxRetries: 3,
    showNotifications: true,
  });

  // Other hooks...

  return (
    <div>
      <Header>
        <SignalRStatusIndicator
          status={signalR.status}
          isPolling={signalR.isPolling}
          onClick={signalR.reconnect}
        />
      </Header>

      {!signalR.isConnected && signalR.isPolling && (
        <WarningBanner>
          Polling modunda çalışıyor. Bazı güncellemeler gecikmeli olabilir.
        </WarningBanner>
      )}

      <MainContent />
    </div>
  );
}

// ============================================================================
// EXAMPLE 4: Error Handling Strategies
// ============================================================================

const RealtimeChart = () => <div>Realtime Chart</div>;
const NotificationsList = ({ mode }: { mode: string }) => <div>Notifications ({mode})</div>;

function SmartErrorHandling() {
  const { isConnected, isPolling, reconnect } = useSignalRConnection({
    maxRetries: 3,
    fallbackToPolling: true,
    showNotifications: true,
  });

  // Block critical features only if error (not if polling)
  const canFetchRealTimeData = isConnected;
  const canContinueWithData = isConnected || isPolling;

  return (
    <div>
      {/* Critical operation - requires real SignalR */}
      {canFetchRealTimeData ? (
        <RealtimeChart />
      ) : (
        <div>
          <p>Gerçek zamanlı veri analizi geçici olarak kullanılamıyor</p>
          <button onClick={reconnect}>Yeniden Bağlan</button>
        </div>
      )}

      {/* Non-critical operation - works with polling */}
      {canContinueWithData ? (
        <NotificationsList mode={isConnected ? 'realtime' : 'polling'} />
      ) : (
        <p>Bildirimler şu anda kullanılamıyor</p>
      )}
    </div>
  );
}

// ============================================================================
// EXAMPLE 5: Connection Status Monitoring
// ============================================================================

const analytics = {
  track: (event: string, data: any) => console.log('Analytics:', event, data),
};

function ConnectionMonitor() {
  const { status, retryCount, isPolling } = useSignalRConnection({
    maxRetries: 3,
    onStatusChange: (newStatus) => {
      // Send to analytics
      analytics.track('signalr_status_change', {
        status: newStatus,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
      });
    },
  });

  const logEntry = {
    timestamp: new Date().toISOString(),
    status,
    retryCount,
    isPolling,
  };

  return (
    <div>
      <h2>SignalR Connection Status</h2>
      <table>
        <tr>
          <td>Status</td>
          <td>{status}</td>
        </tr>
        <tr>
          <td>Retry Count</td>
          <td>{retryCount}</td>
        </tr>
        <tr>
          <td>Polling Mode</td>
          <td>{isPolling ? 'Active' : 'Inactive'}</td>
        </tr>
        <tr>
          <td>Timestamp</td>
          <td>{logEntry.timestamp}</td>
        </tr>
      </table>
    </div>
  );
}

// ============================================================================
// FEATURE BREAKDOWN
// ============================================================================

/**
 * Exponential Backoff Retry Logic
 * ================================
 * - Attempt 1: 1.0 second  + jitter
 * - Attempt 2: 2.0 seconds + jitter
 * - Attempt 3: 4.0 seconds + jitter
 * - Attempt 4: 8.0 seconds + jitter
 * - Attempt 5: Max 30 seconds
 * 
 * Formula: delay = min(initialDelay * 2^attempt, maxDelay) + random(0-1000)
 * 
 * Benefits:
 * ✅ Prevents server overload during outages
 * ✅ Gives server time to recover
 * ✅ Reduces thundering herd problem
 * ✅ Jitter prevents synchronized retries
 */

/**
 * Fallback to Polling
 * ===================
 * If WebSocket connection fails after max retries:
 * 1. Switch to polling mode
 * 2. Check API health every 30 seconds
 * 3. Show "Polling Mode" status to user
 * 4. Continue app without real-time updates
 * 
 * This ensures:
 * ✅ App doesn't break on SignalR failure
 * ✅ User can still access features
 * ✅ Data is updated, just not in real-time
 * ✅ Reduced network overhead (30s vs live)
 */

/**
 * Connection Status States
 * ========================
 * 
 * disconnected: No connection, not attempting
 * connecting:   Attempting to establish connection
 * connected:    WebSocket active, real-time updates
 * polling:      Fallback mode, periodic updates
 * error:        Failed after max retries
 * 
 * Flow: disconnected → connecting → connected
 *                   ↘ error → polling
 */

/**
 * User Notifications
 * ==================
 * 
 * Connection Established:
 * "Gerçek zamanlı güncellemeler aktif"
 * 
 * Falling Back to Polling:
 * "Polling modunda çalışıyor. Güncellemeler yavaş olabilir."
 * 
 * Total Failure:
 * "Gerçek zamanlı güncellemeler kullanılamıyor. Sayfayı yenileyin."
 * 
 * Reconnected:
 * "Gerçek zamanlı güncellemeler yeniden bağlandı"
 */

/**
 * Console Logging
 * ===============
 * 
 * All connection attempts are logged with:
 * - Timestamp
 * - Attempt number
 * - Status changes
 * - Error details
 * - Retry delays
 * 
 * Example:
 * [SignalR Hook] 🔄 Attempting connection (attempt 1/3)...
 * [SignalR Hook] ❌ Connection attempt 1 failed: WebSocket error
 * [SignalR Hook] ⏳ Retrying in 1.2 seconds...
 */

const SignalRExamples = {
  NotificationsComponent,
  AdvancedSignalRUsage,
  ComplexDashboard,
  SmartErrorHandling,
  ConnectionMonitor,
};

export default SignalRExamples;
