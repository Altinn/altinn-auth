FROM mcr.microsoft.com/dotnet/sdk:6.0.400-alpine3.16 AS build
WORKDIR /app


COPY src/ResourceRegistry/*.csproj ./src/ResourceRegistry/
COPY src/Altinn.ResourceRegistry.Core/*.csproj ./src/Altinn.Notifications.Core/
COPY src/Altinn.ResourceRegistry.Integrations/*.csproj ./src/Altinn.Notifications.Integrations/
COPY src/Altinn.ResourceRegistry.Persistence/*.csproj ./src/Altinn.Notifications.Persistence/
RUN dotnet restore ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj


# Copy everything else and build
COPY src ./src
RUN dotnet publish -c Release -o out ./src/ResourceRegistry/Altinn.ResourceRegistry.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.8-alpine3.16 AS final
EXPOSE 5100
WORKDIR /app
COPY --from=build-env /app/out ..
COPY src/ResourceRegistry/Migration ./Migration

# setup the user and group
# the user will have no password, using shell /bin/false and using the group dotnet
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet
# update permissions of files if neccessary before becoming dotnet user
USER dotnet
RUN mkdir /tmp/logtelemetry
ENTRYPOINT ["dotnet", "Altinn.ResourceRegistry.dll"]
