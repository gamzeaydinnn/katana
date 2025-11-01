import React from "react";
import { render, screen, waitFor } from "@testing-library/react";
import PendingAdjustments from "../PendingAdjustments";
import { FeedbackProvider } from "../../../providers/FeedbackProvider";

// Mock API to return empty list initially
jest.mock("../../../services/api", () => ({
  pendingAdjustmentsAPI: {
    list: () => Promise.resolve({ items: [] }),
    approve: jest.fn(),
    reject: jest.fn(),
  },
}));

// Capture handlers to simulate SignalR events
let createdHandler: ((p: any) => void) | null = null;
let approvedHandler: ((p: any) => void) | null = null;

jest.mock("../../../services/signalr", () => ({
  startConnection: () => Promise.resolve(),
  stopConnection: () => Promise.resolve(),
  onPendingCreated: (h: (p: any) => void) => {
    createdHandler = h;
  },
  offPendingCreated: () => {},
  onPendingApproved: (h: (p: any) => void) => {
    approvedHandler = h;
  },
  offPendingApproved: () => {},
}));

describe("PendingAdjustments SignalR Integration", () => {
  it("should update list when onPendingCreated fires", async () => {
    render(
      <FeedbackProvider>
        <PendingAdjustments />
      </FeedbackProvider>
    );

    // Initially shows 'No pending adjustments'
    await waitFor(() => screen.getByText(/No pending adjustments/i));

    // Simulate SignalR event
    createdHandler &&
      createdHandler({ pending: { id: 101, sku: "SKU-1", productId: 1, quantity: 2, status: "Pending" } });

    // Row should appear
    await waitFor(() => screen.getByText("101"));
    expect(screen.getByText("SKU-1")).toBeInTheDocument();
  });
});

