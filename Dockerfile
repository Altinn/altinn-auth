FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app


COPY src/Altinn.ResourceRegistry/*.csproj ./src/Altinn.ResourceRegistry/
COPY src/Altinn.ResourceRegistry.Core/*.csproj ./src/Altinn.Notifications.Core/
COPY src/Altinn.ResourceRegistry.Integration/*.csproj ./src/Altinn.Notifications.Integration/
COPY src/Altinn.ResourceRegistry.Persistence/*.csproj ./src/Altinn.Notifications.Persistence/
RUN dotnet restore ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj


# Copy everything else and build
COPY src ./src
RUN dotnet publish -c Release -o out ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
EXPOSE 5100
WORKDIR /app
COPY --from=build /app/out .

COPY src/Altinn.ResourceRegistry.Persistence/Migration ./Migration

# setup the user and group
# the user will have no password, using shell /bin/false and using the group dotnet
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet
# update permissions of files if neccessary before becoming dotnet user
USER dotnet
RUN mkdir /tmp/logtelemetry
ENTRYPOINT ["dotnet", "Altinn.ResourceRegistry.dll"]
