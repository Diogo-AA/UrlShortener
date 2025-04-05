# UrlShortener

This project is a URL shortener RESTful API implemented in ASP.NET Core.

## Features
- API key and password authentication.
- PostgreSQL database access using ADO.NET.
- Docker containers.
- Redis cache.

## How to build

### Prerequisites
[Docker](https://www.docker.com/) has to be installed.

### Environment file
Create a .env file with the following variables:
  
- `ENVIRONMENT`: Set the environment of ASP.NET Core. It can receive one of the following values:
  - Development
  - Staging
  - Production
 
- `COMPOSE_PROFILES`: Set the Docker Compose profile. There are two profiles:
  - http
  - https
 
- In case you are using the `https` profile you need to set the following variables:
  - `HTTPS_CERTIFICATE_PATH`: Set the local path of the https certificate.
  - `PASSWORD_HTTPS_CERTIFICATE` (optional): Set the password of the https certificate.
  - `DATA_PROTECTION_CERTIFICATE_PATH`: Set the local path of the data protection certificate.
  - `PASSWORD_DATA_PROTECTION_CERTIFICATE` (optional): Set the password of the data protection certificate.

> [!IMPORTANT]
> It's recommended to use separate certificate for HTTPS and data protection.
> It's also recommended to use strong and distinct passwords.
 
- `DOCKERFILE` (optional): Set a custom dockerfile.

- `DB_HOST`, `DB_USER` and `DB_PASSWORD` (optional): Set the database credentials (`postgres` is the default for all three).
- `DB_NAME` (optional): Set the database name (`UrlShortener` is the default name).
- `DB_PORT` (optional): Set the port of the Postgres database (`5432` is the default port)

- `HOSTNAME` (optional): Set a custom hostname in case of deploy to the web. (`localhost:{port}` is the default hostname)
  
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
      - Creates a new user.
      - Receives the following json body parameters:
        - Username
        - Password
      - Response status codes:
        - 200
          - The user was created successfully. The response includes the API key associated for the new user.
        - 400
          - The parameters are not valid.
        - 409
          - Username is already in use.
        - 500
          - Internal server error.
         
    - /update-password [PATCH]
      - Updates the password of an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
        - NewPassword
      - Response status codes:
        - 204
          - The password was updated successfully.
        - 400
          - The new password is not valid.
        - 401
          - Username and password are not valid.
        - 500
          - Internal server error.
         
    - /delete [DELETE]
      - Deletes an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
      - Response status codes:
        - 204
          - The user was deleted successfully.
        - 401
          - Username and password are not valid.
        - 500
          - Internal server error.
          
- /api/api-key
    - /get [POST]
      - Gets the api-key associated with an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
      - Response status codes:
        - 200
          - The API key was retrieved successfully. The response includes the API key.
        - 401
          - Username and password are not valid.
        - 403
          - The API key is expired.
        - 500
          - Internal server error.
         
    - /udpate [PATCH]
      - Updates the API key associated with an existing user.
      - Receives the following json body parameters:
        - Username
        - Password
      - Response status codes:
        - 200
          - The API key was updated successfully. The response includes the updated API key.
        - 401
          - Username and password are not valid.
        - 429
          - The password was changed too recently.
        - 500
          - Internal server error.
         
- /api/url
    - /get&{limit?:int} [GET]
      - Retrieves a list of the user's shortened URLs.
      - It receives an optional query string `limit`. `limit` must be an integer between 1-100. The default is 20.
      - The header X-API-KEY with the associated user API key is expected.
      - Response status codes:
        - 200
          - The URLs were obtained successfully. The response includes a list of all the shortened URLs.
        - 401
          - API key is not valid or X-API-KEY header was not found.
        - 403
          - The API key is expired.
        - 500
          - Internal server error.

    - /get/{shortedUrlId} [GET]
      - Gets the original URL from the shortened url id.
      - The header X-API-KEY with the associated user API key is expected.
      - Response status codes:
        - 200
          - The URL was obtained successfully. The response includes the original URL.
        - 401
          - API key is not valid or X-API-KEY header was not found.
        - 403
          - The API key is expired.
        - 500
          - Internal server error.

    - /create [POST]
      - Creates a new shortened URL.
      - Receives the URL to shorten in the request body.
      - The header X-API-KEY with the associated user API key is expected.
      - Response status codes:
        - 201
          - The URL was successfully created. The response includes the new shortened URL.
        - 400
          - The URL is already shortened.
        - 401
          - API key is not valid or X-API-KEY header was not found.
        - 403
          - The API key is expired.
        - 500
          - Internal server error.

    - /delete [DELETE]
      - Deletes an existing shortened URL.
      - Receives the shortened URL id to delete in the body.
      - The header X-API-KEY with the associated user API key is expected.
      - Response status codes:
        - 204
          - The URL was deleted successfully. The response includes the new shortened URL.
        - 400
          - The shortened URL id is not valid or was not found.
        - 401
          - API key is not valid or X-API-KEY header was not found.
        - 403
          - The API key is expired.
        - 500
          - Internal server error.
        
- /{shortedUrlId} [GET]
    - Redirects to the corresponding url.
