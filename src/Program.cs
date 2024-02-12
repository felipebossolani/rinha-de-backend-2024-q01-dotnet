using Microsoft.AspNetCore.Http.HttpResults;
using rinha_dotnet_8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("rinha");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("No connection string found.");
    return 1;
}

builder.Services.AddSingleton<Database>(provider => new Database(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapGet("/clientes/{idCliente}/extrato", async Task<Results<Ok<Extrato>, NotFound>> (int idCliente, Database db, CancellationToken cancellationToken) =>
{
    try
    {
        var extrato = await db.ObtemExtratoAsync(idCliente, cancellationToken);
        return TypedResults.Ok(extrato);
    }
    catch (ClienteNaoEncontradoException)
    {
        return TypedResults.NotFound();
    }
})
.WithName("ObtemExtratoCliente")
.WithOpenApi();

app.MapPost("/clientes/{idCliente}/transacoes", async Task<Results<Ok<TransacaoOK>, NotFound, UnprocessableEntity>> (int idCliente, TransacaoRequest transacaoRequest, Database db, CancellationToken cancellationToken) =>
{
    try
    {
        var transacao = new Transacao(transacaoRequest.Valor, transacaoRequest.Tipo, transacaoRequest.Descricao, DateTime.UtcNow); ;
        var result = await db.RealizaTransacao(idCliente, transacao, cancellationToken);
        return TypedResults.Ok(result);
    }
    catch (ClienteNaoEncontradoException)
    {
        return TypedResults.NotFound();
    }
    catch (LimiteExcedidoException)
    {
        return TypedResults.UnprocessableEntity();
    }    
});

app.Run();

return 0;