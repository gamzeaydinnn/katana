import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import SyncManagement from "./SyncManagement";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("SyncManagement Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("renders sync management page", () => {
    render(<SyncManagement />);
    expect(screen.getByText(/senkronizasyon yönetimi/i)).toBeInTheDocument();
  });

  test("loads sync history on mount", async () => {
    const mockHistory = [
      {
        id: 1,
        syncType: "Stock",
        status: "Success",
        startTime: "2025-01-01T10:00:00",
        processedRecords: 100,
        successfulRecords: 95,
        failedRecords: 5,
      },
    ];
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<SyncManagement />);

    await waitFor(() => {
      expect(screen.getByText("Stock")).toBeInTheDocument();
    });
  });

  test("opens start sync dialog", () => {
    render(<SyncManagement />);
    const startButton = screen.getByRole("button", {
      name: /senkronizasyon başlat/i,
    });
    fireEvent.click(startButton);

    expect(screen.getByText(/senkronizasyon tipi/i)).toBeInTheDocument();
  });

  test("starts sync with selected type", async () => {
    (api.stockAPI.startSync as jest.Mock).mockResolvedValue({});
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue([]);

    render(<SyncManagement />);

    const startButton = screen.getByRole("button", {
      name: /senkronizasyon başlat/i,
    });
    fireEvent.click(startButton);

    const typeSelect = screen.getByLabelText(/senkronizasyon tipi/i);
    fireEvent.mouseDown(typeSelect);

    const stockOption = await screen.findByText(/stok senkronizasyonu/i);
    fireEvent.click(stockOption);

    const confirmButton = screen.getAllByRole("button", { name: /başlat/i })[1];
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(api.stockAPI.startSync).toHaveBeenCalledWith("Stock");
    });
  });

  test("displays sync status chips", async () => {
    const mockHistory = [
      {
        id: 1,
        syncType: "Stock",
        status: "Success",
        startTime: "2025-01-01",
        processedRecords: 10,
        successfulRecords: 10,
        failedRecords: 0,
      },
      {
        id: 2,
        syncType: "Invoice",
        status: "Failed",
        startTime: "2025-01-02",
        processedRecords: 5,
        successfulRecords: 0,
        failedRecords: 5,
      },
    ];
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<SyncManagement />);

    await waitFor(() => {
      expect(screen.getByText(/başarılı/i)).toBeInTheDocument();
      expect(screen.getByText(/başarısız/i)).toBeInTheDocument();
    });
  });

  test("refreshes history on button click", async () => {
    (api.stockAPI.getSyncHistory as jest.Mock).mockResolvedValue([]);

    render(<SyncManagement />);

    const refreshButton = screen.getByRole("button", { name: /yenile/i });
    fireEvent.click(refreshButton);

    await waitFor(() => {
      expect(api.stockAPI.getSyncHistory).toHaveBeenCalledTimes(2);
    });
  });
});
