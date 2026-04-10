FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY SAD.Inscripciones.API/*.csproj ./SAD.Inscripciones.API/
RUN dotnet restore SAD.Inscripciones.API/SAD.Inscripciones.API.csproj

COPY SAD.Inscripciones.API/ ./SAD.Inscripciones.API/
RUN dotnet publish SAD.Inscripciones.API/SAD.Inscripciones.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5161

ENV ASPNETCORE_URLS=http://+:5161
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "SAD.Inscripciones.API.dll"]
