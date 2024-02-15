using System;

namespace rinha_dotnet_8;

//results
public record struct Extrato(Saldo Saldo, IReadOnlyCollection<Transacao> UltimasTransacoes);

public record struct Saldo(int Total, int Limite)
{
    public readonly DateTime DataExtrato => DateTime.UtcNow;
}

public record struct TransacaoOK(int Limite, int Saldo);

public record struct Transacao(int Valor, char Tipo, string Descricao, DateTime RealizadaEm);

//requests
public record struct TransacaoRequest(object Valor, char Tipo, string Descricao);

//Exceptions
[Serializable]
internal class ClienteNaoEncontradoException : Exception
{

}

[Serializable]
internal class LimiteExcedidoException : Exception
{

}