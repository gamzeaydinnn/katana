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

  
  test("filters records by status", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const statusFilter = screen.getByLabelText("Durum");
    fireEvent.mouseDown(statusFilter);

    
    const failedOption = screen.getByText("Başarısız");
    fireEvent.click(failedOption);

    await waitFor(() => {
      
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
      expect(screen.queryByText("TEST-002")).not.toBeInTheDocument();
    });
  });

  
  test("filters records by record type", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const typeFilter = screen.getByLabelText("Kayıt Tipi");
    fireEvent.mouseDown(typeFilter);

    
    const stockOption = screen.getByText("Stok");
    fireEvent.click(stockOption);

    await waitFor(() => {
      
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
      expect(screen.queryByText("TEST-002")).not.toBeInTheDocument();
    });
  });

  
  test("opens detail dialog when clicking view icon", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const viewButtons = screen.getAllByTestId("VisibilityIcon");
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
      expect(screen.getByText(/Validation failed/)).toBeInTheDocument();
      expect(screen.getByText("Orijinal Veri:")).toBeInTheDocument();
    });

    
    const dataField = screen.getByDisplayValue(/"sku": "TEST-SKU-001"/);
    expect(dataField).toBeInTheDocument();
  });

  
  test("allows editing corrected data in detail dialog", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const viewButtons = screen.getAllByTestId("VisibilityIcon");
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
    });

    
    const originalDataField = screen.getByRole("textbox", {
      name: /Orijinal Veri/i,
    });
    const correctedData = JSON.stringify(
      {
        sku: "TEST-SKU-001",
        quantity: 10, 
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

  
  test("opens resolve dialog and submits resolution", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const editButtons = screen.getAllByTestId("EditIcon");
    fireEvent.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Düzelt ve Gönder")).toBeInTheDocument();
    });

    
    const resolveButton = screen.getByRole("button", {
      name: /Düzelt ve Gönder/i,
    });
    fireEvent.click(resolveButton);

    await waitFor(() => {
      expect(screen.getByText("Hatayı Çöz")).toBeInTheDocument();
    });

    
    const resolutionField = screen.getByLabelText("Çözüm Açıklaması");
    fireEvent.change(resolutionField, {
      target: { value: "Negatif miktar düzeltildi ve pozitif yapıldı" },
    });

    
    const resendSelect = screen.getByLabelText(
      "Düzeltilmiş veriyi yeniden gönder"
    );
    fireEvent.mouseDown(resendSelect);
    const yesOption = screen.getByText("Evet, yeniden gönder");
    fireEvent.click(yesOption);

    
    const submitButton = screen.getByRole("button", { name: "Çöz" });
    fireEvent.click(submitButton);

    await waitFor(() => {
      
      expect(screen.queryByText("Hatayı Çöz")).not.toBeInTheDocument();
    });
  });

  
  test("ignores a failed record with reason", async () => {
    
    global.prompt = jest.fn(() => "Artık gerekli değil");

    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const viewButtons = screen.getAllByTestId("VisibilityIcon");
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
    });

    
    const ignoreButton = screen.getByRole("button", { name: /Göz Ardı Et/i });
    fireEvent.click(ignoreButton);

    await waitFor(() => {
      expect(global.prompt).toHaveBeenCalledWith("Göz ardı etme nedeni:");
    });

    
    await waitFor(() => {
      expect(
        screen.queryByText("Hatalı Kayıt Detayları")
      ).not.toBeInTheDocument();
    });
  });

  
  test("retries a failed record", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const retryButtons = screen.getAllByTestId("RestartAltIcon");
    fireEvent.click(retryButtons[0]);

    await waitFor(() => {
      
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });
  });

  
  test("handles pagination correctly", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const pagination = screen.getByRole("navigation");
    expect(pagination).toBeInTheDocument();

    
    const pageSizeSelect = screen.getByLabelText("Sayfa başına:");
    expect(pageSizeSelect).toBeInTheDocument();
  });

  
  test("displays correct color chips for different statuses", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    const statusChips = screen.getAllByText(/FAILED|RETRYING/);
    expect(statusChips.length).toBeGreaterThan(0);

    
    const failedChip = screen.getByText("FAILED");
    expect(failedChip).toHaveClass("MuiChip-colorError");

    
    const retryingChip = screen.getByText("RETRYING");
    expect(retryingChip).toHaveClass("MuiChip-colorWarning");
  });

  
  test("refreshes data when clicking refresh button", async () => {
    render(<FailedRecords />);

    await waitFor(() => {
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const refreshButton = screen.getByTestId("RefreshIcon").closest("button");
    fireEvent.click(refreshButton!);

    await waitFor(() => {
      
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });
  });

  
  test("handles API errors gracefully", async () => {
    
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
      
      expect(screen.queryByText("TEST-001")).not.toBeInTheDocument();
    });
  });

  
  test("shows loading indicator while fetching data", () => {
    render(<FailedRecords />);

    
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });

  
  test("handles empty records list", async () => {
    server.use(
      rest.get("/api/adminpanel/failed-records", (req, res, ctx) => {
        return res(ctx.json({ total: 0, page: 1, pageSize: 25, items: [] }));
      })
    );

    render(<FailedRecords />);

    await waitFor(() => {
      
      const rows = screen.queryAllByRole("row");
      
      expect(rows.length).toBe(1);
    });
  });
});


describe("FailedRecords - Complete Workflow Integration", () => {
  test("complete error correction workflow from list to resolution", async () => {
    render(<FailedRecords />);

    
    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıtlar")).toBeInTheDocument();
      expect(screen.getByText("TEST-001")).toBeInTheDocument();
    });

    
    const statusFilter = screen.getByLabelText("Durum");
    fireEvent.mouseDown(statusFilter);
    fireEvent.click(screen.getByText("Başarısız"));

    
    await waitFor(() => {
      const viewButtons = screen.getAllByTestId("VisibilityIcon");
      fireEvent.click(viewButtons[0]);
    });

    
    await waitFor(() => {
      expect(screen.getByText("Hatalı Kayıt Detayları")).toBeInTheDocument();
      expect(screen.getByText(/Validation failed/)).toBeInTheDocument();
    });

    
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

    
    const resolveButton = screen.getByRole("button", {
      name: /Düzelt ve Gönder/i,
    });
    fireEvent.click(resolveButton);

    
    await waitFor(() => {
      expect(screen.getByText("Hatayı Çöz")).toBeInTheDocument();
    });

    const resolutionField = screen.getByLabelText("Çözüm Açıklaması");
    fireEvent.change(resolutionField, {
      target: { value: "Düzeltildi: Negatif miktar pozitif yapıldı" },
    });

    
    const resendSelect = screen.getByLabelText(
      "Düzeltilmiş veriyi yeniden gönder"
    );
    fireEvent.mouseDown(resendSelect);
    fireEvent.click(screen.getByText("Evet, yeniden gönder"));

    
    const submitButton = screen.getByRole("button", { name: "Çöz" });
    fireEvent.click(submitButton);

    
    await waitFor(() => {
      expect(screen.queryByText("Hatayı Çöz")).not.toBeInTheDocument();
      
      expect(screen.getByText("Hatalı Kayıtlar")).toBeInTheDocument();
    });
  });
});
