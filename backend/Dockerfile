FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, so this layer is cached when only code changes
COPY AdmissionApi.sln ./
COPY src/AdmissionApi/AdmissionApi.csproj src/AdmissionApi/
COPY tests/AdmissionApi.Tests/AdmissionApi.Tests.csproj tests/AdmissionApi.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/AdmissionApi/AdmissionApi.csproj -c Release -o /app/publish --no-restore

# Used by: docker compose --profile test run --rm backend-tests
FROM build AS test
WORKDIR /src
ENTRYPOINT ["dotnet", "test", "--no-restore"]

# Final image (last stage = default target for docker compose up)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AdmissionApi.dll"]
