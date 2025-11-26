import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import SyncManagement from "./SyncManagement";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("SyncManagement Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();

    
    global.alert = jest.fn();

    
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue([]);
  });

  test("renders sync management page", async () => {
    render(<SyncManagement />);
    expect(screen.getByText(/senkronizasyon yönetimi/i)).toBeInTheDocument();

    
    await waitFor(() => {
      expect(api.stockAPI.getSyncHistory).toHaveBeenCalled();
    });
  });

  test("loads sync history on mount", async () => {
    const mockHistory = [
      {
        id: 1,
        syncType: "Stock",
        status: "Success",
        startTime: "2025-01-01T10:00:00",
        endTime: "2025-01-01T10:05:00",
        processedRecords: 100,
        successfulRecords: 95,
        failedRecords: 5,
        errorMessage: null,
      },
    ];
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<SyncManagement />);

    await waitFor(() => {
      expect(screen.getByText("Stock")).toBeInTheDocument();
    });
  });

  test("opens start sync dialog", async () => {
    render(<SyncManagement />);

    
    await waitFor(() => {
      expect(api.stockAPI.getSyncHistory).toHaveBeenCalled();
    });

    const startButton = screen.getByRole("button", {
      name: /senkronizasyon başlat/i,
    });
    fireEvent.click(startButton);

    
    await waitFor(() => {
      const dialogTitles = screen.getAllByText(/senkronizasyon tipi/i);
      expect(dialogTitles.length).toBeGreaterThan(0);
    });
  });

  test("starts sync with selected type", async () => {
    (api.stockAPI.startSync as jest.Mock).mockResolvedValue({});

    render(<SyncManagement />);

    
    await waitFor(() => {
      expect(api.stockAPI.getSyncHistory).toHaveBeenCalled();
    });

    const startButton = screen.getByRole("button", {
      name: /senkronizasyon başlat/i,
    });
    fireEvent.click(startButton);

    
    await waitFor(() => {
      expect(
        screen.getAllByText(/senkronizasyon tipi/i).length
      ).toBeGreaterThan(0);
    });

    
    const typeSelect = screen.getByRole("combobox");
    fireEvent.mouseDown(typeSelect);

    
    const stockOptions = await screen.findAllByText(/stok senkronizasyonu/i);
    fireEvent.click(stockOptions[stockOptions.length - 1]); 

    
    const buttons = screen.getAllByRole("button");
    const confirmButton = buttons.find((btn) =>
      btn.textContent?.includes("Başlat")
    );
    if (confirmButton) {
      fireEvent.click(confirmButton);
    }

    await waitFor(() => {
      expect(api.stockAPI.startSync).toHaveBeenCalledWith("STOCK");
    });
  });

  test("displays sync status chips", async () => {
    const mockHistory = [
      {
        id: 1,
        syncType: "Stock",
        status: "Success",
        startTime: "2025-01-01",
        endTime: "2025-01-01T10:05:00",
        processedRecords: 10,
        successfulRecords: 10,
        failedRecords: 0,
        errorMessage: null,
      },
      {
        id: 2,
        syncType: "Invoice",
        status: "Failed",
        startTime: "2025-01-02",
        endTime: "2025-01-02T10:05:00",
        processedRecords: 5,
        successfulRecords: 0,
        failedRecords: 5,
        errorMessage: "Test error",
      },
    ];
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<SyncManagement />);

    await waitFor(() => {
      
      const successChips = screen.getAllByText(/başarılı/i);
      const failChips = screen.getAllByText(/başarısız/i);
      expect(successChips.length).toBeGreaterThan(0);
      expect(failChips.length).toBeGreaterThan(0);
    });
  });

  test("refreshes history on button click", async () => {
    let callCount = 0;
    (api.stockAPI.getSyncHistory as jest.Mock).mockImplementation(() => {
      callCount++;
      return Promise.resolve([]);
    });

    render(<SyncManagement />);

    
    await waitFor(() => {
      expect(callCount).toBe(1);
    });

    const refreshButton = screen.getByRole("button", { name: /yenile/i });
    fireEvent.click(refreshButton);

    
    await waitFor(() => {
      expect(callCount).toBe(2);
    });
  });
});
