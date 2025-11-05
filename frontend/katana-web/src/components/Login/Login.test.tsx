import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import Login from "./Login";
import * as api from "../../services/api";

const mockedNavigate = jest.fn();

// Simple mock for react-router-dom to avoid requiring the real package in tests
jest.mock("react-router-dom", () => ({
  BrowserRouter: ({ children }: any) => <div>{children}</div>,
  Routes: ({ children }: any) => <div>{children}</div>,
  Route: ({ element }: any) => element || null,
  useNavigate: () => mockedNavigate,
}));

// Mock API
jest.mock("../../services/api", () => ({
  authAPI: {
    login: jest.fn(),
  },
}));

describe("Login Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  const renderLogin = () => {
    return render(
      <BrowserRouter>
        <Login />
      </BrowserRouter>
    );
  };

  test("renders login form", () => {
    renderLogin();
    expect(screen.getByLabelText(/kullanıcı adı/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/şifre/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /giriş yap/i })
    ).toBeInTheDocument();
  });

  test("shows error on invalid credentials", async () => {
    (api.authAPI.login as jest.Mock).mockRejectedValue({
      response: { data: { message: "Geçersiz kullanıcı adı veya şifre" } },
    });

    renderLogin();

    const usernameInput = screen.getByLabelText(/kullanıcı adı/i);
    const passwordInput = screen.getByLabelText(/şifre/i);
    const submitButton = screen.getByRole("button", { name: /giriş yap/i });

    fireEvent.change(usernameInput, { target: { value: "wrong" } });
    fireEvent.change(passwordInput, { target: { value: "wrong" } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(
        screen.getByText(/geçersiz kullanıcı adı veya şifre/i)
      ).toBeInTheDocument();
    });
  });

  test("navigates to admin on successful login", async () => {
    (api.authAPI.login as jest.Mock).mockResolvedValue({
      token: "fake-jwt-token",
    });

    renderLogin();

    const usernameInput = screen.getByLabelText(/kullanıcı adı/i);
    const passwordInput = screen.getByLabelText(/şifre/i);
    const submitButton = screen.getByRole("button", { name: /giriş yap/i });

    fireEvent.change(usernameInput, { target: { value: "admin" } });
    fireEvent.change(passwordInput, { target: { value: "Admin123!" } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(localStorage.getItem("authToken")).toBe("fake-jwt-token");
      expect(mockedNavigate).toHaveBeenCalledWith("/admin");
    });
  });

  test("shows error when token is missing in response", async () => {
    (api.authAPI.login as jest.Mock).mockResolvedValue({});

    renderLogin();

    const usernameInput = screen.getByLabelText(/kullanıcı adı/i);
    const passwordInput = screen.getByLabelText(/şifre/i);
    const submitButton = screen.getByRole("button", { name: /giriş yap/i });

    fireEvent.change(usernameInput, { target: { value: "admin" } });
    fireEvent.change(passwordInput, { target: { value: "Admin123!" } });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/token alınamadı/i)).toBeInTheDocument();
    });
  });

  test("disables button while loading", async () => {
    (api.authAPI.login as jest.Mock).mockImplementation(
      () =>
        new Promise((resolve) =>
          setTimeout(() => resolve({ token: "test" }), 100)
        )
    );

    renderLogin();

    const submitButton = screen.getByRole("button", { name: /giriş yap/i });

    fireEvent.change(screen.getByLabelText(/kullanıcı adı/i), {
      target: { value: "admin" },
    });
    fireEvent.change(screen.getByLabelText(/şifre/i), {
      target: { value: "password" },
    });
    fireEvent.click(submitButton);

    expect(submitButton).toBeDisabled();
  });

  test("toggles password visibility", () => {
    renderLogin();

    const passwordInput = screen.getByLabelText(/şifre/i) as HTMLInputElement;
    const toggleButtons = screen.getAllByRole("button");
    const toggleButton = toggleButtons.find((btn) => btn.querySelector("svg"));

    expect(passwordInput.type).toBe("password");

    if (toggleButton) {
      fireEvent.click(toggleButton);
      expect(passwordInput.type).toBe("text");

      fireEvent.click(toggleButton);
      expect(passwordInput.type).toBe("password");
    }
  });
});
