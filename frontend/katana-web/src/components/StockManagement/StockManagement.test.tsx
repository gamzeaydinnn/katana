import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import StockManagement from "./StockManagement";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("StockManagement Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("renders stock management page", () => {
    render(<StockManagement />);
    expect(
      screen.getByRole("heading", { name: /stok yönetimi/i })
    ).toBeInTheDocument();
  });

  test("loads products on mount", async () => {
    const mockProducts = [
      {
        id: "1",
        sku: "SKU001",
        name: "Product 1",
        stock: 100,
        isActive: true,
        createdAt: "2025-01-01",
      },
    ];
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue({
      products: mockProducts,
    });

    render(<StockManagement />);

    await waitFor(() => {
      expect(screen.getByText("Product 1")).toBeInTheDocument();
    });
  });

  test("filters products by search", async () => {
    const mockProducts = [
      { id: "1", sku: "SKU001", name: "Product 1", stock: 100, isActive: true },
      { id: "2", sku: "SKU002", name: "Product 2", stock: 50, isActive: true },
    ];
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue({
      products: mockProducts,
    });

    render(<StockManagement />);

    await waitFor(() => screen.getByText("Product 1"));

    const searchInput = screen.getByPlaceholderText(/ürün ara/i);
    fireEvent.change(searchInput, { target: { value: "Product 1" } });

    expect(screen.getByText("Product 1")).toBeInTheDocument();
  });

  test("shows error on API failure", async () => {
    (api.stockAPI.getKatanaProducts as jest.Mock).mockRejectedValue({
      message: "API Error",
    });

    render(<StockManagement />);

    await waitFor(() => {
      expect(screen.getByText(/ürünler yüklenemedi/i)).toBeInTheDocument();
    });
  });

  test("displays stock status chips", async () => {
    const mockProducts = [
      { id: "1", sku: "SKU001", name: "Low Stock", stock: 5, isActive: true },
      {
        id: "2",
        sku: "SKU002",
        name: "Out of Stock",
        stock: 0,
        isActive: true,
      },
    ];
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue({
      products: mockProducts,
    });

    render(<StockManagement />);

    await waitFor(() => {
      expect(screen.getByText(/düşük/i)).toBeInTheDocument();
      expect(screen.getByText(/stokta yok/i)).toBeInTheDocument();
    });
  });

  test("refreshes products on button click", async () => {
    (api.stockAPI.getKatanaProducts as jest.Mock).mockResolvedValue({
      products: [],
    });

    render(<StockManagement />);

    const refreshButton = screen.getByRole("button", { name: /yenile/i });
    fireEvent.click(refreshButton);

    await waitFor(() => {
      expect(api.stockAPI.getKatanaProducts).toHaveBeenCalledTimes(2);
    });
  });
});
