FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/Katana.API/Katana.API.csproj", "src/Katana.API/"]
COPY ["src/Katana.Business/Katana.Business.csproj", "src/Katana.Business/"]
COPY ["src/Katana.Core/Katana.Core.csproj", "src/Katana.Core/"]
COPY ["src/Katana.Data/Katana.Data.csproj", "src/Katana.Data/"]
COPY ["src/Katana.Infrastructure/Katana.Infrastructure.csproj", "src/Katana.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Katana.API/Katana.API.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Katana.API"
RUN dotnet build "Katana.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Katana.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Katana.API.dll"]