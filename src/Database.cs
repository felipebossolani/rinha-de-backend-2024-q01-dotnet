using Npgsql;

namespace rinha_dotnet_8;

public sealed class Database
{
    //private readonly NpgsqlConnection connection;
    private readonly string _connectionString;

    internal Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    internal async Task<Extrato> ObtemExtratoAsync(int idCliente, CancellationToken cancellationToken)
    {
        var saldo = await ObtemSaldo(idCliente, cancellationToken);
        return new Extrato() { Saldo = saldo, UltimasTransacoes = await ObtemUltimasTransacoesAsync(idCliente, cancellationToken) };
    }

    private async Task<Saldo> ObtemSaldo(int idCliente, CancellationToken cancellationToken)
    {
        var sql = "SELECT saldo, limite FROM clientes WHERE id = @id";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        try
        {
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", idCliente);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync())
                throw new ClienteNaoEncontradoException();

            return new() { Total = reader.GetInt32(0), Limite = Math.Abs(reader.GetInt32(1)) };
        }
        finally
        {
            connection.Close();
        }
    }

    private async Task<IReadOnlyCollection<Transacao>> ObtemUltimasTransacoesAsync(int idCliente, CancellationToken cancellationToken)
    {
        var result = new List<Transacao>(10);
        var sql = "SELECT valor, tipo, descricao, realizada_em  FROM transacoes WHERE idcliente = @idcliente ORDER BY realizada_em DESC LIMIT 10;";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        try
        {
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("idcliente", idCliente);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync())
            {
                result.Add(new()
                {
                    Valor = reader.GetInt32(0),
                    Tipo = reader.GetChar(1),
                    Descricao = reader.GetString(2),
                    RealizadaEm = reader.GetDateTime(3)
                });
            }
            return result;
        }
        finally
        {
            connection.Close();
        }
    }

    internal async Task<TransacaoOK> RealizaTransacao(int idCliente, Transacao transacao, CancellationToken cancellationToken)
    {
        var saldo = await ObtemSaldo(idCliente, cancellationToken);

        if (ExcedeLimite(valor: transacao.Valor, saldo: saldo.Total, limite: -saldo.Limite))
            throw new LimiteExcedidoException();

        await InsereTransacao(idCliente, transacao, cancellationToken);

        var novoSaldo = saldo.Total + transacao.Valor;
        return new() { Limite = Math.Abs(saldo.Limite), Saldo = novoSaldo };
    }

    private async Task<int> InsereTransacao(int idCliente, Transacao transacao, CancellationToken cancellationToken)
    {
        var sql = """
            INSERT INTO transacoes(tipo, valor, descricao, realizada_em, idcliente)
            VALUES(@tipo, @valor_abs, @descricao, @realizada_em, @idcliente);

            UPDATE clientes
            SET saldo = saldo + @valor
            WHERE id = @idcliente;
            """;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        try
        {
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tipo", transacao.Tipo);
            command.Parameters.AddWithValue("valor_abs", Math.Abs(transacao.Valor));
            command.Parameters.AddWithValue("valor", transacao.Valor);
            command.Parameters.AddWithValue("descricao", transacao.Descricao);
            command.Parameters.AddWithValue("realizada_em", transacao.RealizadaEm);
            command.Parameters.AddWithValue("idcliente", idCliente);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            connection.Close();
        }
    }

    private bool ExcedeLimite(int valor, int saldo, int limite) => (valor + saldo < limite);
}