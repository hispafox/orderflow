-- Backfill/verificación de BuyerId
-- 1. Detectar inconsistencias (debe devolver 0 antes de la fase Contract)

SELECT COUNT(*) AS Inconsistencies
FROM orders.Orders
WHERE BuyerId IS NULL
   OR BuyerId <> CustomerId;

-- 2. Si hay inconsistencias, copiar desde CustomerId (idempotente)

UPDATE orders.Orders
SET BuyerId = CustomerId
WHERE BuyerId IS NULL OR BuyerId <> CustomerId;

-- 3. Re-verificar — tras el UPDATE debe devolver 0

SELECT COUNT(*) AS RemainingInconsistencies
FROM orders.Orders
WHERE BuyerId IS NULL
   OR BuyerId <> CustomerId;
