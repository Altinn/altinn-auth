FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:2fe880002c458a6e95a3f8bb38b63c0f2e21ffefcb01c0223c4408cc91ad7d9d AS build
WORKDIR /app
WORKDIR /src
COPY src/ .
WORKDIR /src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:d4bf3d8c8f0236341ddd93d15208152e26bc6dcc9d34c635351a3402c284137f AS final
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
