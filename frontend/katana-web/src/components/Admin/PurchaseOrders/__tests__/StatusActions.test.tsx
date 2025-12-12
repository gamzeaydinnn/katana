import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import StatusActions from "../StatusActions";

describe("StatusActions", () => {
  const mockOnStatusChange = vi.fn(() => Promise.resolve());

  beforeEach(() => {
    mockOnStatusChange.mockClear();
  });

  describe("Button visibility based on status", () => {
    it("should show Approve button when status is Pending", () => {
      render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      expect(screen.getByText("Siparişi Onayla")).toBeInTheDocument();
      expect(screen.queryByText("Teslim Alındı")).not.toBeInTheDocument();
    });

    it("should show Receive button when status is Approved", () => {
      render(
        <StatusActions
          currentStatus="Approved"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      expect(screen.getByText("Teslim Alındı")).toBeInTheDocument();
      expect(screen.queryByText("Siparişi Onayla")).not.toBeInTheDocument();
    });

    it("should show no buttons when status is Received", () => {
      const { container } = render(
        <StatusActions
          currentStatus="Received"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      expect(container.firstChild).toBeNull();
    });

    it("should show no buttons when status is Cancelled", () => {
      const { container } = render(
        <StatusActions
          currentStatus="Cancelled"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      expect(container.firstChild).toBeNull();
    });
  });

  describe("Button disabled state during loading", () => {
    it("should disable Approve button when loading", () => {
      render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={true}
        />
      );

      const button = screen.getByText("Siparişi Onayla").closest("button");
      expect(button).toBeDisabled();
    });

    it("should disable Receive button when loading", () => {
      render(
        <StatusActions
          currentStatus="Approved"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={true}
        />
      );

      const button = screen.getByText("Teslim Alındı").closest("button");
      expect(button).toBeDisabled();
    });

    it("should show loading spinner when loading", () => {
      const { container } = render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={true}
        />
      );

      const spinner = container.querySelector(".MuiCircularProgress-root");
      expect(spinner).toBeInTheDocument();
    });
  });

  describe("Confirmation dialog", () => {
    it("should open confirmation dialog when Approve button is clicked", () => {
      render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      const approveButton = screen.getByText("Siparişi Onayla");
      fireEvent.click(approveButton);

      expect(
        screen.getByText("Siparişi Onayla", { selector: "h2" })
      ).toBeInTheDocument();
      expect(
        screen.getByText(/PO-001 numaralı siparişi onaylamak/)
      ).toBeInTheDocument();
    });

    it("should open confirmation dialog when Receive button is clicked", () => {
      render(
        <StatusActions
          currentStatus="Approved"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      const receiveButton = screen.getByText("Teslim Alındı");
      fireEvent.click(receiveButton);

      expect(
        screen.getByText("Teslim Alındı Olarak İşaretle")
      ).toBeInTheDocument();
      expect(
        screen.getByText(/PO-001 numaralı siparişin teslim alındığını/)
      ).toBeInTheDocument();
    });

    it("should close dialog when Cancel button is clicked", async () => {
      render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      const approveButton = screen.getByText("Siparişi Onayla");
      fireEvent.click(approveButton);

      const cancelButton = screen.getByText("İptal");
      fireEvent.click(cancelButton);

      await waitFor(() => {
        expect(
          screen.queryByText("Siparişi Onayla", { selector: "h2" })
        ).not.toBeInTheDocument();
      });
    });

    it("should call onStatusChange with Approved when confirm is clicked for approve action", async () => {
      render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      const approveButton = screen.getByText("Siparişi Onayla");
      fireEvent.click(approveButton);

      const confirmButton = screen.getAllByText("Onayla")[1]; // Second one is in dialog
      fireEvent.click(confirmButton);

      await waitFor(() => {
        expect(mockOnStatusChange).toHaveBeenCalledWith("Approved");
      });
    });

    it("should call onStatusChange with Received when confirm is clicked for receive action", async () => {
      render(
        <StatusActions
          currentStatus="Approved"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={false}
        />
      );

      const receiveButton = screen.getByText("Teslim Alındı");
      fireEvent.click(receiveButton);

      const confirmButton = screen.getByText("Onayla");
      fireEvent.click(confirmButton);

      await waitFor(() => {
        expect(mockOnStatusChange).toHaveBeenCalledWith("Received");
      });
    });

    it("should disable dialog buttons when loading", () => {
      render(
        <StatusActions
          currentStatus="Pending"
          orderNo="PO-001"
          onStatusChange={mockOnStatusChange}
          loading={true}
        />
      );

      const approveButton = screen.getByText("Siparişi Onayla");
      fireEvent.click(approveButton);

      const cancelButton = screen.getByText("İptal");
      const confirmButton = screen.getAllByText(/Onayla/)[1];

      expect(cancelButton).toBeDisabled();
      expect(confirmButton).toBeDisabled();
    });
  });
});
