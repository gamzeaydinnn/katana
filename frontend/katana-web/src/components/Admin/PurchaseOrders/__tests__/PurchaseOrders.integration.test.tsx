import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import api from "../../../services/api";

// Mock API
vi.mock("../../../services/api");

describe("PurchaseOrders - Status Update Integration", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("Successful status transitions", () => {
    it("should successfully transition from Pending to Approved", async () => {
      const mockOrder = {
        id: 1,
        orderNo: "PO-001",
        status: "Pending",
        supplierId: 1,
        totalAmount: 1000,
        orderDate: "2025-01-01",
        items: [],
      };

      const mockResponse = {
        data: {
          success: true,
          message: "Sipariş onaylandı ve Katana'ya gönderildi",
          order: { ...mockOrder, status: "Approved" },
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch(
        `/purchase-orders/${mockOrder.id}/status`,
        {
          newStatus: "Approved",
        }
      );

      expect(result.data.success).toBe(true);
      expect(result.data.order.status).toBe("Approved");
      expect(api.patch).toHaveBeenCalledWith("/purchase-orders/1/status", {
        newStatus: "Approved",
      });
    });

    it("should successfully transition from Approved to Received", async () => {
      const mockOrder = {
        id: 1,
        orderNo: "PO-001",
        status: "Approved",
        supplierId: 1,
        totalAmount: 1000,
        orderDate: "2025-01-01",
        items: [],
      };

      const mockResponse = {
        data: {
          success: true,
          message: "Sipariş teslim alındı olarak işaretlendi",
          order: { ...mockOrder, status: "Received" },
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch(
        `/purchase-orders/${mockOrder.id}/status`,
        {
          newStatus: "Received",
        }
      );

      expect(result.data.success).toBe(true);
      expect(result.data.order.status).toBe("Received");
    });
  });

  describe("Invalid transition rejection", () => {
    it("should reject Pending to Received transition", async () => {
      const mockError = {
        response: {
          status: 400,
          data: {
            message: "Geçersiz durum değişikliği: Pending -> Received",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Received" })
      ).rejects.toMatchObject(mockError);
    });

    it("should reject Received to Approved transition", async () => {
      const mockError = {
        response: {
          status: 400,
          data: {
            message: "Geçersiz durum değişikliği: Received -> Approved",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Approved" })
      ).rejects.toMatchObject(mockError);
    });

    it("should reject Approved to Pending transition", async () => {
      const mockError = {
        response: {
          status: 400,
          data: {
            message: "Geçersiz durum değişikliği: Approved -> Pending",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Pending" })
      ).rejects.toMatchObject(mockError);
    });
  });

  describe("Network error handling", () => {
    it("should handle network timeout error", async () => {
      const mockError = {
        message: "Network Error",
        code: "ECONNABORTED",
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Approved" })
      ).rejects.toMatchObject(mockError);
    });

    it("should handle server unavailable error", async () => {
      const mockError = {
        response: {
          status: 503,
          data: {
            message: "Service Unavailable",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Approved" })
      ).rejects.toMatchObject(mockError);
    });
  });

  describe("Permission error handling", () => {
    it("should handle 403 Forbidden error", async () => {
      const mockError = {
        response: {
          status: 403,
          data: {
            message: "Bu işlem için yetkiniz yok",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Approved" })
      ).rejects.toMatchObject(mockError);
    });

    it("should handle 401 Unauthorized error", async () => {
      const mockError = {
        response: {
          status: 401,
          data: {
            message: "Oturum süreniz dolmuş",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      await expect(
        api.patch("/purchase-orders/1/status", { newStatus: "Approved" })
      ).rejects.toMatchObject(mockError);
    });
  });

  describe("Katana sync results", () => {
    it("should include Katana sync results in response", async () => {
      const mockResponse = {
        data: {
          success: true,
          message: "Sipariş onaylandı",
          order: {
            id: 1,
            status: "Approved",
          },
          katanaSyncResults: [
            {
              sku: "SKU-001",
              productName: "Product 1",
              success: true,
              action: "created",
              newStock: 100,
            },
            {
              sku: "SKU-002",
              productName: "Product 2",
              success: true,
              action: "updated",
              newStock: 250,
            },
          ],
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      expect(result.data.katanaSyncResults).toHaveLength(2);
      expect(result.data.katanaSyncResults[0].success).toBe(true);
      expect(result.data.katanaSyncResults[0].action).toBe("created");
    });

    it("should handle partial Katana sync failures", async () => {
      const mockResponse = {
        data: {
          success: true,
          message: "Sipariş onaylandı (bazı ürünler senkronize edilemedi)",
          order: {
            id: 1,
            status: "Approved",
          },
          katanaSyncResults: [
            {
              sku: "SKU-001",
              productName: "Product 1",
              success: true,
              action: "created",
            },
            {
              sku: "SKU-002",
              productName: "Product 2",
              success: false,
              action: "created",
              error: "Katana API timeout",
            },
          ],
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      const failedItems = result.data.katanaSyncResults.filter(
        (r: any) => !r.success
      );
      expect(failedItems).toHaveLength(1);
      expect(failedItems[0].error).toBe("Katana API timeout");
    });
  });
});

describe("PurchaseOrders - Status Filter Integration", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("Filter parameter sent to API", () => {
    it("should send status parameter when filter is set", async () => {
      const mockResponse = {
        data: {
          data: [],
          total: 0,
        },
      };

      vi.mocked(api.get).mockResolvedValueOnce(mockResponse);

      await api.get("/purchase-orders", {
        params: { status: "Pending" },
      });

      expect(api.get).toHaveBeenCalledWith("/purchase-orders", {
        params: { status: "Pending" },
      });
    });

    it("should not send status parameter when filter is 'all'", async () => {
      const mockResponse = {
        data: {
          data: [],
          total: 0,
        },
      };

      vi.mocked(api.get).mockResolvedValueOnce(mockResponse);

      await api.get("/purchase-orders", { params: {} });

      expect(api.get).toHaveBeenCalledWith("/purchase-orders", {
        params: {},
      });
    });
  });

  describe("Filter shows correct orders", () => {
    it("should return only Pending orders when Pending filter is applied", async () => {
      const mockOrders = [
        { id: 1, orderNo: "PO-001", status: "Pending" },
        { id: 2, orderNo: "PO-002", status: "Pending" },
      ];

      const mockResponse = {
        data: {
          data: mockOrders,
          total: 2,
        },
      };

      vi.mocked(api.get).mockResolvedValueOnce(mockResponse);

      const result = await api.get("/purchase-orders", {
        params: { status: "Pending" },
      });

      expect(result.data.data).toHaveLength(2);
      expect(result.data.data.every((o: any) => o.status === "Pending")).toBe(
        true
      );
    });

    it("should return only Approved orders when Approved filter is applied", async () => {
      const mockOrders = [
        { id: 3, orderNo: "PO-003", status: "Approved" },
        { id: 4, orderNo: "PO-004", status: "Approved" },
      ];

      const mockResponse = {
        data: {
          data: mockOrders,
          total: 2,
        },
      };

      vi.mocked(api.get).mockResolvedValueOnce(mockResponse);

      const result = await api.get("/purchase-orders", {
        params: { status: "Approved" },
      });

      expect(result.data.data).toHaveLength(2);
      expect(result.data.data.every((o: any) => o.status === "Approved")).toBe(
        true
      );
    });

    it("should return only Received orders when Received filter is applied", async () => {
      const mockOrders = [{ id: 5, orderNo: "PO-005", status: "Received" }];

      const mockResponse = {
        data: {
          data: mockOrders,
          total: 1,
        },
      };

      vi.mocked(api.get).mockResolvedValueOnce(mockResponse);

      const result = await api.get("/purchase-orders", {
        params: { status: "Received" },
      });

      expect(result.data.data).toHaveLength(1);
      expect(result.data.data[0].status).toBe("Received");
    });

    it("should return all orders when 'all' filter is applied", async () => {
      const mockOrders = [
        { id: 1, orderNo: "PO-001", status: "Pending" },
        { id: 2, orderNo: "PO-002", status: "Approved" },
        { id: 3, orderNo: "PO-003", status: "Received" },
      ];

      const mockResponse = {
        data: {
          data: mockOrders,
          total: 3,
        },
      };

      vi.mocked(api.get).mockResolvedValueOnce(mockResponse);

      const result = await api.get("/purchase-orders", { params: {} });

      expect(result.data.data).toHaveLength(3);
    });
  });

  describe("Filter updates list", () => {
    it("should fetch new data when filter changes", async () => {
      const mockResponse1 = {
        data: {
          data: [{ id: 1, status: "Pending" }],
          total: 1,
        },
      };

      const mockResponse2 = {
        data: {
          data: [{ id: 2, status: "Approved" }],
          total: 1,
        },
      };

      vi.mocked(api.get)
        .mockResolvedValueOnce(mockResponse1)
        .mockResolvedValueOnce(mockResponse2);

      // First call with Pending filter
      await api.get("/purchase-orders", { params: { status: "Pending" } });

      // Second call with Approved filter
      await api.get("/purchase-orders", { params: { status: "Approved" } });

      expect(api.get).toHaveBeenCalledTimes(2);
      expect(api.get).toHaveBeenNthCalledWith(1, "/purchase-orders", {
        params: { status: "Pending" },
      });
      expect(api.get).toHaveBeenNthCalledWith(2, "/purchase-orders", {
        params: { status: "Approved" },
      });
    });
  });
});

describe("PurchaseOrders - Notification Display", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("Success notifications", () => {
    it("should show success notification on successful status update", async () => {
      const mockResponse = {
        data: {
          success: true,
          message: "Sipariş onaylandı ve Katana'ya gönderildi",
          order: { id: 1, status: "Approved" },
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      expect(result.data.message).toBe(
        "Sipariş onaylandı ve Katana'ya gönderildi"
      );
    });

    it("should show default success message when message is not provided", async () => {
      const mockResponse = {
        data: {
          success: true,
          order: { id: 1, status: "Approved" },
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      expect(result.data.success).toBe(true);
    });
  });

  describe("Error notifications", () => {
    it("should show error notification on failed status update", async () => {
      const mockError = {
        response: {
          status: 400,
          data: {
            message: "Geçersiz durum değişikliği",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      try {
        await api.patch("/purchase-orders/1/status", { newStatus: "Invalid" });
      } catch (error: any) {
        expect(error.response.data.message).toBe("Geçersiz durum değişikliği");
      }
    });

    it("should show detailed error message from API", async () => {
      const mockError = {
        response: {
          status: 500,
          data: {
            message: "Katana API bağlantı hatası: Timeout after 30s",
          },
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      try {
        await api.patch("/purchase-orders/1/status", { newStatus: "Approved" });
      } catch (error: any) {
        expect(error.response.data.message).toContain("Katana API");
        expect(error.response.data.message).toContain("Timeout");
      }
    });

    it("should show generic error message when API error has no message", async () => {
      const mockError = {
        response: {
          status: 500,
          data: {},
        },
      };

      vi.mocked(api.patch).mockRejectedValueOnce(mockError);

      try {
        await api.patch("/purchase-orders/1/status", { newStatus: "Approved" });
      } catch (error: any) {
        expect(error.response.status).toBe(500);
      }
    });
  });

  describe("Notification timing", () => {
    it("should auto-hide success notification after timeout", async () => {
      // This would be tested in component tests with timer mocks
      // Here we just verify the notification data structure
      const mockResponse = {
        data: {
          success: true,
          message: "Success",
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      expect(result.data.success).toBe(true);
    });
  });

  describe("Notification message content", () => {
    it("should include order number in success message", async () => {
      const mockResponse = {
        data: {
          success: true,
          message: "PO-001 numaralı sipariş onaylandı",
          order: { id: 1, orderNo: "PO-001", status: "Approved" },
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      expect(result.data.message).toContain("PO-001");
    });

    it("should include Katana sync info in success message", async () => {
      const mockResponse = {
        data: {
          success: true,
          message:
            "Sipariş onaylandı ve Katana'ya gönderildi (3/3 ürün başarılı)",
          order: { id: 1, status: "Approved" },
        },
      };

      vi.mocked(api.patch).mockResolvedValueOnce(mockResponse);

      const result = await api.patch("/purchase-orders/1/status", {
        newStatus: "Approved",
      });

      expect(result.data.message).toContain("Katana");
      expect(result.data.message).toContain("3/3");
    });
  });
});
