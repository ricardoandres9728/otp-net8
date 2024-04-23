# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 7079
EXPOSE 5048

# Copiar el certificado al contenedor
COPY .aspnet/https/aspnetapp.pfx .aspnet/https/
ENV ASPNETCORE_Kestrel__Certificates__Default__Password="12345"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/.aspnet/https/aspnetapp.pfx
ENV ASPNETCORE_URLS="https://+5048;http://+5048"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["otel.csproj", "."]
RUN dotnet restore "./otel.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./otel.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./otel.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "otel.dll"]
