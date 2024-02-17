using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace rinha_dotnet_8;
/*
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
        var sql = @"SELECT * FROM CriarTransacao(@idcliente, @tipo, @valor, @descricao);";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        try
        {
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("idcliente", idCliente);
            command.Parameters.AddWithValue("tipo", transacao.Tipo);
            command.Parameters.AddWithValue("valor", transacao.Valor);
            command.Parameters.AddWithValue("descricao", transacao.Descricao);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync())
            {
                var status = reader.GetInt32(0);
                if (status == -1)
                    throw new ClienteNaoEncontradoException();
                if (status == -2)
                    throw new LimiteExcedidoException();

                var saldo_novo = reader.GetInt32(1);
                var limite_novo = reader.GetInt32(2);
                return new() { Saldo = saldo_novo, Limite = limite_novo };
            }
            throw new Exception("A função CriarTransação não retornou valores. O que não deveria ocorrer.");
        }
        finally
        {
            connection.Close();
        }
    }
    private bool ExcedeLimite(char tipo, int valor, int saldo, int limite) => tipo.Equals('d') && (saldo - valor) < -limite;
    
}*/

public sealed class Database : IAsyncDisposable
{
    private bool disposed;
    private readonly NpgsqlCommand criarTransacaoCommand = CriarTransacaoCommand();
    private readonly NpgsqlCommand obterSaldoCommand = ObterSaldoCommand();
    private readonly NpgsqlCommand obterTransacoesCommand = ObterTransacoesCommand();
    private readonly NpgsqlDataSource dataSource;

    internal Database(string connectionString)
    {
        dataSource = new NpgsqlSlimDataSourceBuilder(connectionString).Build();
    }

    internal async Task<Extrato> ObtemExtratoAsync(int idCliente, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        var saldo = await ObtemSaldoAsync(idCliente, connection, cancellationToken);
        var ultimasTransacoes = await ObtemUltimasTransacoesAsync(idCliente, connection, cancellationToken);
        return new Extrato() { Saldo = saldo, UltimasTransacoes = ultimasTransacoes };
    }

    private async Task<IReadOnlyCollection<Transacao>> ObtemUltimasTransacoesAsync(int idCliente, NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var result = new List<Transacao>(10);
        using var command = obterTransacoesCommand.Clone();
        command.Connection = connection;
        command.Parameters["idCliente"].Value = idCliente;

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

    private async Task<Saldo> ObtemSaldoAsync(int idCliente, NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        using var command = obterSaldoCommand.Clone();
        command.Connection = connection;
        command.Parameters["id"].Value = idCliente;
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new ClienteNaoEncontradoException();

        return new() { Total = reader.GetInt32(0), Limite = Math.Abs(reader.GetInt32(1)) };
    }

    internal async Task<TransacaoOK> RealizaTransacao(int idCliente, Transacao transacao, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        await using var command = criarTransacaoCommand.Clone();
        command.Connection = connection;
        command.Parameters["idcliente"].Value = idCliente;
        command.Parameters["tipo"].Value = transacao.Tipo;
        command.Parameters["valor"].Value = transacao.Valor;
        command.Parameters["descricao"].Value = transacao.Descricao;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync())
        {
            var status = reader.GetInt32(0);
            if (status == -1)
                throw new ClienteNaoEncontradoException();
            if (status == -2)
                throw new LimiteExcedidoException();

            var saldo_novo = reader.GetInt32(1);
            var limite_novo = reader.GetInt32(2);
            return new() { Saldo = saldo_novo, Limite = limite_novo };
        }
        throw new Exception("A função CriarTransação não retornou valores. O que não deveria ocorrer.");
    }

    private NpgsqlConnection CreateConnection() => dataSource.OpenConnection();

    private static NpgsqlCommand CriarTransacaoCommand() =>
        new(@"SELECT * FROM CriarTransacao(@idcliente, @tipo, @valor, @descricao);")
        {
            Parameters =
            {
                new NpgsqlParameter<int>() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer, ParameterName = "@idcliente" },
                new NpgsqlParameter<char>() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Char, ParameterName = "@tipo" },
                new NpgsqlParameter<int>() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer, ParameterName = "@valor" },
                new NpgsqlParameter<string>() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar, ParameterName = "@descricao" }
            }
        };

    private static NpgsqlCommand ObterSaldoCommand() =>
        new("SELECT saldo, limite FROM clientes WHERE id = @id")
        { Parameters = { new NpgsqlParameter<int>() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer, ParameterName = "@id" } } };

    private static NpgsqlCommand ObterTransacoesCommand() =>
        new("SELECT valor, tipo, descricao, realizada_em  FROM transacoes WHERE idcliente = @idcliente ORDER BY realizada_em DESC LIMIT 10;")
        {
            Parameters = { new NpgsqlParameter<int>() { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer, ParameterName = "@idcliente" } }
        };

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;
        disposed = true;
        if (criarTransacaoCommand is not null)
            await criarTransacaoCommand.DisposeAsync();
        if (obterSaldoCommand is not null)
            await obterSaldoCommand.DisposeAsync();
        if (obterTransacoesCommand is not null)
            await obterTransacoesCommand.DisposeAsync();
    }


}