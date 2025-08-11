FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NZTSite/NZTSite.csproj", "NZTSite/"]
RUN dotnet restore "NZTSite/NZTSite.csproj"
COPY . .
WORKDIR "/src/NZTSite"
RUN dotnet build "./NZTSite.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NZTSite.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy the HTML file from the source
COPY ["NZTSite/index.html", "./index.html"]
ENTRYPOINT ["dotnet", "NZTSite.dll"]