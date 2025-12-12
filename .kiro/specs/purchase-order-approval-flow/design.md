# Design Document

## Overview

Bu özellik, satınalma siparişlerinin admin onay sürecini frontend'e ekleyecek. Backend'de zaten mevcut olan `Pending -> Approved -> Received` durum geçişi mekanizmasını kullanarak, admin kullanıcıların siparişleri onaylamasını ve Katana sistemine stok olarak eklenmesini sağlayacak.

Mevcut backend endpoint'leri:

- `PATCH /api/purchase-orders/{id}/status` - Sipariş durumunu günceller
- `GET /api/purchase-orders` - Sipariş listesini getirir
- `GET /api/purchase-orders/{id}` - Sipariş detaylarını getirir
- `GET /api/purchase-orders/stats` - İstatistikleri getirir

## Architecture

### Component Structure

```
PurchaseOrders (Mevcut)
├── OrderList (Güncellenecek)
│   ├── StatusFilter (Yeni)
│   ├── OrderTable (Güncellenecek)
│   │   ├── StatusBadge (Yeni)
│   │   └── QuickActions (Güncellenecek)
│   └── StatsCards (Güncellenecek)
├── OrderDetail (Güncellenecek)
│   ├── OrderInfo
│   ├── OrderItems
│   ├── StatusActions (Yeni)
│   │   ├── ApproveButton (Yeni)
│   │   ├── ReceiveButton (Yeni)
│   │   └── StatusHistory (Yeni)
│   └── KatanaSyncStatus (Yeni)
└── CreateOrder (Mevcut)
```

### State Management

Mevcut React state yapısını kullanacağız:

- `orders`: Sipariş listesi
- `orderDetail`: Seçili sipariş detayı
- `loading`: Yükleme durumu
- `syncing`: Senkronizasyon durumu
- Yeni: `statusUpdating`: Durum güncelleme durumu

### API Integration

Backend endpoint'leri zaten mevcut, sadece frontend'den çağrılacak:

```typescript
// Durum güncelleme
PATCH /api/purchase-orders/{id}/status
Body: { newStatus: "Approved" | "Received" }

// Response
{
  message: string,
  katanaSyncResults?: {
    sku: string,
    success: boolean,
    action: "created" | "updated",
    error?: string
  }[]
}
```

## Components and Interfaces

### 1. StatusBadge Component

Sipariş durumunu görsel olarak gösterir.

```typescript
interface StatusBadgeProps {
  status: "Pending" | "Approved" | "Received" | "Cancelled";
  showLabel?: boolean;
  size?: "small" | "medium";
}

const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  showLabel,
  size,
}) => {
  const config = {
    Pending: { color: "warning", icon: <PendingIcon />, label: "Beklemede" },
    Approved: { color: "info", icon: <CheckIcon />, label: "Onaylandı" },
    Received: {
      color: "success",
      icon: <DoneAllIcon />,
      label: "Teslim Alındı",
    },
    Cancelled: { color: "error", icon: <CancelIcon />, label: "İptal" },
  };

  return (
    <Chip
      icon={config[status].icon}
      label={showLabel ? config[status].label : status}
      color={config[status].color}
      size={size}
    />
  );
};
```

### 2. StatusActions Component

Sipariş detayında durum değiştirme butonlarını gösterir.

```typescript
interface StatusActionsProps {
  order: PurchaseOrderDetail;
  onStatusChange: (newStatus: string) => Promise<void>;
  loading: boolean;
}

const StatusActions: React.FC<StatusActionsProps> = ({
  order,
  onStatusChange,
  loading,
}) => {
  const canApprove = order.status === "Pending";
  const canReceive = order.status === "Approved";

  return (
    <Box>
      {canApprove && (
        <Button
          variant="contained"
          color="primary"
          startIcon={<CheckIcon />}
          onClick={() => onStatusChange("Approved")}
          disabled={loading}
        >
          Siparişi Onayla
        </Button>
      )}

      {canReceive && (
        <Button
          variant="contained"
          color="success"
          startIcon={<DoneAllIcon />}
          onClick={() => onStatusChange("Received")}
          disabled={loading}
        >
          Teslim Alındı
        </Button>
      )}
    </Box>
  );
};
```

### 3. KatanaSyncStatus Component

Katana senkronizasyon durumunu gösterir.

```typescript
interface KatanaSyncResult {
  sku: string;
  productName: string;
  success: boolean;
  action: "created" | "updated";
  newStock?: number;
  error?: string;
}

interface KatanaSyncStatusProps {
  results: KatanaSyncResult[];
}

const KatanaSyncStatus: React.FC<KatanaSyncStatusProps> = ({ results }) => {
  const successCount = results.filter((r) => r.success).length;
  const failCount = results.filter((r) => !r.success).length;

  return (
    <Card>
      <CardHeader title="Katana Senkronizasyon Durumu" />
      <CardContent>
        <Box sx={{ mb: 2 }}>
          <Chip
            label={`${successCount} Başarılı`}
            color="success"
            size="small"
          />
          {failCount > 0 && (
            <Chip
              label={`${failCount} Hatalı`}
              color="error"
              size="small"
              sx={{ ml: 1 }}
            />
          )}
        </Box>

        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell>Ürün</TableCell>
              <TableCell>İşlem</TableCell>
              <TableCell>Durum</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {results.map((result, idx) => (
              <TableRow key={idx}>
                <TableCell>{result.sku}</TableCell>
                <TableCell>{result.productName}</TableCell>
                <TableCell>
                  {result.action === "created" ? "Oluşturuldu" : "Güncellendi"}
                  {result.newStock && ` (Stok: ${result.newStock})`}
                </TableCell>
                <TableCell>
                  {result.success ? (
                    <CheckCircleIcon color="success" fontSize="small" />
                  ) : (
                    <Tooltip title={result.error}>
                      <ErrorIcon color="error" fontSize="small" />
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
};
```

### 4. StatusFilter Component

Sipariş listesinde durum filtreleme.

```typescript
interface StatusFilterProps {
  value: string;
  onChange: (status: string) => void;
  stats: OrderStats;
}

const StatusFilter: React.FC<StatusFilterProps> = ({
  value,
  onChange,
  stats,
}) => {
  return (
    <FormControl size="small" sx={{ minWidth: 200 }}>
      <InputLabel>Sipariş Durumu</InputLabel>
      <Select
        value={value}
        label="Sipariş Durumu"
        onChange={(e) => onChange(e.target.value)}
      >
        <MenuItem value="all">Tümü ({stats.total})</MenuItem>
        <MenuItem value="Pending">Beklemede ({stats.pending})</MenuItem>
        <MenuItem value="Approved">Onaylandı ({stats.approved})</MenuItem>
        <MenuItem value="Received">Teslim Alındı ({stats.received})</MenuItem>
        <MenuItem value="Cancelled">İptal ({stats.cancelled})</MenuItem>
      </Select>
    </FormControl>
  );
};
```

## Data Models

### PurchaseOrderDetail (Güncellenecek)

```typescript
interface PurchaseOrderDetail {
  // Mevcut alanlar...
  id: number;
  orderNo: string;
  status: "Pending" | "Approved" | "Received" | "Cancelled";

  // Yeni alanlar
  statusHistory?: StatusHistoryEntry[];
  katanaSyncResults?: KatanaSyncResult[];
}

interface StatusHistoryEntry {
  status: string;
  changedBy: string;
  changedAt: string;
  notes?: string;
}
```

### UpdateStatusRequest

```typescript
interface UpdateStatusRequest {
  newStatus: "Approved" | "Received";
}

interface UpdateStatusResponse {
  success: boolean;
  message: string;
  order: PurchaseOrderDetail;
  katanaSyncResults?: KatanaSyncResult[];
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Status transition validity

_For any_ purchase order and any status transition request, the system should only allow valid transitions (Pending→Approved, Approved→Received) and reject invalid ones.

**Validates: Requirements 6.1, 6.2, 6.3**

### Property 2: Katana sync on approval

_For any_ purchase order that transitions to "Approved" status, the system should attempt to sync all order items to Katana and record the results.

**Validates: Requirements 1.4, 7.1**

### Property 3: UI state consistency

_For any_ purchase order detail view, the available action buttons should match the current order status (Approve button for Pending, Receive button for Approved, no buttons for Received).

**Validates: Requirements 1.2, 1.5, 4.1, 4.4**

### Property 4: Filter correctness

_For any_ status filter selection, the displayed orders should only include orders matching that status.

**Validates: Requirements 2.2, 2.3**

### Property 5: Status badge display

_For any_ order in the list, the status badge color and label should correctly reflect the order's current status.

**Validates: Requirements 5.1, 5.2, 5.3, 5.4**

### Property 6: Feedback message display

_For any_ status update operation, the system should display a success message when the operation succeeds and an error message when it fails.

**Validates: Requirements 3.1, 3.2**

### Property 7: Button state during operation

_For any_ status update operation in progress, the action button should be disabled and show a loading indicator.

**Validates: Requirements 3.3**

### Property 8: Auto-refresh after update

_For any_ successful status update, the system should automatically refresh the order details to show the new status.

**Validates: Requirements 3.4**

## Error Handling

### Status Update Errors

1. **Invalid Transition**: Backend zaten kontrol ediyor, frontend'de de kontrol eklenecek

   - Kullanıcıya: "Geçersiz durum değişikliği" mesajı
   - Log: Hata detayları

2. **Katana Sync Failure**: Bazı ürünler senkronize edilemeyebilir

   - Kullanıcıya: Başarılı/başarısız ürün listesi
   - Sipariş durumu yine de "Approved" olur
   - Log: Her ürün için sync sonucu

3. **Network Error**: API çağrısı başarısız olabilir

   - Kullanıcıya: "Bağlantı hatası, lütfen tekrar deneyin"
   - Retry mekanizması: Kullanıcı manuel tekrar deneyebilir

4. **Permission Error**: Kullanıcının yetkisi olmayabilir
   - Kullanıcıya: "Bu işlem için yetkiniz yok"
   - UI: Butonları gizle

### UI Error States

```typescript
interface ErrorState {
  type: "network" | "validation" | "permission" | "unknown";
  message: string;
  retryable: boolean;
}

const handleStatusUpdateError = (error: any): ErrorState => {
  if (error.response?.status === 403) {
    return {
      type: "permission",
      message: "Bu işlem için yetkiniz yok",
      retryable: false,
    };
  }

  if (error.response?.status === 400) {
    return {
      type: "validation",
      message: error.response.data.message || "Geçersiz işlem",
      retryable: false,
    };
  }

  if (!error.response) {
    return {
      type: "network",
      message: "Bağlantı hatası, lütfen tekrar deneyin",
      retryable: true,
    };
  }

  return {
    type: "unknown",
    message: "Beklenmeyen bir hata oluştu",
    retryable: true,
  };
};
```

## Testing Strategy

### Unit Tests

1. **StatusBadge Component**

   - Doğru renk ve icon gösterimi
   - Tüm durum tipleri için render

2. **StatusActions Component**

   - Pending durumunda Approve butonu gösterimi
   - Approved durumunda Receive butonu gösterimi
   - Received durumunda buton göstermeme
   - Loading durumunda buton disable

3. **StatusFilter Component**

   - Filtre değişikliği callback çağrısı
   - İstatistik sayılarının doğru gösterimi

4. **KatanaSyncStatus Component**
   - Başarılı/başarısız sayıların doğru hesaplanması
   - Hata mesajlarının tooltip'te gösterimi

### Integration Tests

1. **Status Update Flow**

   - Pending → Approved geçişi
   - Approved → Received geçişi
   - Geçersiz geçiş denemesi
   - API hata durumları

2. **Filter Functionality**

   - Filtre değişikliğinde API çağrısı
   - Doğru parametrelerle çağrı
   - Sonuçların doğru filtrelenmesi

3. **Katana Sync Display**
   - Sync sonuçlarının doğru gösterimi
   - Başarılı/başarısız durumların ayrımı
   - Hata mesajlarının gösterimi

### Property-Based Tests

Property-based testing için **fast-check** kütüphanesi kullanılacak.

1. **Property 1 Test**: Status transition validity

   - Generator: Random order status + random target status
   - Property: Only valid transitions should be allowed
   - Iterations: 100

2. **Property 3 Test**: UI state consistency

   - Generator: Random order with random status
   - Property: Available buttons should match status
   - Iterations: 100

3. **Property 4 Test**: Filter correctness

   - Generator: Random order list + random filter
   - Property: Filtered results should only contain matching status
   - Iterations: 100

4. **Property 5 Test**: Status badge display
   - Generator: Random order status
   - Property: Badge color/label should match status
   - Iterations: 100

### Manual Testing Scenarios

1. **Happy Path**

   - Sipariş oluştur
   - Pending durumunda görüntüle
   - Onayla
   - Katana sync sonuçlarını kontrol et
   - Teslim alındı olarak işaretle

2. **Error Scenarios**

   - Network hatası simülasyonu
   - Geçersiz durum geçişi denemesi
   - Katana sync hatası durumu

3. **UI/UX**
   - Loading states
   - Success/error messages
   - Button states
   - Filter functionality
