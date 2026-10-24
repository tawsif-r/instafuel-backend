FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Instafuel.Api/Instafuel.Api.csproj Instafuel.Api/
RUN dotnet restore Instafuel.Api/Instafuel.Api.csproj

COPY Instafuel.Api/ Instafuel.Api/
WORKDIR /src/Instafuel.Api
RUN dotnet publish Instafuel.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Instafuel.Api.dll"]
