import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import StatusFilter, { OrderStats } from "../StatusFilter";

describe("StatusFilter", () => {
  const mockStats: OrderStats = {
    total: 100,
    synced: 50,
    notSynced: 30,
    withErrors: 20,
    pending: 25,
    approved: 40,
    received: 30,
    cancelled: 5,
  };

  const mockOnChange = vi.fn();

  beforeEach(() => {
    mockOnChange.mockClear();
  });

  describe("Filter options rendering", () => {
    it("should render all filter options", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("Tümü (100)")).toBeInTheDocument();
      expect(screen.getByText("Beklemede (25)")).toBeInTheDocument();
      expect(screen.getByText("Onaylandı (40)")).toBeInTheDocument();
      expect(screen.getByText("Teslim Alındı (30)")).toBeInTheDocument();
      expect(screen.getByText("İptal (5)")).toBeInTheDocument();
    });

    it("should display correct label", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );
      expect(screen.getByLabelText("Sipariş Durumu")).toBeInTheDocument();
    });
  });

  describe("Count display", () => {
    it("should display total count for 'Tümü' option", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("Tümü (100)")).toBeInTheDocument();
    });

    it("should display pending count", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("Beklemede (25)")).toBeInTheDocument();
    });

    it("should display approved count", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("Onaylandı (40)")).toBeInTheDocument();
    });

    it("should display received count", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("Teslim Alındı (30)")).toBeInTheDocument();
    });

    it("should display cancelled count", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("İptal (5)")).toBeInTheDocument();
    });

    it("should handle zero counts", () => {
      const zeroStats: OrderStats = {
        total: 0,
        synced: 0,
        notSynced: 0,
        withErrors: 0,
        pending: 0,
        approved: 0,
        received: 0,
        cancelled: 0,
      };

      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={zeroStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      expect(screen.getByText("Tümü (0)")).toBeInTheDocument();
      expect(screen.getByText("Beklemede (0)")).toBeInTheDocument();
    });
  });

  describe("onChange callback", () => {
    it("should call onChange when filter is changed", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      const pendingOption = screen.getByText("Beklemede (25)");
      fireEvent.click(pendingOption);

      expect(mockOnChange).toHaveBeenCalledWith("Pending");
    });

    it("should call onChange with correct value for Approved", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      const approvedOption = screen.getByText("Onaylandı (40)");
      fireEvent.click(approvedOption);

      expect(mockOnChange).toHaveBeenCalledWith("Approved");
    });

    it("should call onChange with correct value for Received", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      const receivedOption = screen.getByText("Teslim Alındı (30)");
      fireEvent.click(receivedOption);

      expect(mockOnChange).toHaveBeenCalledWith("Received");
    });

    it("should call onChange with 'all' when Tümü is selected", () => {
      render(
        <StatusFilter
          value="Pending"
          onChange={mockOnChange}
          stats={mockStats}
        />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      fireEvent.mouseDown(select);

      const allOption = screen.getByText("Tümü (100)");
      fireEvent.click(allOption);

      expect(mockOnChange).toHaveBeenCalledWith("all");
    });
  });

  describe("Default value handling", () => {
    it("should display selected value", () => {
      render(
        <StatusFilter
          value="Pending"
          onChange={mockOnChange}
          stats={mockStats}
        />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      expect(select).toHaveValue("Pending");
    });

    it("should display 'all' as default", () => {
      render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      const select = screen.getByLabelText("Sipariş Durumu");
      expect(select).toHaveValue("all");
    });

    it("should update displayed value when prop changes", () => {
      const { rerender } = render(
        <StatusFilter value="all" onChange={mockOnChange} stats={mockStats} />
      );

      let select = screen.getByLabelText("Sipariş Durumu");
      expect(select).toHaveValue("all");

      rerender(
        <StatusFilter
          value="Approved"
          onChange={mockOnChange}
          stats={mockStats}
        />
      );

      select = screen.getByLabelText("Sipariş Durumu");
      expect(select).toHaveValue("Approved");
    });
  });
});
