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

FROM buildbase as migrations
RUN dotnet tool install --version 9.0.4 --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT dotnet-ef database update --project . --startup-project .