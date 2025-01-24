FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["app/Hutch.Relay/Hutch.Relay.csproj", "app/Hutch.Relay/"]
RUN dotnet restore "app/Hutch.Relay/Hutch.Relay.csproj"
COPY . .
WORKDIR "/src/app/Hutch.Relay"
RUN dotnet build "Hutch.Relay.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hutch.Relay.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hutch.Relay.dll"]
