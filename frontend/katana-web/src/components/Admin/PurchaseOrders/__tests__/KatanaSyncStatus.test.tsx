import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import KatanaSyncStatus, { KatanaSyncResult } from "../KatanaSyncStatus";

describe("KatanaSyncStatus", () => {
  describe("Success/fail count calculation", () => {
    it("should display correct success count", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "created",
        },
        {
          sku: "SKU-002",
          productName: "Product 2",
          success: true,
          action: "updated",
        },
        {
          sku: "SKU-003",
          productName: "Product 3",
          success: false,
          action: "created",
          error: "Error",
        },
      ];

      render(<KatanaSyncStatus results={results} />);
      expect(screen.getByText("2 Başarılı")).toBeInTheDocument();
    });

    it("should display correct fail count", () => {
      const results: KatanaSyncResult[] = [
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
          action: "updated",
          error: "Error 1",
        },
        {
          sku: "SKU-003",
          productName: "Product 3",
          success: false,
          action: "created",
          error: "Error 2",
        },
      ];

      render(<KatanaSyncStatus results={results} />);
      expect(screen.getByText("2 Hatalı")).toBeInTheDocument();
    });

    it("should not display fail chip when all succeed", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "created",
        },
        {
          sku: "SKU-002",
          productName: "Product 2",
          success: true,
          action: "updated",
        },
      ];

      render(<KatanaSyncStatus results={results} />);
      expect(screen.getByText("2 Başarılı")).toBeInTheDocument();
      expect(screen.queryByText(/Hatalı/)).not.toBeInTheDocument();
    });
  });

  describe("Table rendering", () => {
    it("should render all sync results in table", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "created",
        },
        {
          sku: "SKU-002",
          productName: "Product 2",
          success: true,
          action: "updated",
        },
      ];

      render(<KatanaSyncStatus results={results} />);

      expect(screen.getByText("SKU-001")).toBeInTheDocument();
      expect(screen.getByText("Product 1")).toBeInTheDocument();
      expect(screen.getByText("SKU-002")).toBeInTheDocument();
      expect(screen.getByText("Product 2")).toBeInTheDocument();
    });

    it("should display 'Oluşturuldu' for created action", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "created",
        },
      ];

      render(<KatanaSyncStatus results={results} />);
      expect(screen.getByText("Oluşturuldu")).toBeInTheDocument();
    });

    it("should display 'Güncellendi' for updated action", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "updated",
        },
      ];

      render(<KatanaSyncStatus results={results} />);
      expect(screen.getByText("Güncellendi")).toBeInTheDocument();
    });

    it("should display new stock quantity when available", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "updated",
          newStock: 150,
        },
      ];

      render(<KatanaSyncStatus results={results} />);
      expect(screen.getByText(/Stok: 150/)).toBeInTheDocument();
    });

    it("should display success icon for successful sync", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: true,
          action: "created",
        },
      ];

      const { container } = render(<KatanaSyncStatus results={results} />);
      const successIcon = container.querySelector(
        '[data-testid="CheckCircleIcon"]'
      );
      expect(successIcon).toBeInTheDocument();
    });

    it("should display error icon for failed sync", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: false,
          action: "created",
          error: "Test error",
        },
      ];

      const { container } = render(<KatanaSyncStatus results={results} />);
      const errorIcon = container.querySelector('[data-testid="ErrorIcon"]');
      expect(errorIcon).toBeInTheDocument();
    });
  });

  describe("Error tooltip display", () => {
    it("should show error message in tooltip", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: false,
          action: "created",
          error: "Connection timeout",
        },
      ];

      const { container } = render(<KatanaSyncStatus results={results} />);
      const errorIcon = container.querySelector('[data-testid="ErrorIcon"]');
      const tooltip = errorIcon?.closest("[title]");

      expect(tooltip).toHaveAttribute("title", "Connection timeout");
    });

    it("should show default error message when error is undefined", () => {
      const results: KatanaSyncResult[] = [
        {
          sku: "SKU-001",
          productName: "Product 1",
          success: false,
          action: "created",
        },
      ];

      const { container } = render(<KatanaSyncStatus results={results} />);
      const errorIcon = container.querySelector('[data-testid="ErrorIcon"]');
      const tooltip = errorIcon?.closest("[title]");

      expect(tooltip).toHaveAttribute("title", "Bilinmeyen hata");
    });
  });

  describe("Empty state handling", () => {
    it("should display empty message when results array is empty", () => {
      render(<KatanaSyncStatus results={[]} />);
      expect(
        screen.getByText("Henüz senkronizasyon yapılmadı.")
      ).toBeInTheDocument();
    });

    it("should not display table when results array is empty", () => {
      const { container } = render(<KatanaSyncStatus results={[]} />);
      const table = container.querySelector("table");
      expect(table).not.toBeInTheDocument();
    });

    it("should not display chips when results array is empty", () => {
      render(<KatanaSyncStatus results={[]} />);
      expect(screen.queryByText(/Başarılı/)).not.toBeInTheDocument();
      expect(screen.queryByText(/Hatalı/)).not.toBeInTheDocument();
    });
  });
});
