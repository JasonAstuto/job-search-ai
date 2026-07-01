FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Api/JobSearchAi.Api.csproj", "src/Api/"]
COPY ["src/Core/Application/JobSearchAi.Core.Application.csproj", "src/Core/Application/"]
COPY ["src/Core/Infrastructure/JobSearchAi.Core.Infrastructure.csproj", "src/Core/Infrastructure/"]
COPY ["src/Core/Domain/JobSearchAi.Core.Domain.csproj", "src/Core/Domain/"]

RUN dotnet restore "src/Api/JobSearchAi.Api.csproj"

COPY . .
WORKDIR /src/src/Api
RUN dotnet publish "JobSearchAi.Api.csproj" -c Release -o /app/publish /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "JobSearchAi.Api.dll"]
