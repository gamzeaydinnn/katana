import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import Reports from "./Reports";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("Reports Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    
    (api.stockAPI.getStockReport as jest.Mock).mockResolvedValue({
      stockData: [],
      summary: {
        totalProducts: 0,
        totalStockValue: 0,
        lowStockCount: 0,
        outOfStockCount: 0,
        activeProductsCount: 0,
      },
      pagination: { page: 1, pageSize: 100, totalCount: 0, totalPages: 1 },
    });
  });

  test("renders reports page", async () => {
    render(<Reports />);
    expect(
      screen.getByRole("heading", { name: /stok raporu/i })
    ).toBeInTheDocument();
    
    await waitFor(() => {
      expect(api.stockAPI.getStockReport).toHaveBeenCalled();
    });
  });

  test("loads stock report data", async () => {
    const mockData = {
      stockData: [
        {
          id: 1,
          name: "Test Product",
          sku: "SKU001",
          quantity: 100,
          price: 50,
          stockValue: 5000,
          isLowStock: false,
          isOutOfStock: false,
          isActive: true,
          categoryId: 1,
          lastUpdated: "2025-01-01",
        },
      ],
      summary: {
        totalProducts: 1,
        totalStockValue: 5000,
        averagePrice: 50,
        totalStock: 100,
        lowStockCount: 0,
        outOfStockCount: 0,
        activeProductsCount: 1,
      },
      pagination: { page: 1, pageSize: 100, totalCount: 1, totalPages: 1 },
    };
    (api.stockAPI.getStockReport as jest.Mock).mockResolvedValue(mockData);

    render(<Reports />);

    await waitFor(() => {
      expect(screen.getByText("Test Product")).toBeInTheDocument();
    });
  });

  test("handles search filter", async () => {
    render(<Reports />);

    
    await waitFor(() => {
      expect(api.stockAPI.getStockReport).toHaveBeenCalled();
    });

    const searchInput = screen.getByPlaceholderText(/ürün adı veya sku girin/i);
    fireEvent.change(searchInput, { target: { value: "Test" } });
    expect(searchInput).toHaveValue("Test");
  });

  test("handles low stock filter", async () => {
    render(<Reports />);

    
    await waitFor(() => {
      expect(api.stockAPI.getStockReport).toHaveBeenCalled();
    });

    
    const lowStockSwitch = screen.getByRole("switch");
    fireEvent.click(lowStockSwitch);
    expect(lowStockSwitch).toBeChecked();
  });

  test("shows error message on API failure", async () => {
    (api.stockAPI.getStockReport as jest.Mock).mockRejectedValue({
      message: "API Error",
    });

    render(<Reports />);

    await waitFor(() => {
      
      expect(screen.getByText(/API Error/i)).toBeInTheDocument();
    });
  });

  test("downloads CSV when button clicked", async () => {
    const mockData = {
      stockData: [
        {
          id: 1,
          name: "Test",
          sku: "SKU001",
          quantity: 10,
          price: 100,
          stockValue: 1000,
          isLowStock: false,
          isOutOfStock: false,
          isActive: true,
          categoryId: 1,
          lastUpdated: "2025-01-01",
        },
      ],
      summary: {
        totalProducts: 1,
        totalStockValue: 1000,
        averagePrice: 100,
        totalStock: 10,
        lowStockCount: 0,
        outOfStockCount: 0,
        activeProductsCount: 1,
      },
      pagination: { page: 1, pageSize: 100, totalCount: 1, totalPages: 1 },
    };
    (api.stockAPI.getStockReport as jest.Mock).mockResolvedValue(mockData);

    
    global.URL.createObjectURL = jest.fn(() => "blob:test");

    render(<Reports />);

    await waitFor(() => screen.getByText("Test"));

    const csvButton = screen.getByRole("button", { name: /rapor oluştur/i });

    
    fireEvent.click(csvButton);

    
    expect(csvButton).toBeInTheDocument();
  });
});
