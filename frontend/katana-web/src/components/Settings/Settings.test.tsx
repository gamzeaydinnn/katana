import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import Settings from "./Settings";

describe("Settings Component", () => {
  test("renders settings page", () => {
    render(<Settings />);
    expect(
      screen.getAllByRole("heading", { name: /ayarlar/i })[0]
    ).toBeInTheDocument();
  });

  test("displays API settings section", () => {
    render(<Settings />);
    expect(screen.getByText(/api ayarları/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/katana api key/i)).toBeInTheDocument();
  });

  test("toggles API key visibility", () => {
    render(<Settings />);
    const apiKeyInput = screen.getByLabelText(
      /katana api key/i
    ) as HTMLInputElement;
    const toggleButtons = screen.getAllByRole("button");
    const visibilityButton = toggleButtons.find(
      (btn) => btn.getAttribute("edge") === "end"
    );

    expect(apiKeyInput.type).toBe("password");

    if (visibilityButton) {
      fireEvent.click(visibilityButton);
      expect(apiKeyInput.type).toBe("text");
    }
  });

  test("shows success message on save", async () => {
    render(<Settings />);
    const saveButton = screen.getByRole("button", { name: /kaydet/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(
        screen.getByText(/ayarlar başarıyla kaydedildi/i)
      ).toBeInTheDocument();
    });
  });

  test("handles auto sync toggle", () => {
    render(<Settings />);
    const autoSyncSwitch = screen.getByRole("switch", {
      name: /otomatik senkronizasyon/i,
    });
    fireEvent.click(autoSyncSwitch);
    expect(autoSyncSwitch).not.toBeChecked();
  });

  test("displays system information", () => {
    render(<Settings />);
    expect(screen.getByText(/sistem bilgisi/i)).toBeInTheDocument();
    expect(screen.getByText(/v1\.0\.0/i)).toBeInTheDocument();
  });
});
