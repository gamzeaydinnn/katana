import {
  render,
  screen,
  fireEvent,
  waitFor,
  within,
} from "@testing-library/react";
import { rest } from "msw";
import { setupServer } from "msw/node";
import FailedRecords from "../../../components/Admin/FailedRecords";
import api from "../../../services/api";

/**
 * FailedRecords Component Integration Tests
 *
 * Test Coverage:
 * 1. Component renders and fetches data
 * 2. Filtering by status and recordType
 * 3. Pagination
 * 4. View details dialog
 * 5. Resolve dialog workflow
 * 6. Ignore workflow
 * 7. Retry action
 * 8. API error handling
 */

// Mock API responses
const mockFailedRecords = {
  total: 2,
  page: 1,
  pageSize: 25,
  items: [
    {
      id: 1,
      recordType: "STOCK",
      recordId: "TEST-001",
      sourceSystem: "Katana",
      errorMessage: "Validation failed: Quantity cannot be negative",
      errorCode: "VAL-001",
      failedAt: "2025-01-15T10:30:00Z",
      retryCount: 0,
      lastRetryAt: null,
      status: "FAILED",
      resolvedAt: null,
      resolvedBy: null,
      integrationLogId: 100,
    },
    {
      id: 2,
      recordType: "ORDER",
      recordId: "TEST-002",
      sourceSystem: "Luca",
      errorMessage: "Customer not found",
      errorCode: "CUST-404",
      failedAt: "2025-01-15T11:00:00Z",
      retryCount: 2,
      lastRetryAt: "2025-01-15T11:30:00Z",
      status: "RETRYING",
      resolvedAt: null,
      resolvedBy: null,
      integrationLogId: 101,
    },
  ],
};

const mockFailedRecordDetail = {
  ...mockFailedRecords.items[0],
  originalData: JSON.stringify(
    {
      sku: "TEST-SKU-001",
      quantity: -1,
      productName: "Test Product",
    },
    null,
    2
  ),
  nextRetryAt: null,
  resolution: null,
  integrationLog: {
    id: 100,
    syncType: "KATANA_TO_LUCA",
    status: "FAILED",
    startTime: "2025-01-15T10:30:00Z",
  },
};

// MSW Server setup
const server = setupServer(
  rest.get("/api/adminpanel/failed-records", (req, res, ctx) => {
    try {
      const status = req.url.searchParams.get("status");
      const recordType = req.url.searchParams.get("recordType");

      let filteredItems = [...mockFailedRecords.items];
      if (status) {
        filteredItems = filteredItems.filter((item) => item.status === status);
      }
      if (recordType) {
        filteredItems = filteredItems.filter(
          (item) => item.recordType === recordType
        );
      }

      return res(
        ctx.status(200),
        ctx.json({
          ...mockFailedRecords,
          items: filteredItems,
          total: filteredItems.length,
        })
      );
    } catch (error) {
      console.error("MSW handler error:", error);
      return res(ctx.status(500), ctx.json({ error: "Internal server error" }));
    }
  }),

  rest.get("/api/adminpanel/failed-records/:id", (req, res, ctx) => {
    return res(ctx.status(200), ctx.json(mockFailedRecordDetail));
  }),

  rest.put("/api/adminpanel/failed-records/:id/resolve", (req, res, ctx) => {
    return res(
      ctx.status(200),
      ctx.json({ success: true, message: "Record resolved successfully" })
    );
  }),

  rest.put("/api/adminpanel/failed-records/:id/ignore", (req, res, ctx) => {
    return res(
      ctx.status(200),
      ctx.json({ success: true, message: "Record ignored successfully" })
    );
  }),

  rest.post("/api/adminpanel/failed-records/:id/retry", (req, res, ctx) => {
    return res(
      ctx.status(200),
      ctx.json({ success: true, message: "Retry initiated", retryCount: 1 })
    );
  })
);

beforeAll(() => server.listen({ onUnhandledRequest: "warn" }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe("FailedRecords Component", () => {
  // Test 1: Component Renders and Fetches Data
  test("renders component and displays failed records table", async () => {
    render(<FailedRecords />);

    expect(screen.getByText("Hatalı Kayıtlar")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
      expect(screen.getByText("TEST-002")).toBeInTheDocument();
    });

    expect(
      screen.getByText("Validation failed: Quantity cannot be negative")
    ).toBeInTheDocument();
    expect(screen.getByText("Customer not found")).toBeInTheDocument();
  });

  // Test 2: Status Filter
  test("filters records by status", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Open status filter
    const statusFilter = screen.getByLabelText("Durum");
    fireEvent.mouseDown(statusFilter);

    // Select "Başarısız" (FAILED)
    const failedOption = screen.getByText("Başarısız");
    fireEvent.click(failedOption);

    await waitFor(() => {
      // Should only show FAILED records
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
      expect(screen.queryByText("TEST-002")).not.toBeInTheDocument();
    });
  });

  // Test 3: RecordType Filter
  test("filters records by record type", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Open record type filter
    const typeFilter = screen.getByLabelText("Kayıt Tipi");
    fireEvent.mouseDown(typeFilter);

    // Select "Stok" (STOCK)
    const stockOption = screen.getByText("Stok");
    fireEvent.click(stockOption);

    await waitFor(() => {
      // Should only show STOCK records
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
      expect(screen.queryByText("TEST-002")).not.toBeInTheDocument();
    });
  });

  // Test 4: View Details Dialog
  test("opens detail dialog when clicking view icon", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Click view icon (first Visibility icon)
    const viewButtons = screen.getAllByTestId("VisibilityIcon");
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
      expect(screen.getByText(/Validation failed/)).toBeInTheDocument();
      expect(screen.getByText("Orijinal Veri:")).toBeInTheDocument();
    });

    // Check if original data is displayed
    const dataField = screen.getByDisplayValue(/"sku": "TEST-SKU-001"/);
    expect(dataField).toBeInTheDocument();
  });

  // Test 5: Edit Corrected Data
  test("allows editing corrected data in detail dialog", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Open detail dialog
    const viewButtons = screen.getAllByTestId("VisibilityIcon");
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
    });

    // Find and edit the text field
    const originalDataField = screen.getByRole("textbox", {
      name: /Orijinal Veri/i,
    });
    const correctedData = JSON.stringify(
      {
        sku: "TEST-SKU-001",
        quantity: 10, // Fixed: positive quantity
        productName: "Test Product",
      },
      null,
      2
    );

    fireEvent.change(originalDataField, { target: { value: correctedData } });

    expect((originalDataField as HTMLTextAreaElement).value).toContain(
      '"quantity": 10'
    );
  });

  // Test 6: Resolve Dialog Workflow
  test("opens resolve dialog and submits resolution", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Click edit icon
    const editButtons = screen.getAllByTestId("EditIcon");
    fireEvent.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Düzelt ve Gönder")).toBeInTheDocument();
    });

    // Click "Düzelt ve Gönder" button
    const resolveButton = screen.getByRole("button", {
      name: /Düzelt ve Gönder/i,
    });
    fireEvent.click(resolveButton);

    await waitFor(() => {
      expect(screen.getByText("Hatayı Çöz")).toBeInTheDocument();
    });

    // Enter resolution
    const resolutionField = screen.getByLabelText("Çözüm Açıklaması");
    fireEvent.change(resolutionField, {
      target: { value: "Negatif miktar düzeltildi ve pozitif yapıldı" },
    });

    // Select "Evet, yeniden gönder"
    const resendSelect = screen.getByLabelText(
      "Düzeltilmiş veriyi yeniden gönder"
    );
    fireEvent.mouseDown(resendSelect);
    const yesOption = screen.getByText("Evet, yeniden gönder");
    fireEvent.click(yesOption);

    // Submit resolution
    const submitButton = screen.getByRole("button", { name: "Çöz" });
    fireEvent.click(submitButton);

    await waitFor(() => {
      // Dialog should close and records should refresh
      expect(screen.queryByText("Hatayı Çöz")).not.toBeInTheDocument();
    });
  });

  // Test 7: Ignore Workflow
  test("ignores a failed record with reason", async () => {
    // Mock window.prompt
    global.prompt = jest.fn(() => "Artık gerekli değil");

    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Open detail dialog
    const viewButtons = screen.getAllByTestId("VisibilityIcon");
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
    });

    // Click "Göz Ardı Et" button
    const ignoreButton = screen.getByRole("button", { name: /Göz Ardı Et/i });
    fireEvent.click(ignoreButton);

    await waitFor(() => {
      expect(global.prompt).toHaveBeenCalledWith("Göz ardı etme nedeni:");
    });

    // Dialog should close after ignore
    await waitFor(() => {
      expect(
        screen.queryByText("Hatalı Kayıt Detayları")
      ).not.toBeInTheDocument();
    });
  });

  // Test 8: Retry Action
  test("retries a failed record", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Click retry icon (RestartAlt icon)
    const retryButtons = screen.getAllByTestId("RestartAltIcon");
    fireEvent.click(retryButtons[0]);

    await waitFor(() => {
      // Records should refresh after retry
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });
  });

  // Test 9: Pagination
  test("handles pagination correctly", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Find pagination controls
    const pagination = screen.getByRole("navigation");
    expect(pagination).toBeInTheDocument();

    // Check page size selector
    const pageSizeSelect = screen.getByLabelText("Sayfa başına:");
    expect(pageSizeSelect).toBeInTheDocument();
  });

  // Test 10: Status Chip Colors
  test("displays correct color chips for different statuses", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    const statusChips = screen.getAllByText(/FAILED|RETRYING/);
    expect(statusChips.length).toBeGreaterThan(0);

    // FAILED should have error color
    const failedChip = screen.getByText("FAILED");
    expect(failedChip).toHaveClass("MuiChip-colorError");

    // RETRYING should have warning color
    const retryingChip = screen.getByText("RETRYING");
    expect(retryingChip).toHaveClass("MuiChip-colorWarning");
  });

  // Test 11: Refresh Button
  test("refreshes data when clicking refresh button", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Click refresh button
    const refreshButton = screen.getByTestId("RefreshIcon").closest("button");
    fireEvent.click(refreshButton!);

    await waitFor(() => {
      // Data should be refetched
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });
  });

  // Test 12: Error Handling
  test("handles API errors gracefully", async () => {
    // Override server with error response
    server.use(
      rest.get("/api/adminpanel/failed-records", (req, res, ctx) => {
        return res(
          ctx.status(500),
          ctx.json({ error: "Internal server error" })
        );
      })
    );

    render(<FailedRecords />);

    await waitFor(() => {
      // Component should handle error without crashing
      expect(screen.queryByText("TEST-001")).not.toBeInTheDocument();
    });
  });

  // Test 13: Loading State
  test("shows loading indicator while fetching data", () => {
    render(<FailedRecords />);

    // Should show loading initially
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });

  // Test 14: Empty State
  test("handles empty records list", async () => {
    server.use(
      rest.get("/api/adminpanel/failed-records", (req, res, ctx) => {
        return res(ctx.json({ total: 0, page: 1, pageSize: 25, items: [] }));
      })
    );

    render(<FailedRecords />);

    await waitFor(() => {
      // Table should be empty
      const rows = screen.queryAllByRole("row");
      // Only header row should exist
      expect(rows.length).toBe(1);
    });
  });
});

// Integration Test: Complete Error Correction Flow
describe("FailedRecords - Complete Workflow Integration", () => {
  test("complete error correction workflow from list to resolution", async () => {
    render(<FailedRecords />);

    // Step 1: View list
    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıtlar")).toBeInTheDocument();
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    // Step 2: Filter by FAILED status
    const statusFilter = screen.getByLabelText("Durum");
    fireEvent.mouseDown(statusFilter);
    fireEvent.click(screen.getByText("Başarısız"));

    // Step 3: Open detail dialog
    await waitFor(() => {
      const viewButtons = screen.getAllByTestId("VisibilityIcon");
      fireEvent.click(viewButtons[0]);
    });

    // Step 4: View error details
    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
      expect(screen.getByText(/Validation failed/)).toBeInTheDocument();
    });

    // Step 5: Edit corrected data
    const dataField = screen.getByRole("textbox", { name: /Orijinal Veri/i });
    const correctedData = JSON.stringify(
      {
        sku: "TEST-SKU-001",
        quantity: 10,
        productName: "Test Product",
      },
      null,
      2
    );
    fireEvent.change(dataField, { target: { value: correctedData } });

    // Step 6: Open resolve dialog
    const resolveButton = screen.getByRole("button", {
      name: /Düzelt ve Gönder/i,
    });
    fireEvent.click(resolveButton);

    // Step 7: Enter resolution
    await waitFor(() => {
      expect(screen.getByText("Hatayı Çöz")).toBeInTheDocument();
    });

    const resolutionField = screen.getByLabelText("Çözüm Açıklaması");
    fireEvent.change(resolutionField, {
      target: { value: "Düzeltildi: Negatif miktar pozitif yapıldı" },
    });

    // Step 8: Select resend option
    const resendSelect = screen.getByLabelText(
      "Düzeltilmiş veriyi yeniden gönder"
    );
    fireEvent.mouseDown(resendSelect);
    fireEvent.click(screen.getByText("Evet, yeniden gönder"));

    // Step 9: Submit resolution
    const submitButton = screen.getByRole("button", { name: "Çöz" });
    fireEvent.click(submitButton);

    // Step 10: Verify success
    await waitFor(() => {
      expect(screen.queryByText("Hatayı Çöz")).not.toBeInTheDocument();
      // Records should be refreshed
      expect(screen.getByText("Hatalı Kayıtlar")).toBeInTheDocument();
    });
  });
});
