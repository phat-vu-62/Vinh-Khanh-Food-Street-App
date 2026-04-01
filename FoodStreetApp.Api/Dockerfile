FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FoodStreetApp.Api/FoodStreetApp.Api.csproj", "FoodStreetApp.Api/"]
COPY ["FoodStreetApp.Shared/FoodStreetApp.Shared.csproj", "FoodStreetApp.Shared/"]
RUN dotnet restore "FoodStreetApp.Api/FoodStreetApp.Api.csproj"

COPY FoodStreetApp.Api/. FoodStreetApp.Api/
COPY FoodStreetApp.Shared/. FoodStreetApp.Shared/
RUN dotnet publish "FoodStreetApp.Api/FoodStreetApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}; dotnet FoodStreetApp.Api.dll"]
