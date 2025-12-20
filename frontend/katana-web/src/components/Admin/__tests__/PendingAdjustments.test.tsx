import React from "react";
import { render, screen, act } from "@testing-library/react";
import PendingAdjustments from "../PendingAdjustments";
import { FeedbackProvider } from "../../../providers/FeedbackProvider";


jest.mock("../../../services/api", () => ({
  pendingAdjustmentsAPI: {
    list: () => Promise.resolve({ items: [] }),
    approve: jest.fn(),
    reject: jest.fn(),
  },
}));


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
  onPendingRejected: () => {},
  offPendingRejected: () => {},
}));

describe("PendingAdjustments SignalR Integration", () => {
  it("should update list when onPendingCreated fires", async () => {
    render(
      <FeedbackProvider>
        <PendingAdjustments />
      </FeedbackProvider>
    );

    
    await screen.findByText(/No pending adjustments/i);

    
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

    
    await screen.findByText("101");
    expect(screen.getByText("SKU-1")).toBeInTheDocument();
  });
});
