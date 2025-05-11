FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UserService/UserService.csproj", "UserService/"]
RUN dotnet restore "./UserService/UserService.csproj"

COPY . . 

RUN mkdir -p /app/certs && echo "Directory /app/certs created"
RUN mkdir -p /app/keys && echo "Directory /app/keys created"

COPY ["UserService/certs/devcert.pfx", "/app/certs/devcert.pfx"]

WORKDIR "/src/UserService"
RUN dotnet build "./UserService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=https://+:8080
ENV CERT_PATH=/app/certs/devcert.pfx
ENV CERT_PASSWORD=myPassword123

ENTRYPOINT ["dotnet", "UserService.dll"]