FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DummyApp.EmailService.Functions/DummyApp.EmailService.Functions.csproj", "src/DummyApp.EmailService.Functions/"]
RUN dotnet restore "./src/DummyApp.EmailService.Functions/DummyApp.EmailService.Functions.csproj"
COPY . .
WORKDIR "/src/src/DummyApp.EmailService.Functions"
RUN dotnet build "./DummyApp.EmailService.Functions.csproj" -c Release -o /app/build /p:DefineConstants=DEBUG

FROM build AS publish
WORKDIR "/src/src/DummyApp.EmailService.Functions"
RUN dotnet publish "./DummyApp.EmailService.Functions.csproj" -c Release -o /app/publish /p:DefineConstants=DEBUG

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4 AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates apt-transport-https gnupg \
    && wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && rm /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends dotnet-runtime-8.0 \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 80
