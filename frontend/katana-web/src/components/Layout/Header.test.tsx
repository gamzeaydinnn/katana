import { render, screen, fireEvent } from "@testing-library/react";
import Header from "./Header";
import * as signalr from "../../services/signalr";

jest.mock("../../services/signalr");
jest.mock("../../services/api");

describe("Header Component", () => {
  const mockOnMenuClick = jest.fn();
  const mockOnOpenBranchSelector = jest.fn();
  const mockOnToggleMode = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (signalr.startConnection as jest.Mock).mockResolvedValue(undefined);
    (signalr.onPendingCreated as jest.Mock).mockImplementation(() => {});
    (signalr.onPendingApproved as jest.Mock).mockImplementation(() => {});
    (signalr.offPendingCreated as jest.Mock).mockImplementation(() => {});
    (signalr.offPendingApproved as jest.Mock).mockImplementation(() => {});
  });

  test("renders header with title", () => {
    render(<Header onMenuClick={mockOnMenuClick} sidebarOpen={false} />);

    expect(screen.getByText(/beformet metal erp/i)).toBeInTheDocument();
  });

  test("calls onMenuClick when menu button is clicked", () => {
    render(<Header onMenuClick={mockOnMenuClick} sidebarOpen={false} />);

    const menuButton = screen.getAllByRole("button")[0];
    fireEvent.click(menuButton);

    expect(mockOnMenuClick).toHaveBeenCalled();
  });

  test("displays branch selector when provided", () => {
    render(
      <Header
        onMenuClick={mockOnMenuClick}
        sidebarOpen={false}
        currentBranchName="Test Branch"
        onOpenBranchSelector={mockOnOpenBranchSelector}
      />
    );

    expect(screen.getByText("Test Branch")).toBeInTheDocument();
  });

  test("calls onToggleMode when theme button clicked", () => {
    render(
      <Header
        onMenuClick={mockOnMenuClick}
        sidebarOpen={false}
        mode="light"
        onToggleMode={mockOnToggleMode}
      />
    );

    const themeButtons = screen.getAllByRole("button");
    const themeButton = themeButtons.find((btn) => btn.querySelector("svg"));

    if (themeButton) {
      fireEvent.click(themeButton);
      expect(mockOnToggleMode).toHaveBeenCalled();
    }
  });

  test("opens notification menu", () => {
    render(<Header onMenuClick={mockOnMenuClick} sidebarOpen={false} />);

    const buttons = screen.getAllByRole("button");
    const notificationButton = buttons[buttons.length - 2];
    fireEvent.click(notificationButton);

    expect(screen.getByText(/bildirim yok/i)).toBeInTheDocument();
  });

  test("opens profile menu", () => {
    render(<Header onMenuClick={mockOnMenuClick} sidebarOpen={false} />);

    const avatarButton =
      screen.getAllByRole("button")[screen.getAllByRole("button").length - 1];
    fireEvent.click(avatarButton);

    expect(screen.getByText(/profil/i)).toBeInTheDocument();
    expect(screen.getByText(/ayarlar/i)).toBeInTheDocument();
  });
});
