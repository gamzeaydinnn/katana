import * as api from "./api";

jest.mock("axios", () => ({
  create: jest.fn(() => ({
    post: jest.fn(),
    get: jest.fn(),
    interceptors: {
      request: { use: jest.fn() },
      response: { use: jest.fn() },
    },
  })),
}));

describe("API Service", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  test("authAPI exists", () => {
    expect(api.authAPI).toBeDefined();
    expect(api.authAPI.login).toBeDefined();
  });

  test("stockAPI exists", () => {
    expect(api.stockAPI).toBeDefined();
    expect(api.stockAPI.getDashboardStats).toBeDefined();
  });
});
