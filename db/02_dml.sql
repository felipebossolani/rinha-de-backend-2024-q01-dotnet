delete from transacoes;
delete from clientes;

INSERT INTO clientes (id, saldo, limite)
VALUES 
    (1, 0, -100000),
    (2, 0, -80000),
    (3, 0, -1000000),
    (4, 0, -10000000),
    (5, 0, -500000);
