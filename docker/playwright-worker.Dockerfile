FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/PlaywrightWorker/JobSearchAi.PlaywrightWorker.csproj", "src/PlaywrightWorker/"]
COPY ["src/Core/Application/JobSearchAi.Core.Application.csproj", "src/Core/Application/"]
COPY ["src/Core/Infrastructure/JobSearchAi.Core.Infrastructure.csproj", "src/Core/Infrastructure/"]
COPY ["src/Core/Domain/JobSearchAi.Core.Domain.csproj", "src/Core/Domain/"]

RUN dotnet restore "src/PlaywrightWorker/JobSearchAi.PlaywrightWorker.csproj"

COPY . .
WORKDIR /src/src/PlaywrightWorker
RUN dotnet publish "JobSearchAi.PlaywrightWorker.csproj" -c Release -o /app/publish /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "JobSearchAi.PlaywrightWorker.dll"]
