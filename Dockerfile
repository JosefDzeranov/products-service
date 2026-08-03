# Этап 1. Сборка, здесь нужен SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файл проекта, чтобы сработал кэш слоев
COPY *.csproj .
RUN dotnet restore

# Теперь весь остальной код
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Этап 2. Запуск, здесь достаточно runtime для ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET внутри контейнера слушает порт 8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductsService.dll"]
