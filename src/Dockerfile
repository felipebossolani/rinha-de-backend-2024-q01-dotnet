FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.18 AS base
USER app
WORKDIR /app
ENV ASPNETCORE_URLS http://*:9999
EXPOSE 9999

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.18 AS build
WORKDIR /src
COPY ["rinha-dotnet-8.csproj", "."]
RUN dotnet restore "./rinha-dotnet-8.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "rinha-dotnet-8.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "rinha-dotnet-8.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "rinha-dotnet-8.dll"]


#docker build -t nome-da-imagem .
#docker run -e "ConnectionStrings__rinha=Host=localhost;Database=rinha;Username=user;Password=password;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;" -d -p 9999:9999 rinha-de-backend-2024-q01-dotnet 