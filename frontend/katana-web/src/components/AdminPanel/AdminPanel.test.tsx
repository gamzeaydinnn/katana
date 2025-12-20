import { render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import AdminPanel from "./AdminPanel";
import * as api from "../../services/api";

jest.mock("../../services/api");
jest.mock("./LogsViewer", () => () => <div>LogsViewer Mock</div>);
jest.mock("../Settings/Settings", () => () => <div>Settings Mock</div>);
jest.mock("../Admin/PendingAdjustments", () => () => (
  <div>PendingAdjustments Mock</div>
));

describe("AdminPanel Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  const mockApiResponses = () => {
    (api.default.get as jest.Mock).mockImplementation((url: string) => {
      if (url === "/adminpanel/statistics") {
        return Promise.resolve({
          data: {
            totalProducts: 100,
            totalStock: 5000,
            successfulSyncs: 50,
            failedSyncs: 5,
          },
        });
      }
      if (url.includes("/adminpanel/products")) {
        return Promise.resolve({
          data: {
            data: [
              {
                id: "1",
                sku: "SKU001",
                name: "Product 1",
                stock: 100,
                isActive: true,
              },
            ],
          },
        });
      }
      if (url.includes("/adminpanel/sync-logs")) {
        return Promise.resolve({
          data: {
            data: [
              {
                id: 1,
                integrationName: "Katana",
                createdAt: "2025-01-01",
                isSuccess: true,
              },
            ],
          },
        });
      }
      if (url === "/adminpanel/katana-health") {
        return Promise.resolve({ data: { isHealthy: true } });
      }
      return Promise.resolve({ data: {} });
    });
  };

  test("renders admin panel", async () => {
    mockApiResponses();
    render(
      <BrowserRouter>
        <AdminPanel />
      </BrowserRouter>
    );
    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: /admin paneli/i })
      ).toBeInTheDocument();
    });
  });

  test("loads and displays statistics", async () => {
    mockApiResponses();
    render(
      <BrowserRouter>
        <AdminPanel />
      </BrowserRouter>
    );

    const hundreds = await screen.findAllByText("100");
    expect(hundreds[0]).toBeInTheDocument();

    expect(await screen.findByText("5.000")).toBeInTheDocument();
  });

  test("displays products table", async () => {
    mockApiResponses();
    render(
      <BrowserRouter>
        <AdminPanel />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText("Product 1")).toBeInTheDocument();
    });
  });

  test("shows Katana API health status", async () => {
    mockApiResponses();
    render(
      <BrowserRouter>
        <AdminPanel />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/katana api bağlı/i)).toBeInTheDocument();
    });
  });

  test("renders child components", async () => {
    mockApiResponses();
    render(
      <BrowserRouter>
        <AdminPanel />
      </BrowserRouter>
    );

    expect(await screen.findByText("PendingAdjustments Mock")).toBeInTheDocument();
    expect(await screen.findByText("LogsViewer Mock")).toBeInTheDocument();
    expect(await screen.findByText("Settings Mock")).toBeInTheDocument();
  });
});
