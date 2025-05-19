FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:2244f80ac7179b0feaf83ffca8fe82d31fbced5b7e353755bf9515a420eba711 AS build
WORKDIR /app
WORKDIR /src
COPY src/ .
WORKDIR /src/apps/Altinn.Authorization/src/Altinn.Authorization
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:30fdbd1b5963bba6ed66190d72d877b750d4203a671c9b54592f4551b8c5a087 AS final
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
