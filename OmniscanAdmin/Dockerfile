FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OmniscanAdmin/OmniscanAdmin.csproj", "OmniscanAdmin/"]
RUN dotnet restore "OmniscanAdmin/OmniscanAdmin.csproj"
COPY . .
WORKDIR "/src/OmniscanAdmin"
RUN dotnet build "./OmniscanAdmin.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OmniscanAdmin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy data files
COPY ["OmniscanAdmin/chips.json", "./chips.json"]
COPY ["OmniscanAdmin/users.json", "./users.json"]
ENTRYPOINT ["dotnet", "OmniscanAdmin.dll"]
