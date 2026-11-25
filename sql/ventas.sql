CREATE TABLE ventas_cabecera (
    id           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    id_cliente   BIGINT UNSIGNED NOT NULL,
    id_empleado  BIGINT UNSIGNED NOT NULL,
    id_empresa   BIGINT UNSIGNED NOT NULL,
    id_sucursal  BIGINT UNSIGNED NOT NULL,
    total        DECIMAL(15,2) NOT NULL DEFAULT 0,
    grav         DECIMAL(15,2) NOT NULL DEFAULT 0,
    exc          DECIMAL(15,2) NOT NULL DEFAULT 0,
    iva          DECIMAL(15,2) NOT NULL DEFAULT 0,
    created_at   TIMESTAMP NULL DEFAULT NULL,
    updated_at   TIMESTAMP NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY idx_vc_empresa_fecha (id_empresa, created_at),
    KEY idx_vc_sucursal (id_sucursal),
    KEY idx_vc_cliente (id_cliente),
    KEY idx_vc_empleado (id_empleado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ventas_detalle (
    id           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    id_venta     BIGINT UNSIGNED NOT NULL,
    id_articulo  BIGINT UNSIGNED NOT NULL,
    cantidad     DECIMAL(12,3) NOT NULL DEFAULT 1,
    precio       DECIMAL(15,2) NOT NULL DEFAULT 0,
    iva          DECIMAL(15,2) NOT NULL DEFAULT 0,
    grav         DECIMAL(15,2) NOT NULL DEFAULT 0,
    excentas     DECIMAL(15,2) NOT NULL DEFAULT 0,
    created_at   TIMESTAMP NULL DEFAULT NULL,
    updated_at   TIMESTAMP NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY idx_vd_venta (id_venta),
    KEY idx_vd_articulo (id_articulo),
    CONSTRAINT fk_vd_venta
        FOREIGN KEY (id_venta) REFERENCES ventas_cabecera (id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;