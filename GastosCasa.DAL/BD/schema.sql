-- Script de la base de datos (SQLite).
-- Lo ejecuta InicializadorBD al arrancar la API. "IF NOT EXISTS" hace que
-- se pueda correr muchas veces sin romper nada.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Usuario (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre        TEXT    NOT NULL UNIQUE COLLATE NOCASE,
    PasswordHash  TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Categoria (
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre  TEXT    NOT NULL,
    Tipo    INTEGER NOT NULL CHECK (Tipo IN (1, 2)),   -- 1 = Gasto, 2 = Ingreso
    UNIQUE (Nombre, Tipo)
);

CREATE TABLE IF NOT EXISTS Movimiento (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Tipo          INTEGER NOT NULL CHECK (Tipo IN (1, 2)),
    MontoCentavos INTEGER NOT NULL CHECK (MontoCentavos > 0), -- plata en centavos: sin errores de redondeo
    Fecha         TEXT    NOT NULL,                           -- 'yyyy-MM-dd' (así se ordena bien como texto)
    Descripcion   TEXT    NOT NULL DEFAULT '',
    CategoriaId   INTEGER NOT NULL REFERENCES Categoria(Id),
    UsuarioId     INTEGER NOT NULL REFERENCES Usuario(Id),
    CreadoEn      TEXT    NOT NULL                            -- fecha/hora UTC en formato ISO
);

-- Índice para que filtrar por mes sea rápido aunque haya miles de movimientos.
CREATE INDEX IF NOT EXISTS IX_Movimiento_Fecha ON Movimiento (Fecha);

-- Categorías iniciales. INSERT OR IGNORE: si ya existen (por el UNIQUE), no hace nada.
INSERT OR IGNORE INTO Categoria (Nombre, Tipo) VALUES
    ('Supermercado', 1), ('Servicios', 1), ('Transporte', 1), ('Salidas', 1),
    ('Salud', 1), ('Casa', 1), ('Educación', 1), ('Otros gastos', 1),
    ('Sueldo', 2), ('Changas', 2), ('Otros ingresos', 2);
