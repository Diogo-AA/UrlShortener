# UrlShortener

This project it's a url shortener REST API implemented in ASP.NET Core.

## Features
- API key and password authentication.
- PostgreSQL database using ADO.NET.
- Docker containers.
- Redis cache.

## How to build

### Prerequisites
[Docker](https://www.docker.com/) has to be installed.

### Environment file
Create a .env file with the following variables:
- DB_HOST
- DB_NAME
- DB_USER
- DB_PASSWORD
- DB_PORT (5432 is the default Postgre port)
- ENVIRONMENT: Development/Staging/Production
- COMPOSE_PROFILES: http/https
- In case you are using the https profile you need to set the following variables:
  - HTTPS_CERTIFICATE_PATH
  - PASSWORD_HTTPS_CERTIFICATE (optional)
  - DATA_PROTECTION_CERTIFICATE_PATH
  - PASSWORD_DATA_PROTECTION_CERTIFICATE (optional)
  
### Build
> [!NOTE]
> The following command is executed in the project directory.

Build and deploy the containers with the following command:

`docker-compose up --build`

> [!NOTE]
> In case you get the following warning with the Redis container:
>
> `WARNING Memory overcommit must be enabled! Without it, ...`
>
> You can fix this with the following [solution](https://github.com/nextcloud/all-in-one/discussions/1731).

## How to use

This API defines the following endpoints:

- /api/user
    - /create [POST]
      - Creates a new user. Returns the API key associated for the created user.
      - Receives the following json body parameters:
        - Username
        - Password
    - /update-password [PATCH]
      - Updates the password of an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
        - NewPassword
    - /delete [DELETE]
      - Deletes an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
          
- /api/api-key
    - /get [POST]
      - Gets the api-key associated with an existing user.
      - Receives the optional boolean query string parameter UpdateIfExpired.
      - Receives the following json body parameters:
        - Username
        - Password
    - /udpate [PATCH]
      - Updates the api-key associated with an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
       
- /api/url
    - /get [GET]
      - Gets all the shorted urls of the user.
      - The header X-API-KEY with the associated user API key is expected.
    - /create [POST]
      - Creates a new shorted url.
      - Receives the url to shorten in the body.
      - The header X-API-KEY with the associated user API key is expected.
    - /delete [DELETE]
      - Deletes an existing shorted url.
      - Receives the shorted url id to delete in the body.
      - The header X-API-KEY with the associated user API key is expected.
        
- /{shortedUrlId} [GET]
    - Redirects to the corresponding url.
