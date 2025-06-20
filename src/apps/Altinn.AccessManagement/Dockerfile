FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:cec8f5d4537ff29112274379401142fa73d97fcc9f174dc1c623c29dcaef24c1 AS build
WORKDIR /app
WORKDIR /src
COPY src/ .
WORKDIR /src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:30fdbd1b5963bba6ed66190d72d877b750d4203a671c9b54592f4551b8c5a087 AS final
EXPOSE 5110
WORKDIR /app
COPY --from=build /app .

# setup the user and group
# the user will have no password, using shell /bin/false and using the group dotnet
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet
# update permissions of files if neccessary before becoming dotnet user
USER dotnet
RUN mkdir /tmp/logtelemetry
ENTRYPOINT ["dotnet", "Altinn.AccessManagement.dll"]
