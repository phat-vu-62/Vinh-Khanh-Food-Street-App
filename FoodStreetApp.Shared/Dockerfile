FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FoodStreetApp.CMS/FoodStreetApp.CMS.csproj", "FoodStreetApp.CMS/"]
COPY ["FoodStreetApp.Shared/FoodStreetApp.Shared.csproj", "FoodStreetApp.Shared/"]

RUN dotnet restore "FoodStreetApp.CMS/FoodStreetApp.CMS.csproj"

COPY FoodStreetApp.CMS/. FoodStreetApp.CMS/
COPY FoodStreetApp.Shared/. FoodStreetApp.Shared/

RUN dotnet publish "FoodStreetApp.CMS/FoodStreetApp.CMS.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Add dependencies for GSSAPI/PostgreSQL and clean up
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://*:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FoodStreetApp.CMS.dll"]
