-- Clear pending adjustments for specific orders
DELETE FROM PendingStockAdjustments
WHERE ExternalOrderId IN ('SO-41', 'SO-47', 'SO-56');

-- Check remaining pending adjustments
SELECT COUNT(*) AS RemainingPendingAdjustments FROM PendingStockAdjustments;
