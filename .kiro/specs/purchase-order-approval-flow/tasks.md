# Implementation Plan

- [x] 1. Create StatusBadge component

  - Create new component file `frontend/katana-web/src/components/Admin/PurchaseOrders/StatusBadge.tsx`
  - Implement status-to-color mapping (Pending→warning, Approved→info, Received→success, Cancelled→error)
  - Add Material-UI icons for each status
  - Support size prop (small, medium)
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 1.1 Write unit tests for StatusBadge

  - Test correct color for each status
  - Test correct icon for each status
  - Test correct label for each status
  - Test size prop variations
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 2. Create StatusActions component

  - Create new component file `frontend/katana-web/src/components/Admin/PurchaseOrders/StatusActions.tsx`
  - Implement Approve button (visible when status=Pending)
  - Implement Receive button (visible when status=Approved)
  - Add loading state with disabled buttons and spinner
  - Add confirmation dialog for status changes
  - _Requirements: 1.2, 1.5, 4.1, 4.4_

- [x] 2.1 Write unit tests for StatusActions

  - Test Approve button visibility for Pending status
  - Test Receive button visibility for Approved status
  - Test no buttons for Received status
  - Test button disabled state during loading
  - Test confirmation dialog display
  - _Requirements: 1.2, 1.5, 4.1, 4.4_

- [x] 3. Create KatanaSyncStatus component

  - Create new component file `frontend/katana-web/src/components/Admin/PurchaseOrders/KatanaSyncStatus.tsx`
  - Display success/fail count chips
  - Create table showing SKU, product name, action (created/updated), status
  - Add tooltip for error messages
  - Show new stock quantity when available
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 3.1 Write unit tests for KatanaSyncStatus

  - Test success/fail count calculation
  - Test table rendering with sync results
  - Test error tooltip display
  - Test empty state handling
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 4. Create StatusFilter component

  - Create new component file `frontend/katana-web/src/components/Admin/PurchaseOrders/StatusFilter.tsx`
  - Implement Material-UI Select with status options
  - Show count for each status from stats
  - Add "Tümü" option showing total count
  - Emit onChange event when filter changes
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 4.1 Write unit tests for StatusFilter

  - Test all filter options rendering
  - Test count display for each option
  - Test onChange callback invocation
  - Test default value handling
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 5. Update PurchaseOrders main component - Add status update API call

  - Add `updateOrderStatus` function that calls `PATCH /api/purchase-orders/{id}/status`
  - Add `statusUpdating` state variable
  - Handle success response and update orderDetail state
  - Handle error response and show error message
  - Add auto-refresh after successful update
  - _Requirements: 1.3, 3.1, 3.2, 3.3, 3.4_

- [x] 5.1 Write integration tests for status update API call

  - Test successful Pending→Approved transition
  - Test successful Approved→Received transition
  - Test invalid transition rejection
  - Test network error handling
  - Test permission error handling
  - _Requirements: 1.3, 3.1, 3.2, 3.3, 3.4, 6.1, 6.2, 6.3_

- [x] 6. Update PurchaseOrders main component - Add status filter

  - Add `statusFilter` state variable
  - Add StatusFilter component to list view
  - Update `fetchOrders` to include status filter parameter
  - Update stats display to show counts per status
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 6.1 Write integration tests for status filter

  - Test filter parameter sent to API
  - Test list updates when filter changes
  - Test "Tümü" filter shows all orders
  - Test each status filter shows correct orders
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 7. Update OrderDetail view - Integrate StatusActions

  - Import and add StatusActions component to detail view
  - Pass order and updateOrderStatus callback
  - Position below order info card
  - Show/hide based on order status
  - _Requirements: 1.2, 1.5, 4.1, 4.4_

- [x] 8. Update OrderDetail view - Integrate KatanaSyncStatus

  - Import and add KatanaSyncStatus component to detail view
  - Show only when order status is Approved or Received
  - Display katanaSyncResults from order detail
  - Position below order items table
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 9. Update OrderList view - Integrate StatusBadge

  - Import StatusBadge component
  - Replace current status text with StatusBadge in table
  - Add status column if not present
  - Ensure proper alignment and spacing
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 10. Update OrderList view - Integrate StatusFilter

  - Import StatusFilter component
  - Add filter controls above table
  - Connect to statusFilter state
  - Update fetchOrders call when filter changes
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 11. Add success/error notifications

  - Use existing Snackbar component
  - Show success message: "Sipariş onaylandı ve Katana'ya gönderildi"
  - Show error message with details from API response
  - Auto-hide after 5 seconds for success, 10 seconds for error
  - _Requirements: 3.1, 3.2_

- [x] 11.1 Write tests for notification display

  - Test success notification on successful update
  - Test error notification on failed update
  - Test notification auto-hide timing
  - Test notification message content
  - _Requirements: 3.1, 3.2_

- [x] 12. Update TypeScript interfaces

  - Add `statusHistory` field to PurchaseOrderDetail interface
  - Add `katanaSyncResults` field to PurchaseOrderDetail interface
  - Create KatanaSyncResult interface
  - Create StatusHistoryEntry interface
  - Create UpdateStatusRequest interface
  - Create UpdateStatusResponse interface
  - _Requirements: All_

- [x] 13. Add error handling utilities

  - Create `handleStatusUpdateError` function
  - Map HTTP status codes to user-friendly messages
  - Determine if error is retryable
  - Export ErrorState interface
  - _Requirements: 3.2, 6.3_

- [x] 13.1 Write tests for error handling utilities

  - Test 403 permission error mapping
  - Test 400 validation error mapping
  - Test network error mapping
  - Test unknown error mapping
  - Test retryable flag logic
  - _Requirements: 3.2, 6.3_

- [x] 14. Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [x] 15. Update backend response (if needed)

  - Check if backend returns katanaSyncResults in status update response
  - If not, update PurchaseOrdersController to include sync results
  - Update response DTO to include katanaSyncResults array
  - _Requirements: 7.1, 7.2, 7.3_

- [x] 15.1 Write backend tests for sync results

  - Test katanaSyncResults included in response
  - Test sync results format
  - Test sync results for successful items
  - Test sync results for failed items
  - _Requirements: 7.1, 7.2, 7.3_

- [x] 16. Add status transition validation on frontend

  - Create `isValidTransition` function
  - Check current status before allowing update
  - Show error message for invalid transitions
  - Disable buttons for invalid transitions
  - _Requirements: 6.1, 6.2, 6.3_

- [x] 16.1 Write tests for status transition validation

  - Test Pending→Approved is valid
  - Test Approved→Received is valid
  - Test Pending→Received is invalid
  - Test Received→Approved is invalid
  - Test Approved→Pending is invalid
  - _Requirements: 6.1, 6.2, 6.3_

- [x] 17. Add loading states and optimistic updates

  - Show loading spinner on status update button
  - Disable all action buttons during update
  - Optionally show optimistic status change
  - Revert on error
  - _Requirements: 3.3_

- [x] 17.1 Write tests for loading states

  - Test button disabled during update
  - Test spinner display during update
  - Test optimistic update display
  - Test revert on error
  - _Requirements: 3.3_

- [x] 18. Add confirmation dialogs

  - Create confirmation dialog for Approve action
  - Create confirmation dialog for Receive action
  - Show order summary in dialog
  - Add "Cancel" and "Confirm" buttons
  - _Requirements: 1.3, 4.2_

- [x] 18.1 Write tests for confirmation dialogs

  - Test dialog opens on button click
  - Test dialog shows correct order info
  - Test cancel button closes dialog
  - Test confirm button triggers update
  - _Requirements: 1.3, 4.2_

- [x] 19. Update stats display

  - Ensure stats API includes pending, approved, received counts
  - Update StatsCards component to show all status counts
  - Add visual indicators (icons, colors) for each status
  - Update on filter change
  - _Requirements: 2.1_

- [x] 19.1 Write tests for stats display

  - Test stats API call
  - Test stats display for each status
  - Test stats update on filter change
  - Test stats visual indicators
  - _Requirements: 2.1_

- [x] 20. Add status history display (optional enhancement)

  - Create StatusHistory component
  - Show timeline of status changes
  - Display who changed status and when
  - Show any notes/comments
  - _Requirements: 5.5_

- [x] 20.1 Write tests for status history

  - Test history timeline rendering
  - Test user and timestamp display
  - Test notes display
  - Test empty history state
  - _Requirements: 5.5_

- [x] 21. Final Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [x] 22. Manual testing and polish

  - Test complete flow: Create → Approve → Receive
  - Test error scenarios
  - Test filter functionality
  - Verify UI/UX consistency
  - Check responsive design
  - Verify accessibility
  - _Requirements: All_
