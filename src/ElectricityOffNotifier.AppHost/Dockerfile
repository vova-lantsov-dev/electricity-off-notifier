FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/ElectricityOffNotifier.AppHost/ElectricityOffNotifier.AppHost.csproj", "ElectricityOffNotifier.AppHost/"]
COPY ["src/ElectricityOffNotifier.Data/ElectricityOffNotifier.Data.csproj", "ElectricityOffNotifier.Data/"]
RUN dotnet restore "ElectricityOffNotifier.AppHost/ElectricityOffNotifier.AppHost.csproj"
COPY src/ .

WORKDIR "/src/ElectricityOffNotifier.AppHost"
RUN dotnet build "ElectricityOffNotifier.AppHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ElectricityOffNotifier.AppHost.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ElectricityOffNotifier.AppHost.dll"]
