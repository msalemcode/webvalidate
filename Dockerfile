### build the app
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

### Optional: Set Proxy Variables
# ENV http_proxy {value}
# ENV https_proxy {value}
# ENV HTTP_PROXY {value}
# ENV HTTPS_PROXY {value}
# ENV no_proxy {value}
# ENV NO_PROXY {value}

# Copy the source
COPY src /src

### Run the unit tests
WORKDIR /src/unit-tests
RUN dotnet test --logger:trx

### Build the release app
WORKDIR /src/app
RUN dotnet publish -c Release -o /app

    
###########################################################


### build the runtime container
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

# create a user
RUN groupadd -g 4120 webv && \
    useradd -r -u 4120 -g webv webv

# run as the webv user
USER webv

WORKDIR /app
COPY --from=build /app .

ENTRYPOINT [ "dotnet",  "webvalidate.dll" ]
