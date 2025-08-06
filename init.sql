-- Enable pgcrypto for UUID generation
CREATE
EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS users
(
    id
    UUID
    PRIMARY
    KEY
    DEFAULT
    gen_random_uuid
(
),
    name VARCHAR
(
    100
) NOT NULL,
    age int NOT NULL
    );


CREATE TABLE IF NOT EXISTS products
(
    id
    SERIAL
    PRIMARY
    KEY,
    name
    VARCHAR
(
    100
) NOT NULL,
    price DECIMAL
(
    10,
    2
) NOT NULL
    );

-- Insert sample data
INSERT INTO users (name, age)
VALUES ('Alice', 20),
       ('Bob', 18),
       ('Carol', 25);

INSERT INTO products (name, price)
VALUES ('Standard Widget', 9.99),
       ('Premium Widget', 19.99);