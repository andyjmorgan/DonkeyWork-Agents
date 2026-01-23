# Credentials project

This project provides a secure and efficient way to manage user credentials for applications. It includes features for storing, retrieving, and validating credentials, ensuring that sensitive information is handled with care.

THe persistence should be postgres database with column level encryption.

Credentials will be of three types:

External entity Api keys
- stored with a known enum type (e.g., stripe, sendgrid, etc)
- the actual api key value
- stored as a dictionary for future extensibility
  - Username
  - Password
  - ApiKey
  - etc. Opinionated, known fields per type.
External OAuth tokens
- the access token and refresh token
- the scopes granted
- the expiry time
- the provider type (google, microsoft, etc)
- the id of user provided
- worker service (hosted service in Api) to refresh tokens before expiry 

Internal, user api keys they can create for accessing their own data.

OAUTH:

External users should be able to add their own oauth provider.
We'll store the client id, client secret and redirect url for each provider.
These should be strongly typed (microsoft, google for now)

persistence projects should include the entities, entity framework contexts, repositories

Repositories should take the values from contracts and convert to entities and vice versa.

