# syntax=docker/dockerfile:1

##############################################################################
# Stage 1: restore + build the whole solution
##############################################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first so Docker can cache the (slow) restore layer
# as long as no .csproj/.sln changes, even when source files change.
COPY FraudDetection.sln ./
COPY src/FraudDetection.Domain/FraudDetection.Domain.csproj src/FraudDetection.Domain/
COPY src/FraudDetection.Application/FraudDetection.Application.csproj src/FraudDetection.Application/
COPY src/FraudDetection.Infrastructure/FraudDetection.Infrastructure.csproj src/FraudDetection.Infrastructure/
COPY src/FraudDetection.Api/FraudDetection.Api.csproj src/FraudDetection.Api/
COPY tests/FraudDetection.Domain.Tests/FraudDetection.Domain.Tests.csproj tests/FraudDetection.Domain.Tests/
COPY tests/FraudDetection.Application.Tests/FraudDetection.Application.Tests.csproj tests/FraudDetection.Application.Tests/
COPY tests/FraudDetection.IntegrationTests/FraudDetection.IntegrationTests.csproj tests/FraudDetection.IntegrationTests/

RUN dotnet restore FraudDetection.sln

# Now bring in the rest of the source and build.
COPY . .
RUN dotnet build FraudDetection.sln -c Release --no-restore

##############################################################################
# Stage 2: run the tests that don't need external infrastructure
##############################################################################
# Only the Domain and Application unit tests run here: they are pure/mocked and need
# nothing but the .NET runtime, so they can gate the image build itself. The
# integration test suite spins up its own PostgreSQL via Testcontainers, which needs a
# Docker daemon it cannot reach from inside this build — run it directly on the host
# instead (`dotnet test tests/FraudDetection.IntegrationTests`, see README.md).
FROM build AS test
RUN dotnet test tests/FraudDetection.Domain.Tests/FraudDetection.Domain.Tests.csproj -c Release --no-build --logger "console;verbosity=normal" \
 && dotnet test tests/FraudDetection.Application.Tests/FraudDetection.Application.Tests.csproj -c Release --no-build --logger "console;verbosity=normal"

##############################################################################
# Stage 3: publish the API
##############################################################################
# Depending on the `test` stage means `docker build` will not produce an image at all
# unless the unit tests above passed.
FROM test AS publish
RUN dotnet publish src/FraudDetection.Api/FraudDetection.Api.csproj -c Release --no-build -o /app/publish

##############################################################################
# Stage 4: final runtime image
##############################################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

RUN addgroup --system --gid 1000 appgroup \
 && adduser --system --uid 1000 --ingroup appgroup appuser
USER appuser

COPY --from=publish /app/publish .

EXPOSE 8080

# The API applies any pending EF Core migrations on startup (see Program.cs), which
# creates the database schema automatically the first time it runs against a fresh
# PostgreSQL instance — no separate migration step is needed.
ENTRYPOINT ["dotnet", "FraudDetection.Api.dll"]
