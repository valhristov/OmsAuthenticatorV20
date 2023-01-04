# What is OMS Authenticator?

OMS Authenticator is a service application that exposes endpoints allowing another application to obtain OMS and TrueApi tokens. It can also sign values using the configured certificates.

# How does it work?

OMS Authenticator uses the configured certificate(s) to obtain and cache tokens from the configured CRPT GISMT endpoints. The tokens automatically expire after the configured time.

# Configuration

OMS Authenticator can be configured to obtain tokens using different CRPT API endpoints and certificates. A single OMS Authenticator instance can handle both QAS and PROD environments and multiple certificates.

Each configuraton is called "token provider" and has unique key, that is part of the path of each OMS Authenticator endpoint (see Endpoints below).

# Endpoints

- OMS tokens:
    GET /api/v2/<provider>/oms/token
- TrueApi tokens:
    GET /api/v2/<provider>/true/token
- Sign values:
    GET /api/v2/<provider>/sign

There are legacy endpoints for backwards compatibility with older versions of the application:

- OMS tokens:
    GET /api/v1/<provider>/oms/token
- OMS tokens obtained with the first configured token provider (by configuration order)
    GET /oms/token
- Sign values with the first configured token provider (by configuration order)
    GET /api/v1/sign

# How to use?

- Get last valid token - to get the last valid token in the OMS Authenticator cache execute the following request. If a valid token does not exist in cache a new one will be retrieved.
-- omsId and omsConnection are identifiers coming from OMS.
-- token is the token
-- expires is a ISO formatted token expiration date in UTC
-- requestId is a unique token identifier, assigned by the OMS Authenticator.
    GET /api/v2/<provider>/oms/token?omsId=<omsId>&omsConnection=<omsConnection>
    { "token": "xxx", "expires": "date", "requestId": "yyy" }

- Get specific token - to get a specific token 
-- Request
    GET /api/v2/<provider>/oms/token?omsId=<omsId>&omsConnection=<omsConnection>&requestId=<requestId>

- Get new token
    GET /api/v2/<provider>/oms/token?omsId=<omsId>&omsConnection=<omsConnection>&requestId=<new Guid>

# How to handle errors

OMS Authenticator will return HTTP StatusCode 422 when some of the CRPT APIs return errors, or 400 when some of the required parameters were not provided. The response body will contain description of the errors:

    { "errors": [ "", ... ] }
