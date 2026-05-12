FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY ["UsbCopier.Api/UsbCopier.Api.csproj", "UsbCopier.Api/"]
RUN dotnet restore "UsbCopier.Api/UsbCopier.Api.csproj"

# Копируем весь код и собираем
COPY . .
WORKDIR "/src/UsbCopier.Api"
RUN dotnet build "UsbCopier.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UsbCopier.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#qwe
# Railway передаёт порт через переменную PORT
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "UsbCopier.Api.dll"]