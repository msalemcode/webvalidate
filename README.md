# Web Validate - A web request validation tool

![License](https://img.shields.io/badge/license-MIT-green.svg)
![Docker Image Build](https://github.com/retaildevcrews/webvalidate/workflows/Docker%20Image%20Build/badge.svg)

Web Validate (WebV) is a web request validation tool that we use to run end-to-end tests and long-running smoke tests.

## WebV Quick Start

WebV is published as a dotnet package and can be installed as a dotnet global tool. WebV can also be run as a docker container from docker hub. If you have dotnet core sdk installed, running as a dotnet global tool is the simplest and fastest way to run WebV.

## Running as a dotnet global tool

Install WebV as a dotnet global tool

```bash

# this allows you to execute WebV from the shell
dotnet tool install -g webvalidate --version 1.0.7.3

```

Run a sample validation test against `microsoft.com`

```bash

# change to a directory with WebV test files in it
cd src/app

# run a test
webv --server https://www.microsoft.com --files msft.json

```

Run more complex tests against ["Helium"](https://github.com/retaildevcrews/helium) hosted at [froyo](https://froyo.azurewebsites.net) by using:

```bash

# baseline tests
webv --server https://froyo.azurewebsites.net --files helium.json

```

Experiment with WebV

```bash

# get help
webv --help

```

## Running from source

```bash

# change to the app directory
cd src/app


```

Run a sample validation test against `microsoft.com`

```bash

# run a test
dotnet run -- --server https://www.microsoft.com --files msft.json

```

Run more complex tests against ["Helium"](https://github.com/retaildevcrews/helium) hosted at [froyo](https://froyo.azurewebsites.net) by using:

```bash

# baseline tests
dotnet run -- --server https://froyo.azurewebsites.net --files helium.json

```

Experiment with WebV

```bash

# get help
dotnet run -- --help

```

## Running as a docker container

Run a sample validation test against `microsoft.com`

```bash

# run the tests from Docker
docker run -it --rm retaildevcrew/webvalidate --server https://www.microsoft.com --files msft.json

# run the tests from Docker using the latest version of WebV
docker run -it --rm retaildevcrew/webvalidate:beta --server https://www.microsoft.com --files msft.json

```

Run more complex tests against ["Helium"](https://github.com/retaildevcrews/helium) hosted at [froyo](https://froyo.azurewebsites.net) by using:

```bash

# baseline tests
docker run -it --rm retaildevcrew/webvalidate --server https://froyo.azurewebsites.net --files helium.json

```

Experiment with WebV

```bash

# get help
docker run -it --rm retaildevcrew/webvalidate --help

```

Use your own test files

```bash

# assuming you want to mount ~/webv to the containers /app/TestFiles
# this will start bash so you can verify the mount worked correctly
docker run -it --rm -v ~/webv:/app/TestFiles --entrypoint bash retaildevcrew/webvalidate

# run a test against a local web server running on port 8080 using ~/webv/myTest.json
docker run -it --rm -v ~/webv:/app/TestFiles --net=host  retaildevcrew/webvalidate --server localhost:8080 --files myTest.json

```

Web Validate uses both environment variables as well as command line options for configuration. Command flags take precedence over environment variables.

Web Validate works in two distinct modes. The default mode processes the input file(s) in sequential order one time and exits. The "run loop" mode runs in a continuous loop until stopped or for the specified duration. Some environment variables and command flags are only valid if run loop is specified and the application will exit and display usage information. Some parameters have different default values depending on the mode of execution.

## Command Line Parameters

- --version
  - other parameters are ignored
  - environment variables are ignored
- -h --help
  - other parameters are ignored
  - environment variables are ignored
- -d --dry-run
  - validate parameters but do not execute tests
- -s --server string
  - base Url (i.e. `https://www.microsoft.com`)
  - required
- -f --files file1 [file2 file3 ...]
  - one or more json test files
  - default baseline.json
  - default location ./TestFiles/
- -l --sleep int
  - number of milliseconds to sleep between requests
  - default 0
- --max-errors int
  - end test after max-errors
  - if --max-errors is exceeded, WebV will exit with non-zero exit code
- -t --timeout int
  - HTTP request timeout in seconds
  - default 30 sec
- --verbose
  - log 200 and 300 results as well as errors
  - default true

### RunLoop Mode Parameters

- -r --runloop
  - runs the test in a continuous loop
- -l --sleep int
  - number of milliseconds to sleep between requests
  - default 1000
- --duration int
  - run test for duration seconds then exit
  - default run until OS signal
- --max-concurrent int
  - max concurrent requests
  - default 100
- --random
  - randomize requests
  - default false
- --verbose
  - log 200 and 300 results as well as errors
  - default false
- --telemetry-name appName
  - App Insights application name
  - default none
  - both telemetry-name and telemetry-key must be specified or omitted
- --telemetry-key key
  - App Insights key
  - default none
  - both telemetry-name and telemetry-key must be specified or omitted

## Environment Variables

- SERVER=string
- FILES=space separated list of string
- SLEEP=int
- TIMEOUT=int
- VERBOSE=bool
- MAX_ERRORS=int

### Additional run Loop environment variables

- RUN_LOOP=bool
- DURATION=int
- MAX_CONCURRENT=int
- RANDOM=bool
- TELEMETRY_NAME=string
- TELEMETRY_KEY=string

## Running as part of an CI-CD pipeline

WebV will return a non-zero exit code (fail) under the following conditions

- Error parsing the test files
- If an exception is thrown during a test
- StatusCode validation fails
- ContentType validation fails
- --max-errors is exceeded
  - To cause the test to fail on any validation error, set --max-errors 0 (default is 10)
- Any validation error on a test that has FailOnValidationError set to true

## Validation Files

Validation files are located in the /app/TestFiles directory and are json files that control the validation tests.

You can mount a local volume into the Docker container at /app/TestFiles to test your files against your server if you don't want to rebuild the container

- HTTP redirects are not followed
- All string comparisons are case sensitive

- Path (required)
  - path to resource (do not include http or dns name)
  - valid: must begin with /
- Verb
  - default: GET
  - valid: HTTP verbs
- FailOnValidationError (optional)
  - If true, any validation error will cause that test to fail
  - default: false
- Validation (optional)
  - if not specified in test file, no validation checks will run
  - StatusCode
    - http status code
    - a validation error will cause the test to fail and return a non-zero error code
    - default: 200
    - valid: 100-599
  - ContentType
    - http Content-Type header
    - a validation error will cause the test to fail and return a non-zero error code
    - default: application/json
    - valid: null or non-empty string
  - Length
    - length of content
      - cannot be combined with MinLength or MaxLength
    - valid: null or >= 0
  - MinLength
    - minimum content length
    - valid: null or >= 0
  - MaxLength
    - maximum content length
    - valid: null or > MinLength
    - valid: if MinLength == null >= 0
  - MaxMilliSeconds
    - maximum duration in ms
    - valid: null or > 0
  - ExactMatch
    - Body exactly matches value
    - valid: non-empty string
  - Contains[string]
    - case sensitive string "contains"
    - string
      - valid: non-empty string
  - JsonArray
    - valid: parses into json array
    - Count
      - exact number of items
      - Valid: >= 0
      - valid: cannot be combined with MinCout or MaxCount
    - MinCount
      - minimum number of items
      - valid: >= 0
        - can be combined with MaxCount
    - MaxCount
      - maximum number of items
      - valid: > MinCount
        - can be comined with MinCount
    - ForEach[JsonObject]
      - checks each json object in the array
    - Objects[]
      - validates object[index]
      - Index
        - Index of object to check
        - valid: >= 0
      - JsonObject
        - JsonObject definition to check
        - valid: JsonObject rules
  - JsonObject[]
    - valid: parses into json object
    - Field
      - name of field
      - valid: non-empty string
    - Value (optional)
      - if not specified, verifies the Field exists in the json document
      - valid: null, number or string
- PerfTarget (optional)
  - Category
    - used to group requests into categories for reporting
    - see [helium](https://github.com/retaildevcrews/helium) examples below
    - valid: non-empty string
  - Targets[3]
    - maximum quartile value in ascending order

## Sample `microsoft.com` validation tests

The msft.json file contains sample validation tests that will will successfully run against the `microsoft.com` endpoint (assuming content hasn't changed)

- note that http status codes are not specified when 200 is expected
- note that ContentType is not specified when the default of application/json is expected

### Redirect from home page

- Note that redirects are not followed

```json

{
  "Url":"/",
  "Validation":
  {
    "Code":302
  }
}

```

### home page (en-us)

```json

{
  "Url":"/en-us",
  "Validation":
  {"
    ContentType":"text/html",
    "Contains":
    [
      { "Value":"<title>Microsoft - Official Home Page</title>" },
      { "Value":"<head data-info=\"{" }
    ]
  }
}

```

### favicon

```json
{
  "Url": "/favicon.ico",
  "Validation":
  {
    "ContentType":"image/x-icon"
  }
}
```

### robots.txt

```json
{
  "Url": "/robots.txt",
  "Validation":
  {
    "ContentType": "text/plain",
    "MinLength": 200,
    "Contains":
    [
      { "Value": "User-agent: *" },
      { "Value": "Disallow: /en-us/windows/si/matrix.html"}
    ]
  }
}
```

## Sample [helium](https://github.com/retaildevcrews/helium) tests

```json

{
    "Url":"/version",
    "Validation":
    {
        "Code":200,
        "ContentType":"text/plain"
    }
}

{
    "Url":"/healthz",
    "PerfTarget":
    {
        "Category":"healthz"
    },
    "Validation":
    {
        "ContentType":"text/plain",
        "ExactMatch":
        {
            "Value":"pass"
        }
    }
}

{
    "Url":"/healthz/ietf",
    "PerfTarget":
    {
        "Category":"healthz"
    },
    "Validation":
    {
        "ContentType":"application/health+json",
        "JsonObject":
        [
          {
            "Field":"status",
            "Value":"pass"
          }
        ]
    }
}

{
    "Url":"/api/actors",
    "PerfTarget":
    {
        "Category":"PagedRead"
    },
    "Validation":
    {
        "JsonArray":
        {
            "Count":100
        }
    }
}

```

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [Microsoft Contributor License Agreement](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## CI-CD

This repo uses [GitHub Actions](/.github/workflows/dockerCI.yml) for Continuous Integration.

- CI supports pushing to Azure Container Registry or DockerHub
- The action is setup to execute on a PR or commit to ```master```
  - The action does not run on commits to branches other than ```master```
- The action always publishes an image with the ```:beta``` tag
- If you tag the repo with a version i.e. ```v1.0.8``` the action will also
  - Tag the image with ```:1.0.8```
  - Tag the image with ```:latest```
  - Note that the ```v``` is case sensitive (lower case)

### Pushing to Azure Container Registry

In order to push to ACR, you must create a Service Principal that has push permissions to the ACR and set the following ```secrets``` in your GitHub repo:

- Azure Login Information
  - TENANT
  - SERVICE_PRINCIPAL
  - SERVICE_PRINCIPAL_SECRET

- ACR Information
  - ACR_REG
  - ACR_REPO
  - ACR_IMAGE

### Pushing to DockerHub

In order to push to DockerHub, you must set the following ```secrets``` in your GitHub repo:

- DOCKER_REPO
- DOCKER_USER
- DOCKER_PAT
  - Personal Access Token
