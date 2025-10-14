FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:585d4596e86953d93ebef638af2c1c470bad6b29c1d7e6987dbf7010995a9b39 AS build
WORKDIR /app
WORKDIR /src
COPY src/ .
WORKDIR /src/apps/Altinn.Authorization/src/Altinn.Authorization
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:56cbdd25b168276871f8f4916efebde4d10e69250bb6b3e8a4f6f95db8d65ac2 AS final
EXPOSE 5050
WORKDIR /app
COPY --from=build /app .

COPY src/apps/Altinn.Authorization/src/Altinn.Authorization/Migration ./Migration

# setup the user and group
# the user will have no password, using shell /bin/false and using the group dotnet
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet
# update permissions of files if neccessary before becoming dotnet user
USER dotnet
RUN mkdir /tmp/logtelemetry
ENTRYPOINT ["dotnet", "Altinn.Authorization.dll"]
