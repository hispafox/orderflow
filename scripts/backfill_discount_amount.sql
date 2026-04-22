-- Backfill idempotente de DiscountAmount en orders.Orders
-- Ejecutable múltiples veces sin efectos secundarios.
-- Procesa en lotes de 1.000 con pausa de 100ms entre lotes (seguro para producción).

DECLARE @BatchSize INT = 1000;
DECLARE @Processed INT = 0;
DECLARE @BatchRows INT;

PRINT 'Starting DiscountAmount backfill...';

WHILE 1 = 1
BEGIN
    UPDATE TOP(@BatchSize) orders.Orders
    SET DiscountAmount = 0.00
    WHERE DiscountAmount IS NULL;

    SET @BatchRows = @@ROWCOUNT;
    SET @Processed = @Processed + @BatchRows;

    IF @BatchRows = 0 BREAK;

    PRINT CONCAT('Processed ', @Processed, ' rows...');
    WAITFOR DELAY '00:00:00.100';
END

PRINT CONCAT('Backfill complete. Total rows updated: ', @Processed);
