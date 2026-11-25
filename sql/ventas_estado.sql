-- Agrega el estado a las ventas. Las ventas existentes quedan como 'Completado'.
-- Se puede ejecutar mas de una vez.
ALTER TABLE ventas_cabecera
    ADD COLUMN IF NOT EXISTS estado ENUM('Completado','Pendiente','Anulado') NOT NULL DEFAULT 'Completado' AFTER iva;
