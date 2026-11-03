FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src
COPY global.json Directory.Build.props DualPath.sln ./
COPY src/DualPath.Web/DualPath.Web.csproj src/DualPath.Web/packages.lock.json src/DualPath.Web/
RUN dotnet restore src/DualPath.Web/DualPath.Web.csproj --locked-mode
COPY src/DualPath.Web/ src/DualPath.Web/
RUN dotnet publish src/DualPath.Web/DualPath.Web.csproj -c Release --no-restore -o /out /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14
WORKDIR /app
COPY --from=build --chown=app:app /out ./
RUN mkdir -p /app/App_Data/keys /app/App_Data/private && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet","DualPath.Web.dll"]
