# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY *.csproj ./

RUN dotnet clean
RUN dotnet restore

# copy everything else and build app
COPY . ./
WORKDIR /source/
RUN dotnet publish UrlShortener.csproj -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "UrlShortener.dll"]