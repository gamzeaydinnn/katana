-- Check if orders exist in database
SELECT 
    Id, 
    OrderNo, 
    KatanaOrderId, 
    CustomerId, 
    Status, 
    Total, 
    OrderCreatedDate,
    CreatedAt
FROM SalesOrders
WHERE OrderNo IN ('SO-41', 'SO-47', 'SO-56')
   OR KatanaOrderId IN (
       SELECT CAST(SUBSTRING(OrderNo, 4, LEN(OrderNo)-3) AS BIGINT)
       FROM (VALUES ('SO-41'), ('SO-47'), ('SO-56')) AS T(OrderNo)
   )
ORDER BY OrderCreatedDate DESC;

-- Check all orders
SELECT COUNT(*) AS TotalOrders FROM SalesOrders;

-- Check recent orders
SELECT TOP 10
    Id, 
    OrderNo, 
    KatanaOrderId, 
    CustomerId, 
    Status, 
    Total, 
    OrderCreatedDate
FROM SalesOrders
ORDER BY CreatedAt DESC;
