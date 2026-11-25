-- Datos de prueba (ficticios) para taller_mecanico.
-- Importar SOLO sobre tablas vacias: usa ids fijos; si ya existen, falla y no guarda nada.
-- Los empleados no tienen una clave valida (todavia no hay login).
SET NAMES utf8mb4;
START TRANSACTION;

INSERT INTO empresas (id, nombre, ruc, direccion, correo, rubro) VALUES
(1,'Taller El Pistón S.A.','80000001-1','Av. Principal 100','piston@example.com','Taller mecánico'),
(2,'Auto Service Guaraní','80000002-2','Ruta 2 km 15','guarani@example.com','Taller y repuestos'),
(3,'Mecánica Rápida del Este','80000003-3','Calle Comercio 250','este@example.com','Taller mecánico');

INSERT INTO sucursales (id, id_empresa, nombre, direccion, timbrado, punto_expedicion, ultimo_numero) VALUES
(1,1,'Casa Central','Av. Principal 100','10000001','001-001',0),
(2,1,'Sucursal Norte','Av. Norte 500','10000001','002-001',0),
(3,2,'Casa Central','Ruta 2 km 15','10000002','001-001',0),
(4,2,'Sucursal Luque','Calle Luque 80','10000002','002-001',0),
(5,3,'Casa Central','Calle Comercio 250','10000003','001-001',0),
(6,3,'Sucursal Km 7','Km 7 Monday','10000003','002-001',0);

INSERT INTO tipos_empleados (id, descripcion) VALUES (1,'Administrador'),(2,'Mecánico'),(3,'Vendedor'),(4,'Recepcionista');

INSERT INTO empleados (id, nombre, correo, password_hash, id_tipo_empleado, activo, id_empresa, id_sucursal)
SELECT seq, CONCAT(ELT(1+seq%9,'Carlos','María','José','Laura','Pedro','Ana','Diego','Sofía','Hugo'),' ',
       ELT(1+seq%7,'Benítez','González','Rojas','Villalba','Ortiz','Duarte','Acosta')),
       CONCAT('empleado',seq,'@example.com'), '!', 1+seq%4, seq<=16, (seq-1)%6 DIV 2 + 1, (seq-1)%6+1
  FROM seq_1_to_18;

INSERT INTO clientes (id, nombre, apellido, nacimiento, direccion, nro_documento, dv, telefono, correo, id_empresa, id_sucursal)
SELECT seq, ELT(1+seq%12,'Luis','Rosa','Miguel','Carmen','Jorge','Patricia','Raúl','Gladys','Óscar','Liz','Iván','Nilda'),
       ELT(1+seq%11,'Gómez','Martínez','Fernández','Cáceres','Ramírez','Núñez','Aquino','Vera','López','Giménez','Ayala'),
       DATE_ADD('1965-01-10', INTERVAL seq*113 DAY), CONCAT('Calle ',1+seq%40,' Nro. ',100+seq),
       3000000+seq*12347, seq%10, CONCAT('0981 000 ',LPAD(seq,3,'0')), CONCAT('cliente',seq,'@example.com'),
       (seq-1)%6 DIV 2 + 1, (seq-1)%6+1
  FROM seq_1_to_120;

INSERT INTO marcas (id, descripcion) VALUES (1,'Toyota'),(2,'Kia'),(3,'Hyundai'),(4,'Nissan'),(5,'Volkswagen'),
(6,'Chevrolet'),(7,'Ford'),(8,'Fiat'),(9,'Honda'),(10,'Mitsubishi'),(11,'Suzuki'),(12,'Isuzu'),(13,'Renault'),(14,'Peugeot'),(15,'Mercedes-Benz');

INSERT INTO vehiculos (id, id_cliente, chapa, id_marca, color, observacion, modelo, anho)
SELECT seq, (seq-1)%120+1, CONCAT('T',CHAR(65+seq%26 USING utf8mb4),CHAR(65+seq%25 USING utf8mb4),'C ',LPAD(seq,3,'0')),
       IF(seq%25=0,NULL,(seq-1)%15+1), ELT(1+seq%6,'Blanco','Negro','Gris','Plata','Rojo','Azul'),
       IF(seq%8=0,'Dato de prueba: revisar frenos',NULL),
       ELT((seq-1)%15+1,'Hilux','Sportage','Tucson','Frontier','Gol','Onix','Ranger','Strada','Civic','L200','Swift','D-Max','Duster','208','Sprinter'),
       1998+seq%28
  FROM seq_1_to_150;

INSERT INTO categorias (id, descripcion) VALUES (1,'Lubricantes'),(2,'Filtros'),(3,'Frenos'),(4,'Suspensión'),(5,'Eléctrico'),(6,'Motor');

INSERT INTO articulos (id, id_categoria, descripcion, precio, stock)
SELECT seq, (seq-1) DIV 10 + 1, ELT(seq,
'Aceite 5W30 sintético 4L','Aceite 10W40 semisintético 4L','Aceite 15W40 mineral 4L','Aceite 20W50 1L','Aceite de caja 75W90','Aceite ATF Dexron III','Grasa multiuso 500g','Líquido refrigerante 1L','Líquido hidráulico 1L','Manual de mantenimiento',
'Filtro de aceite Hilux','Filtro de aceite Gol','Filtro de aire Tucson','Filtro de aire Ranger','Filtro de combustible diésel','Filtro de combustible nafta','Filtro de cabina','Filtro de caja automática','Filtro de aire Onix','Catálogo de filtros',
'Pastillas de freno delanteras','Pastillas de freno traseras','Disco de freno delantero','Disco de freno trasero','Campana de freno','Zapatas de freno','Líquido de freno DOT4','Cilindro maestro','Manguera de freno','Guía de frenos',
'Amortiguador delantero','Amortiguador trasero','Espiral delantero','Rótula inferior','Extremo de dirección','Bieleta estabilizadora','Buje de parrilla','Parrilla de suspensión','Cazoleta','Guía de suspensión',
'Batería 60Ah','Batería 75Ah','Batería 90Ah','Lámpara H4','Lámpara H7','Kit LED H7','Alternador 12V','Motor de arranque','Fusibles surtidos','Guía eléctrica',
'Correa de distribución','Kit de distribución','Correa Poly-V','Bomba de agua','Termostato','Juego de bujías x4','Junta de tapa','Bobina de encendido','Radiador','Guía de motor'),
       IF(seq%10=0, 30000+seq*500, ROUND(20000+(seq*37%60)*14500,-3)), 0
  FROM seq_1_to_60;

INSERT INTO servicios (id, id_categoria, descripcion, precio_servicio, id_empleado)
SELECT seq, (seq-1)%6+1, ELT(seq,'Cambio de aceite','Cambio de filtros','Cambio de pastillas','Cambio de amortiguadores','Diagnóstico eléctrico','Afinación de motor',
'Engrase general','Limpieza de inyectores','Rectificado de discos','Alineación y balanceo','Cambio de batería','Cambio de distribución'), 80000+seq*35000, seq
  FROM seq_1_to_12;

-- 100 ordenes de trabajo
INSERT INTO ordenes_trabajo (id, id_empleado, id_vehiculo, fecha_recepcion, estado, diagnostico, fecha_diagnostico, fecha_finalizacion, id_empresa)
SELECT o.seq, e.id, v.id, o.f, o.estado,
       IF(o.n IN (1,8),NULL,'Diagnóstico de prueba'), IF(o.n IN (1,8),NULL,o.f + INTERVAL 2 HOUR),
       IF(o.n IN (6,7),o.f + INTERVAL 6 HOUR,NULL), e.id_empresa
  FROM (SELECT seq, 1+seq%8 AS n,
               ELT(1+seq%8,'Recibido','Diagnosticado','Presupuestado','Aceptado','En reparacion','Finalizado','Entregado','Cancelado') AS estado,
               CURDATE() - INTERVAL (101-seq) DAY + INTERVAL 8 HOUR AS f
          FROM seq_1_to_100) o
  JOIN vehiculos v ON v.id = o.seq
  JOIN clientes c ON c.id = v.id_cliente
  JOIN empleados e ON e.id = (c.id_empresa-1)*2+1+o.seq%2;

INSERT INTO ordenes_trabajo_articulos (id_orden, id_articulo, cantidad, precio)
SELECT o.seq, a.id, 1+o.seq%3, a.precio FROM seq_1_to_100 o JOIN articulos a ON a.id = 1+o.seq*7%60;

INSERT INTO ordenes_trabajo_servicios (id_orden, id_servicio, observacion, precio)
SELECT o.seq, s.id, 'Dato de prueba', s.precio_servicio FROM seq_1_to_100 o JOIN servicios s ON s.id = 1+o.seq%12;

INSERT INTO facturas (id_orden, id_sucursal, nro_factura, estado, fecha_emision)
SELECT o.id, e.id_sucursal, CONCAT(s.punto_expedicion,'-',LPAD(o.id,7,'0')),
       IF(o.estado='Entregado','Pagada','Pendiente'), o.fecha_finalizacion
  FROM ordenes_trabajo o
  JOIN empleados e ON e.id = o.id_empleado
  JOIN sucursales s ON s.id = e.id_sucursal
 WHERE o.estado IN ('Finalizado','Entregado');

INSERT INTO factura_detalle (id_factura, tipo_item, descripcion, cantidad, precio, gravada, exenta)
SELECT f.id,'Articulo',a.descripcion,x.cantidad,x.precio,IF(a.id%10=0,0,x.cantidad*x.precio),IF(a.id%10=0,x.cantidad*x.precio,0)
  FROM facturas f JOIN ordenes_trabajo_articulos x ON x.id_orden=f.id_orden JOIN articulos a ON a.id=x.id_articulo
UNION ALL
SELECT f.id,'Servicio',s.descripcion,1,x.precio,x.precio,0
  FROM facturas f JOIN ordenes_trabajo_servicios x ON x.id_orden=f.id_orden JOIN servicios s ON s.id=x.id_servicio;

INSERT INTO pagos (id_factura, fecha_pago, monto, metodo_pago, observacion)
SELECT f.id, f.fecha_emision, SUM(d.gravada+d.exenta), ELT(1+f.id%3,'Efectivo','Tarjeta','Transferencia'), 'Dato de prueba'
  FROM facturas f JOIN factura_detalle d ON d.id_factura=f.id
 WHERE f.estado='Pagada' GROUP BY f.id, f.fecha_emision;

UPDATE sucursales s SET ultimo_numero = COALESCE((SELECT MAX(f.id_orden) FROM facturas f WHERE f.id_sucursal=s.id),0);

-- 300 ventas con 1 a 4 lineas (los articulos multiplos de 10 son exentos)
INSERT INTO ventas_cabecera (id, id_cliente, id_empleado, id_empresa, id_sucursal, created_at, updated_at)
SELECT n.seq, 1+n.seq*13%120, e.id, e.id_empresa, e.id_sucursal, n.f, n.f
  FROM (SELECT seq, CURDATE() - INTERVAL (1+(300-seq) DIV 2) DAY + INTERVAL (480+seq%2*240+seq%50) MINUTE AS f
          FROM seq_1_to_300) n
  JOIN empleados e ON e.id = 1+n.seq*7%16;

INSERT INTO ventas_detalle (id_venta, id_articulo, cantidad, precio, iva, grav, excentas, created_at, updated_at)
SELECT x.id_venta, x.id_articulo, x.cant, x.precio,
       IF(x.ex,0,ROUND(x.cant*x.precio/11,2)),
       IF(x.ex,0,x.cant*x.precio-ROUND(x.cant*x.precio/11,2)),
       IF(x.ex,x.cant*x.precio,0), x.f, x.f
  FROM (SELECT v.id AS id_venta, a.id AS id_articulo, 1+(v.id+k.seq)%3 AS cant, a.precio, a.id%10=0 AS ex, v.created_at AS f, k.seq AS linea
          FROM ventas_cabecera v
          JOIN seq_1_to_4 k ON k.seq <= 1+v.id%4
          JOIN articulos a ON a.id = 1+(v.id*11+k.seq*17)%60) x
 ORDER BY x.id_venta, x.linea;

UPDATE ventas_cabecera v
  JOIN (SELECT id_venta, SUM(grav) g, SUM(excentas) e, SUM(iva) i FROM ventas_detalle GROUP BY id_venta) d ON d.id_venta=v.id
   SET v.grav=d.g, v.exc=d.e, v.iva=d.i, v.total=d.g+d.e+d.i, v.updated_at=v.created_at;

-- Stock que queda luego de las ventas: hay agotados (0) y con pocas unidades (2) para probar
UPDATE articulos SET stock = CASE WHEN id IN (5,25,45) THEN 0 WHEN id IN (8,28,48) THEN 2 ELSE 20+id*7%80 END;

COMMIT;
