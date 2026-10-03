FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Helpdesk.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
COPY docker-entrypoint.sh /docker-entrypoint.sh
COPY rewrite-appsettings.sh /rewrite-appsettings.sh
RUN chmod +x /docker-entrypoint.sh /rewrite-appsettings.sh \
 && mkdir -p /app/uploads \
 && chown -R $APP_UID:$APP_UID /app /docker-entrypoint.sh /rewrite-appsettings.sh
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["/docker-entrypoint.sh"]
