import { render, screen, waitFor } from "@testing-library/react";
import Dashboard from "./Dashboard";
import * as api from "../../services/api";

jest.mock("../../services/api", () => ({
  stockAPI: {
    getDashboardStats: jest.fn(),
  },
}));

describe("Dashboard Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("renders loading state initially", () => {
    (api.stockAPI.getDashboardStats as jest.Mock).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<Dashboard />);
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });

  test("displays stats after loading", async () => {
    const mockStats = {
      totalProducts: 150,
      totalStock: 500,
      lowStockItems: 12,
      outOfStockItems: 3,
    };

    (api.stockAPI.getDashboardStats as jest.Mock).mockResolvedValue(mockStats);

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
    });

    expect(screen.getByText(/toplam ürün/i)).toBeInTheDocument();
    expect(screen.getByText(/toplam stok/i)).toBeInTheDocument();
  });

  test("shows error message on failure", async () => {
    (api.stockAPI.getDashboardStats as jest.Mock).mockRejectedValue(
      new Error("API hatası")
    );

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText(/api hatası/i)).toBeInTheDocument();
    });
  });

  test("handles empty stats gracefully", async () => {
    (api.stockAPI.getDashboardStats as jest.Mock).mockResolvedValue({});

    render(<Dashboard />);

    await waitFor(() => {
      const zeros = screen.getAllByText("0");
      expect(zeros.length).toBeGreaterThan(0);
    });
  });
});
