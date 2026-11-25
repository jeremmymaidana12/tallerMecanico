-- Tabla de modelos por marca (la usa el boton "+" de Vehiculos y el ABM de Modelos).
-- La aplicacion la crea sola al conectarse (Db.AsegurarEsquema); este script es por si
-- se quiere crear a mano desde phpMyAdmin. Se puede ejecutar mas de una vez.
CREATE TABLE IF NOT EXISTS modelos (
    id          INT(11)      NOT NULL AUTO_INCREMENT,
    id_marca    INT(11)      NOT NULL,
    descripcion VARCHAR(100) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_modelo_marca (id_marca, descripcion),
    CONSTRAINT fk_modelos_marca FOREIGN KEY (id_marca) REFERENCES marcas (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Carga inicial: los modelos que ya estan escritos en los vehiculos.
INSERT IGNORE INTO modelos (id_marca, descripcion)
SELECT DISTINCT id_marca, TRIM(modelo)
  FROM vehiculos
 WHERE id_marca IS NOT NULL AND COALESCE(TRIM(modelo), '') <> '';
