import { describe, expect, it } from "vitest";
import { handleStatusUpdateError } from "../errorHandling";

describe("errorHandling", () => {
  describe("handleStatusUpdateError", () => {
    describe("Permission errors (403)", () => {
      it("should map 403 error to permission type", () => {
        const error = {
          response: {
            status: 403,
            data: {
              message: "Forbidden",
            },
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("permission");
        expect(result.message).toBe("Bu işlem için yetkiniz yok");
        expect(result.retryable).toBe(false);
      });
    });

    describe("Validation errors (400)", () => {
      it("should map 400 error to validation type", () => {
        const error = {
          response: {
            status: 400,
            data: {
              message: "Geçersiz durum değişikliği",
            },
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("validation");
        expect(result.message).toBe("Geçersiz durum değişikliği");
        expect(result.retryable).toBe(false);
      });

      it("should use default message when API message is missing", () => {
        const error = {
          response: {
            status: 400,
            data: {},
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("validation");
        expect(result.message).toBe("Geçersiz işlem");
        expect(result.retryable).toBe(false);
      });
    });

    describe("Network errors", () => {
      it("should map network error to network type", () => {
        const error = {
          message: "Network Error",
          code: "ECONNABORTED",
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("network");
        expect(result.message).toBe("Bağlantı hatası, lütfen tekrar deneyin");
        expect(result.retryable).toBe(true);
      });

      it("should handle timeout errors", () => {
        const error = {
          message: "timeout of 5000ms exceeded",
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("network");
        expect(result.retryable).toBe(true);
      });
    });

    describe("Unknown errors", () => {
      it("should map unknown error to unknown type", () => {
        const error = {
          response: {
            status: 500,
            data: {
              message: "Internal Server Error",
            },
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("unknown");
        expect(result.message).toBe("Internal Server Error");
        expect(result.retryable).toBe(true);
      });

      it("should use default message for unknown errors without message", () => {
        const error = {
          response: {
            status: 500,
            data: {},
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.type).toBe("unknown");
        expect(result.message).toBe("Beklenmeyen bir hata oluştu");
        expect(result.retryable).toBe(true);
      });
    });

    describe("Retryable flag logic", () => {
      it("should mark permission errors as not retryable", () => {
        const error = {
          response: {
            status: 403,
            data: {},
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.retryable).toBe(false);
      });

      it("should mark validation errors as not retryable", () => {
        const error = {
          response: {
            status: 400,
            data: {},
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.retryable).toBe(false);
      });

      it("should mark network errors as retryable", () => {
        const error = {
          message: "Network Error",
        };

        const result = handleStatusUpdateError(error);

        expect(result.retryable).toBe(true);
      });

      it("should mark unknown errors as retryable", () => {
        const error = {
          response: {
            status: 500,
            data: {},
          },
        };

        const result = handleStatusUpdateError(error);

        expect(result.retryable).toBe(true);
      });
    });
  });
});
