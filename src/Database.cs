using Npgsql;

namespace rinha_dotnet_8;

public sealed class Database
{
    private readonly NpgsqlConnection _connection;

    public Database(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }

    public async Task<Extrato> ObtemExtratoAsync(int idCliente)
    {
        var saldo = await ObtemSaldo(idCliente);
        return new Extrato() { Saldo = saldo, UltimasTransacoes = await ObtemUltimasTransacoesAsync(idCliente) };
    }

    private async Task<Saldo> ObtemSaldo(int idCliente)
    {
        var sql = "SELECT saldo, limite FROM clientes WHERE idCliente = @idCliente";

        using var command = new NpgsqlCommand(sql, _connection);
        command.Parameters.AddWithValue("idcliente", idCliente);

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new ClienteNaoEncontradoException();

        return new() { Total = reader.GetInt32(0), Limite = reader.GetInt32(1) };
    }

    private async Task<IReadOnlyCollection<Transacao>> ObtemUltimasTransacoesAsync(int idCliente)
    {
        var result = new List<Transacao>(10);

        var sql = "SELECT valor, tipo, descricao, realizada_em  FROM transacoes WHERE idcliente = @idcliente ORDER BY realizada_em DESC LIMIT 10;";

        using var command = new NpgsqlCommand(sql, _connection);
        command.Parameters.AddWithValue("idcliente", idCliente);

        using var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync())
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
}

[Serializable]
internal class ClienteNaoEncontradoException : Exception
{

}