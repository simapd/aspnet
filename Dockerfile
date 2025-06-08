ARG DOTNET_RUNTIME=mcr.microsoft.com/dotnet/aspnet:9.0
ARG DOTNET_SDK=mcr.microsoft.com/dotnet/sdk:9.0

FROM ${DOTNET_SDK} AS build
WORKDIR /source

COPY ["SimapdApi.csproj", "./"]
RUN dotnet restore SimapdApi.csproj

COPY . .
RUN dotnet publish SimapdApi.csproj -c Release -o /app/publish

RUN dotnet tool install --version 9.0.4 --global dotnet-ef

FROM ${DOTNET_SDK} AS final

RUN groupadd -r appuser && useradd -r -g appuser -u 1001 -m appuser

WORKDIR /app

RUN apt-get update && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --version 9.0.4 --global dotnet-ef

COPY --from=build /app/publish .

COPY --from=build /source .

RUN cp -r /root/.dotnet/tools /app/tools

RUN echo '#!/bin/bash\n\
echo "Waiting for database..."\n\
sleep 10\n\
echo "Running database migrations..."\n\
export PATH="$PATH:/app/tools"\n\
/app/tools/dotnet-ef database update --project . --startup-project . || echo "Migration failed, continuing..."\n\
echo "Starting application..."\n\
exec dotnet SimapdApi.dll' > /app/start.sh

RUN chmod +x /app/start.sh && \
    chown -R appuser:appuser /app

USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV DB_CONNECTION_STRING=""

EXPOSE 8080

ENTRYPOINT ["/app/start.sh"]