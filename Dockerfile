FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apk add --no-cache \
    libc6-compat \
    libaio

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

RUN dotnet tool install --global dotnet-ef

COPY ["SimapdApi.csproj", "."]
RUN dotnet restore "./SimapdApi.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "./SimapdApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SimapdApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=build /root/.dotnet/tools /usr/local/bin

RUN addgroup -g 1001 -S appuser && \
    adduser -S appuser -G appuser -u 1001 -h /home/appuser && \
    chown -R appuser:appuser /app

USER appuser
ENV PATH="$PATH:/usr/local/bin"

ENTRYPOINT ["sh", "-c", "dotnet-ef database update --no-build --verbose && dotnet SimapdApi.dll"]