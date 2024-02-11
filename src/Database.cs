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

    public async Task<Extrato> ObtemExtratoAsync(int id)
    {
        var sql = "SELECT saldo, limite FROM clientes WHERE id = @id";
        
        using var command = new NpgsqlCommand(sql, _connection);
        command.Parameters.AddWithValue("id", id);

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new ClienteNaoEncontradoException();

        var saldo = new Saldo() { Total = reader.GetInt32(0), Limite = reader.GetInt32(1) };
        return new Extrato() { Saldo = saldo, UltimasTransacoes = Array.Empty<Transacao>() };
    }
}

[Serializable]
internal class ClienteNaoEncontradoException : Exception
{

}