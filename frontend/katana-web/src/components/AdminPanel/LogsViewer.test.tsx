import { render, screen, waitFor } from "@testing-library/react";
import LogsViewer from "./LogsViewer";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("LogsViewer Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();

    // Mock API with proper structure including stats
    (api.default.get as jest.Mock).mockImplementation((url: string) => {
      if (url.includes("/stats")) {
        return Promise.resolve({
          data: {
            errorStats: [
              { level: "Error", count: 5 },
              { level: "Warning", count: 10 },
            ],
            auditStats: [
              { actionType: "Create", count: 3 },
              { actionType: "Update", count: 7 },
            ],
            categoryStats: [{ category: "System", count: 8 }],
            period: "Last 24 Hours",
          },
        });
      }
      // Default response for logs
      return Promise.resolve({
        data: { logs: [], total: 0, page: 1, limit: 50 },
      });
    });
  });

  test("renders logs viewer", async () => {
    render(<LogsViewer />);

    // Wait for component to load
    await waitFor(() => {
      expect(api.default.get).toHaveBeenCalled();
    });

    // Component text is in English
    expect(screen.getByText(/system logs/i)).toBeInTheDocument();
  });

  test("shows error and audit tabs", async () => {
    render(<LogsViewer />);

    // Wait for async operations to complete
    await waitFor(() => {
      expect(api.default.get).toHaveBeenCalled();
    });

    // Tab texts appear multiple times, use getAllByText
    const errorLogTexts = screen.getAllByText(/error logs/i);
    const auditLogTexts = screen.getAllByText(/audit logs/i);

    expect(errorLogTexts.length).toBeGreaterThan(0);
    expect(auditLogTexts.length).toBeGreaterThan(0);
  });
});
