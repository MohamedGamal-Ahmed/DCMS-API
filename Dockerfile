# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/DCMS.Domain/DCMS.Domain.csproj src/DCMS.Domain/
COPY src/DCMS.Application/DCMS.Application.csproj src/DCMS.Application/
COPY src/DCMS.Infrastructure/DCMS.Infrastructure.csproj src/DCMS.Infrastructure/
COPY src/DCMS.Web/DCMS.Web.csproj src/DCMS.Web/

RUN dotnet restore src/DCMS.Web/DCMS.Web.csproj

# Copy all source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/DCMS.Web/DCMS.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "DCMS.Web.dll"]
