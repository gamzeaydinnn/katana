import { render, screen, waitFor } from "@testing-library/react";
import LogsViewer from "./LogsViewer";
import * as api from "../../services/api";

jest.mock("../../services/api");

describe("LogsViewer Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();

    // Mock API to return empty data immediately
    (api.default.get as jest.Mock).mockResolvedValue({ 
      data: { logs: [], total: 0, page: 1, limit: 50 } 
    });
  });

  test("renders logs viewer", async () => {
    render(<LogsViewer />);
    expect(screen.getByText("System Logs")).toBeInTheDocument();
    
    // Wait for async operations to complete
    await waitFor(() => {
      expect(api.default.get).toHaveBeenCalled();
    });
  });

  test("shows error and audit tabs", async () => {
    render(<LogsViewer />);
    expect(screen.getByText(/error logs/i)).toBeInTheDocument();
    expect(screen.getByText(/audit logs/i)).toBeInTheDocument();
    
    // Wait for async operations to complete
    await waitFor(() => {
      expect(api.default.get).toHaveBeenCalled();
    });
  });
});
