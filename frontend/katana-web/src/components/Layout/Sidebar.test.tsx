import { render, screen, fireEvent } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import Sidebar from "./Sidebar";

describe("Sidebar Component", () => {
  const mockOnClose = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  test("renders sidebar with menu items", () => {
    render(
      <BrowserRouter>
        <Sidebar open={true} onClose={mockOnClose} />
      </BrowserRouter>
    );

    expect(screen.getByText(/dashboard/i)).toBeInTheDocument();
    expect(screen.getByText(/admin paneli/i)).toBeInTheDocument();
    expect(screen.getByText(/stok yönetimi/i)).toBeInTheDocument();
  });

  test("calls onClose when close button clicked", () => {
    render(
      <BrowserRouter>
        <Sidebar open={true} onClose={mockOnClose} />
      </BrowserRouter>
    );

    const closeButton = screen.getAllByRole("button")[0];
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  test("displays version information", () => {
    render(
      <BrowserRouter>
        <Sidebar open={true} onClose={mockOnClose} />
      </BrowserRouter>
    );

    expect(screen.getByText(/katana integration v1\.0/i)).toBeInTheDocument();
  });
});
