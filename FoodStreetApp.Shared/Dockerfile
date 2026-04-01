FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FoodStreetApp.Api/FoodStreetApp.Api.csproj", "FoodStreetApp.Api/"]
COPY ["FoodStreetApp.CMS/FoodStreetApp.CMS.csproj", "FoodStreetApp.CMS/"]
COPY ["FoodStreetApp.Shared/FoodStreetApp.Shared.csproj", "FoodStreetApp.Shared/"]

RUN dotnet restore "FoodStreetApp.Api/FoodStreetApp.Api.csproj"
RUN dotnet restore "FoodStreetApp.CMS/FoodStreetApp.CMS.csproj"

COPY FoodStreetApp.Api/. FoodStreetApp.Api/
COPY FoodStreetApp.CMS/. FoodStreetApp.CMS/
COPY FoodStreetApp.Shared/. FoodStreetApp.Shared/

RUN dotnet publish "FoodStreetApp.Api/FoodStreetApp.Api.csproj" -c Release -o /app/publish/api /p:UseAppHost=false
RUN dotnet publish "FoodStreetApp.CMS/FoodStreetApp.CMS.csproj" -c Release -o /app/publish/cms /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish /app/publish

ENV PORT=8080
ENV APP_DLL=FoodStreetApp.Api.dll
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}; if [ \"$APP_DLL\" = \"FoodStreetApp.CMS.dll\" ]; then cd /app/publish/cms; else cd /app/publish/api; fi; dotnet $APP_DLL"]
