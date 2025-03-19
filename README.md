# UrlShortener

This project it's a url shortener REST API implemented in ASP.NET Core.

## Features
- API key and password authentication
- PostgreSQL database using ADO.NET
- Docker containers
- Redis Cache (Coming soon...)

## How to build

### Prerequisites
[Docker](https://www.docker.com/) has to be installed.

### Environment file
Create a .env file with the following variables:
- DB_HOST
- DB_NAME
- DB_USER
- DB_PASSWORD
- DB_PORT: 5432 is the default Postgre port
- ENVIRONMENT: Development/Staging/Production
- COMPOSE_PROFILES: http/https
  
### Build
> [!NOTE]
> The following commands are executed in the project directory.

Build the images with the following command:

`docker build -t url-shortener .`

### Deploy

Deploy the containers with the following command:

`docker-compose up`

## How to use

This API defines the following endpoints:
- /api/user
    - /create [POST]
    - /update-password [PATCH]
    - /delete [DELETE]
- /api/api-key
    - /get [POST]
    - /udpate [PATCH]
- /api/url
    - /get [GET]
    - /create [POST]
    - /delete [DELETE]
- /{shortedUrlId} [GET]