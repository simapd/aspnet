ARG DOTNET_RUNTIME=mcr.microsoft.com/dotnet/aspnet:9.0
ARG DOTNET_SDK=mcr.microsoft.com/dotnet/sdk:9.0

FROM ${DOTNET_RUNTIME} AS base
ENV ASPNETCORE_URLS=http://+:8080
WORKDIR /home/app
EXPOSE 8080

FROM ${DOTNET_SDK} AS buildbase
WORKDIR /source

COPY ["SimapdApi.csproj", "./"]
RUN dotnet restore SimapdApi.csproj

COPY . .

FROM buildbase AS publish
RUN dotnet publish SimapdApi.csproj -c Release -o /app/publish

FROM buildbase as migrations
RUN dotnet tool install --version 9.0.4 --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT dotnet-ef database update --project . --startup-project .

FROM base AS final

RUN apt-get update && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
ENV PATH="$PATH:/root/.dotnet:/root/.dotnet/tools"
RUN /root/.dotnet/dotnet tool install --version 9.0.4 --global dotnet-ef

COPY --from=publish /app/publish .
COPY --from=buildbase /source .

RUN useradd --create-home --uid 1001 appuser && \
    chown -R appuser:appuser /home/app

ENV DB_CONNECTION_STRING ''

RUN echo '#!/bin/bash\n\
echo "Running database migrations..."\n\
dotnet-ef database update --no-build || echo "Migration failed, continuing..."\n\
echo "Starting application..."\n\
exec dotnet SimapdApi.dll' > /home/app/start.sh

RUN chmod +x /home/app/start.sh && \
    chown appuser:appuser /home/app/start.sh

USER appuser

ENTRYPOINT ["/home/app/start.sh"]