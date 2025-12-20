import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import StatusBadge, { OrderStatus } from "../StatusBadge";

describe("StatusBadge", () => {
  describe("Color mapping", () => {
    it("should render warning color for Pending status", () => {
      const { container } = render(<StatusBadge status="Pending" />);
      const chip = container.querySelector(".MuiChip-colorWarning");
      expect(chip).toBeInTheDocument();
    });

    it("should render info color for Approved status", () => {
      const { container } = render(<StatusBadge status="Approved" />);
      const chip = container.querySelector(".MuiChip-colorInfo");
      expect(chip).toBeInTheDocument();
    });

    it("should render success color for Received status", () => {
      const { container } = render(<StatusBadge status="Received" />);
      const chip = container.querySelector(".MuiChip-colorSuccess");
      expect(chip).toBeInTheDocument();
    });

    it("should render error color for Cancelled status", () => {
      const { container } = render(<StatusBadge status="Cancelled" />);
      const chip = container.querySelector(".MuiChip-colorError");
      expect(chip).toBeInTheDocument();
    });
  });

  describe("Icon display", () => {
    it("should render HourglassEmpty icon for Pending status", () => {
      const { container } = render(<StatusBadge status="Pending" />);
      const icon = container.querySelector(
        '[data-testid="HourglassEmptyIcon"]'
      );
      expect(icon).toBeInTheDocument();
    });

    it("should render CheckCircle icon for Approved status", () => {
      const { container } = render(<StatusBadge status="Approved" />);
      const icon = container.querySelector('[data-testid="CheckCircleIcon"]');
      expect(icon).toBeInTheDocument();
    });

    it("should render DoneAll icon for Received status", () => {
      const { container } = render(<StatusBadge status="Received" />);
      const icon = container.querySelector('[data-testid="DoneAllIcon"]');
      expect(icon).toBeInTheDocument();
    });

    it("should render Cancel icon for Cancelled status", () => {
      const { container } = render(<StatusBadge status="Cancelled" />);
      const icon = container.querySelector('[data-testid="CancelIcon"]');
      expect(icon).toBeInTheDocument();
    });
  });

  describe("Label display", () => {
    it("should render 'Beklemede' label for Pending status", () => {
      render(<StatusBadge status="Pending" />);
      expect(screen.getByText("Beklemede")).toBeInTheDocument();
    });

    it("should render 'Onaylandı' label for Approved status", () => {
      render(<StatusBadge status="Approved" />);
      expect(screen.getByText("Onaylandı")).toBeInTheDocument();
    });

    it("should render 'Teslim Alındı' label for Received status", () => {
      render(<StatusBadge status="Received" />);
      expect(screen.getByText("Teslim Alındı")).toBeInTheDocument();
    });

    it("should render 'İptal' label for Cancelled status", () => {
      render(<StatusBadge status="Cancelled" />);
      expect(screen.getByText("İptal")).toBeInTheDocument();
    });

    it("should render status value when showLabel is false", () => {
      render(<StatusBadge status="Pending" showLabel={false} />);
      expect(screen.getByText("Pending")).toBeInTheDocument();
      expect(screen.queryByText("Beklemede")).not.toBeInTheDocument();
    });
  });

  describe("Size prop", () => {
    it("should render small size by default", () => {
      const { container } = render(<StatusBadge status="Pending" />);
      const chip = container.querySelector(".MuiChip-sizeSmall");
      expect(chip).toBeInTheDocument();
    });

    it("should render small size when explicitly set", () => {
      const { container } = render(
        <StatusBadge status="Pending" size="small" />
      );
      const chip = container.querySelector(".MuiChip-sizeSmall");
      expect(chip).toBeInTheDocument();
    });

    it("should render medium size when set", () => {
      const { container } = render(
        <StatusBadge status="Pending" size="medium" />
      );
      const chip = container.querySelector(".MuiChip-sizeMedium");
      expect(chip).toBeInTheDocument();
    });
  });

  describe("Edge cases", () => {
    it("should handle unknown status gracefully", () => {
      const consoleWarnSpy = vi
        .spyOn(console, "warn")
        .mockImplementation(() => {});
      render(<StatusBadge status={"Unknown" as OrderStatus} />);
      expect(consoleWarnSpy).toHaveBeenCalledWith("Unknown status: Unknown");
      expect(screen.getByText("Unknown")).toBeInTheDocument();
      consoleWarnSpy.mockRestore();
    });
  });
});
