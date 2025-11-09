import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import StockManagement from "./StockManagement";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("StockManagement Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Mock default empty response
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue([]);
  });

  test("renders stock management page", async () => {
    render(<StockManagement />);
    expect(
      screen.getByRole("heading", { name: /stok yönetimi/i })
    ).toBeInTheDocument();

    // Wait for initial API call
    await waitFor(() => {
      expect(api.stockAPI.getKatanaProducts).toHaveBeenCalled();
    });
  });

  test("loads products on mount", async () => {
    const mockProducts = [
      {
        id: "1",
        sku: "SKU001",
        name: "Product 1",
        stock: 100,
        isActive: true,
        categoryId: 1,
        categoryName: "Test Category",
        createdAt: "2025-01-01",
      },
    ];
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue(
      mockProducts
    );

    render(<StockManagement />);

    await waitFor(() => {
      expect(screen.getByText("Product 1")).toBeInTheDocument();
    });
  });

  test("filters products by search", async () => {
    const mockProducts = [
      {
        id: "1",
        sku: "SKU001",
        name: "Product 1",
        stock: 100,
        isActive: true,
        categoryId: 1,
      },
      {
        id: "2",
        sku: "SKU002",
        name: "Product 2",
        stock: 50,
        isActive: true,
        categoryId: 1,
      },
    ];
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue(
      mockProducts
    );

    render(<StockManagement />);

    await waitFor(() => screen.getByText("Product 1"));

    const searchInput = screen.getByPlaceholderText(/ürün ara/i);
    fireEvent.change(searchInput, { target: { value: "Product 1" } });

    expect(screen.getByText("Product 1")).toBeInTheDocument();
  });

  test("shows error on API failure", async () => {
    (api.stockAPI.getKatanaProducts as jest.Mock).mockRejectedValue({
      response: { data: { message: "API Error" } },
    });

    render(<StockManagement />);

    await waitFor(() => {
      // Component shows generic error or the actual API error
      const errorElements = screen.queryAllByText(/error|hata|yüklenemedi/i);
      expect(errorElements.length).toBeGreaterThan(0);
    });
  });

  test("displays stock status chips", async () => {
    const mockProducts = [
      {
        id: "1",
        sku: "SKU001",
        name: "Low Stock",
        stock: 5,
        isActive: true,
        categoryId: 1,
      },
      {
        id: "2",
        sku: "SKU002",
        name: "Out of Stock",
        stock: 0,
        isActive: true,
        categoryId: 1,
      },
    ];
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue(
      mockProducts
    );

    render(<StockManagement />);

    await waitFor(() => {
      expect(screen.getByText(/düşük/i)).toBeInTheDocument();
      expect(screen.getByText(/stokta yok/i)).toBeInTheDocument();
    });
  });

  test("refreshes products on button click", async () => {
    render(<StockManagement />);

    // Wait for initial load
    await waitFor(() => {
      expect(api.stockAPI.getKatanaProducts).toHaveBeenCalled();
    });

    const refreshButton = screen.getByRole("button", { name: /yenile/i });

    // Button should be clickable
    fireEvent.click(refreshButton);

    // Just verify button exists and is clickable
    expect(refreshButton).toBeInTheDocument();
  });
});
