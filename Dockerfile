# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ResponsabiliMano.slnx ./
COPY src/ResponsabiliMano.Core/ResponsabiliMano.Core.csproj src/ResponsabiliMano.Core/
COPY src/ResponsabiliMano.Infrastructure/ResponsabiliMano.Infrastructure.csproj src/ResponsabiliMano.Infrastructure/
COPY src/ResponsabiliMano.Web/ResponsabiliMano.Web.csproj src/ResponsabiliMano.Web/

RUN dotnet restore src/ResponsabiliMano.Web/ResponsabiliMano.Web.csproj

COPY . .

RUN dotnet publish src/ResponsabiliMano.Web/ResponsabiliMano.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ResponsabiliMano.Web.dll"]
