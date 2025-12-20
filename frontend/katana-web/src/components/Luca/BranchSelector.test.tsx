import { render, screen, fireEvent } from "@testing-library/react";
import BranchSelector from "./BranchSelector";

describe("BranchSelector Component", () => {
  const mockBranches = [
    { id: 1, sirketSubeAdi: "Branch 1", orgSirketSubeId: 100 },
    { id: 2, sirketSubeAdi: "Branch 2", orgSirketSubeId: 200 },
  ];

  const mockOnClose = jest.fn();
  const mockOnSelect = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("renders branch selector dialog", () => {
    render(
      <BranchSelector
        open={true}
        onClose={mockOnClose}
        branches={mockBranches}
        onSelect={mockOnSelect}
      />
    );

    expect(screen.getByText(/şirket \/ şube seçimi/i)).toBeInTheDocument();
  });

  test("displays branch list", () => {
    render(
      <BranchSelector
        open={true}
        onClose={mockOnClose}
        branches={mockBranches}
        onSelect={mockOnSelect}
      />
    );

    expect(screen.getByText("Branch 1")).toBeInTheDocument();
    expect(screen.getByText("Branch 2")).toBeInTheDocument();
  });

  test("calls onSelect when branch is clicked", () => {
    render(
      <BranchSelector
        open={true}
        onClose={mockOnClose}
        branches={mockBranches}
        onSelect={mockOnSelect}
      />
    );

    const branch1 = screen.getByText("Branch 1");
    fireEvent.click(branch1);

    expect(mockOnSelect).toHaveBeenCalledWith(mockBranches[0]);
  });

  test("shows no branches message when list is empty", () => {
    render(
      <BranchSelector
        open={true}
        onClose={mockOnClose}
        branches={[]}
        onSelect={mockOnSelect}
      />
    );

    expect(
      screen.getByText(/yetkili şirket\/şube bulunamadı/i)
    ).toBeInTheDocument();
  });

  test("calls onClose when close button is clicked", () => {
    render(
      <BranchSelector
        open={true}
        onClose={mockOnClose}
        branches={mockBranches}
        onSelect={mockOnSelect}
      />
    );

    const closeButton = screen.getByRole("button", { name: /kapat/i });
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalled();
  });
});
