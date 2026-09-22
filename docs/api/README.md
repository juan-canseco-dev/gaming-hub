# GameHub API

GameHub exposes an ASP.NET Core HTTP API for identity, channels, and chats, plus a SignalR hub for real-time notifications. The HTTP contract is generated with the built-in .NET OpenAPI support and is the authoritative machine-readable description of request and response schemas.

## OpenAPI

Run the API in the `Development` or `Docker` environment and retrieve the OpenAPI 3 document from:

```text
GET /openapi/v1.json
```

For the default local launch profile, the complete URL is `https://localhost:7058/openapi/v1.json`. The document is intentionally not exposed in other environments.

Interactive Swagger UI is available in the same environments at:

```text
https://localhost:7058/swagger
```

Select **Authorize**, enter the JWT returned by the sign-in endpoint, and Swagger UI will add the bearer token to protected requests. The UI uses a relative OpenAPI URL so it also works when GameHub is hosted below a reverse-proxy path base.

## Authentication

Register a user, exchange credentials for a JWT, and send that token with every protected request:

```http
POST /api/identity/auth/register
Content-Type: application/json

{
  "email": "player@example.com",
  "username": "player_one",
  "fullname": "Player One",
  "password": "replace-with-a-strong-password",
  "confirmPassword": "replace-with-a-strong-password"
}
```

Registration returns the new user ID as a JSON string. Sign-in returns an object containing the token:

```http
POST /api/identity/auth
Content-Type: application/json

{
  "email": "player@example.com",
  "password": "replace-with-a-strong-password"
}
```

```json
{
  "token": "<jwt>"
}
```

Use the token with the standard bearer scheme:

```http
Authorization: Bearer <jwt>
```

Authentication establishes the caller's identity. Individual chat operations may also enforce membership or other resource-level rules.

## HTTP conventions

- JSON uses ASP.NET Core web defaults, including camel-case property names.
- Identifiers described as `Guid` are serialized as UUID strings.
- Timestamps are ISO 8601 strings with an offset.
- Expected failures use `application/problem+json` and follow RFC 9457-style Problem Details.
- All requests may include `X-Correlation-ID`. Accepted values contain 1-64 ASCII letters, digits, `.`, `_`, or `-`. The response always echoes the accepted value or a server-generated value in the same header.
- `401 Unauthorized` means that a bearer token is missing, invalid, or expired. `403 Forbidden` means that an authenticated caller does not satisfy an authorization policy.

A domain or application failure includes a stable machine-readable `code`:

```json
{
  "type": "https://api.gamehub.example/problems/not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Chat group '01900000-0000-7000-8000-000000000001' was not found.",
  "instance": "/api/chats/01900000-0000-7000-8000-000000000001",
  "code": "Chat.ChatGroupNotFound",
  "correlationId": "01J8EXAMPLE9YQ8C8T6T1F5X"
}
```

Validation failures use the same base fields and add an `errors` object whose keys are request property names and whose values are arrays of messages. A temporary database failure returns `503 Service Unavailable` and may include `Retry-After: 30`.

## Endpoints

Routes retain the existing singular and plural `chat`/`chats` forms for client compatibility.

| Method | Route | Auth | Success response | Purpose |
| --- | --- | --- | --- | --- |
| `POST` | `/api/identity/auth/register` | Anonymous | `200` + user `Guid` | Register an identity account and profile |
| `POST` | `/api/identity/auth` | Anonymous | `200` + `{ token }` | Sign in and issue a JWT |
| `GET` | `/api/channels` | Bearer | `200` + `ChannelDto[]` | List available channels |
| `POST` | `/api/channels/join` | Bearer | `200` | Join the chat identified by `chatId` in the JSON body |
| `GET` | `/api/chats` | Bearer | `200` + `ChatDto[]` | List the caller's joined chats |
| `GET` | `/api/chats/{chatId}` | Bearer | `200` + `ChatDto` | Get one chat |
| `POST` | `/api/chats/messages` | Bearer | `200` + `MessageDto` | Send a message using `chatId` and `content` in the JSON body |
| `GET` | `/api/chat/messages/{messageId}` | Bearer | `200` + `MessageDto` | Get one message |
| `GET` | `/api/chat/{chatId}/messages` | Bearer | `200` + cursor page of `MessageDto` | List messages with `limit` and optional `cursor` query parameters |
| `GET` | `/api/chat/{chatId}/members` | Bearer | `200` + cursor page of `UserDto` | List participants with `limit` and optional `cursor` query parameters |
| `GET` | `/api/chat/{chatId}/members/count` | Bearer | `200` + integer | Get the participant count |
| `GET` | `/api/chats/{chatId}/messages/unread/count` | Bearer | `200` + integer | Get the caller's unread count for one chat |
| `GET` | `/api/chats/unread-count` | Bearer | `200` + integer | Get the caller's total unread count |
| `POST` | `/api/chats/{chatId}/read` | Bearer | `200` | Mark the chat as read through its latest message |
| `GET` | `/health/live` | Anonymous | `200` or `503` | Check process liveness |
| `GET` | `/health/ready` | Anonymous | `200` or `503` | Check readiness, including SQL Server |

The generated OpenAPI document describes the concrete request fields, response schemas, and failure statuses for each operation.

## Cursor pagination

Message and participant lists return this envelope:

```json
{
  "items": [],
  "next": "<opaque-cursor-or-null>"
}
```

Treat `next` as opaque. When it is non-null, pass it unchanged in the next request's `cursor` query parameter while keeping the same `chatId` and `limit`. A null value means there are no more results. Do not decode, modify, cache indefinitely, or synthesize cursor values in a client.

## SignalR

The authenticated hub endpoint is:

```text
/hubs/chat
```

HTTP clients normally send the bearer token in the `Authorization` header. SignalR transports that cannot set the header may send it as the `access_token` query parameter; the API accepts that parameter only for paths under `/hubs`.

Client-to-server methods:

| Method | Arguments | Purpose |
| --- | --- | --- |
| `JoinChat` | `Guid chatId` | Subscribe the current connection to a chat group |
| `LeaveChat` | `Guid chatId` | Remove the current connection from a chat group |
| `UpdatePresence` | None | Refresh the authenticated user's presence |

Server-to-client events:

| Event | Payload |
| --- | --- |
| `MessageSent` | `MessageNotification` with `chatId` and `message` |
| `UserJoinedChat` | `UserJoinedNotification` with `chatId`, `numberOfParticipants`, and a system `message` |
| `OnUserPresenceUpdated` | `UserPresenceUpdatedNotification` with `presence` and `onlineUsersCount` |

Call the HTTP join operation before subscribing a connection to a chat's real-time group. Clients must tolerate reconnection, repeated notifications, and an HTTP write succeeding before its asynchronous notification arrives.

## Compatibility

Treat routes, JSON property names and nullability, Problem Details codes, cursor semantics, hub method names, and event payloads as public contracts. Coordinate changes with the Blazor client and integration tests. Additive OpenAPI descriptions are safe; changing a route, field, status, or event requires a versioning and migration decision.
