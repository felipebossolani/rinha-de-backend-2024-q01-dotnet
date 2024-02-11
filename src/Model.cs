using System;

namespace rinha_dotnet_8;

public record struct Extrato(Saldo Saldo, IReadOnlyCollection<Transacao> UltimasTransacoes);

public record struct Saldo(int Total, int Limite)
{
    public DateTime DataExtrato => DateTime.UtcNow;
}

public record struct Transacao(int Valor, string Tipo, string Descricao, DateTime RealizadaEm);