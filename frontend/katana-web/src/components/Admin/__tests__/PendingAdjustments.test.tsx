import React from "react";
import { render, screen, act } from "@testing-library/react";
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

jest.mock("../../../services/signalr", () => ({
  startConnection: () => Promise.resolve(),
  stopConnection: () => Promise.resolve(),
  onPendingCreated: (h: (p: any) => void) => {
    createdHandler = h;
  },
  offPendingCreated: () => {},
  onPendingApproved: () => {},
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
    await screen.findByText(/No pending adjustments/i);

    // Simulate SignalR event inside act to avoid unwrapped state updates
    expect(createdHandler).toBeTruthy();
    await act(async () => {
      createdHandler!({
        pending: {
          id: 101,
          sku: "SKU-1",
          productId: 1,
          quantity: 2,
          status: "Pending",
        },
      });
    });

    // Row should appear
    await screen.findByText("101");
    expect(screen.getByText("SKU-1")).toBeInTheDocument();
  });
});
