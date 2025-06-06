ARG DOTNET_RUNTIME=mcr.microsoft.com/dotnet/aspnet:9.0-alpine
ARG DOTNET_SDK=mcr.microsoft.com/dotnet/sdk:9.0-alpine

FROM ${DOTNET_RUNTIME} AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apk add --no-cache \
    libc6-compat \
    libaio

FROM ${DOTNET_SDK} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["SimapdApi.csproj", "."]
RUN dotnet restore "./SimapdApi.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "./SimapdApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SimapdApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM build as migrations
RUN dotnet tool install --version 9.0.0 --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT dotnet-ef database update --project . --startup-project .

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN addgroup -g 1001 -S appuser && \
    adduser -S appuser -G appuser -u 1001 && \
    chown -R appuser:appuser /app

USER appuser

ENTRYPOINT ["dotnet", "SimapdApi.dll"]