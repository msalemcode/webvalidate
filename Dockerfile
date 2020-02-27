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

### Run end-to-end tests
### run as separate steps to make debugging failures easier
WORKDIR /app
RUN dotnet webvalidate.dll --host bluebell --files dotnet.json baseline.json featured.json genres.json moviesByActorId.json rating.json search.json year.json
RUN dotnet webvalidate.dll --host froyo    --files dotnet.json baseline.json featured.json genres.json moviesByActorId.json rating.json search.json year.json
RUN dotnet webvalidate.dll --host sherbert --files node.json   baseline.json featured.json genres.json moviesByActorId.json rating.json search.json year.json
#RUN dotnet webvalidate.dll --host gelato   --files java.json   baseline.json featured.json genres.json moviesByActorId.json rating.json search.json year.json
RUN dotnet webvalidate.dll --host bluebell --files actorById.json --sleep 50
#RUN dotnet webvalidate.dll --host gelato   --files benchmark.json --sleep 50
RUN dotnet webvalidate.dll --host bluebell --files movieById.json --sleep 50
    
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
