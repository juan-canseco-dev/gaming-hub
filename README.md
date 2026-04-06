# GameHub

GameHub is a **real-time multi-channel chat application** built with **ASP.NET Core**, **Blazor WebAssembly**, **MudBlazor**, and **SignalR**, following **Clean Architecture** principles.

This project showcases modern full-stack .NET development with a strong focus on **scalable backend design**, **real-time communication**, and a polished **single-page chat experience**.

---

## 🚀 Features

- Clean Architecture and separation of concerns
- CQRS-style application layer organization
- JWT-based authentication
- Real-time messaging with SignalR
- Cursor-based pagination for efficient message loading
- Infinite scroll experience in Blazor WebAssembly
- Responsive UI built with MudBlazor
- Scalable messaging infrastructure with MassTransit + RabbitMQ
- Redis backplane support for SignalR scale-out
- Unit and integration testing with Testcontainers

---

## 🧱 Tech Stack

### Backend
- ASP.NET Core Web API (.NET 9)
- Carter
- MediatR
- FluentValidation
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- SignalR
- Redis
- MassTransit
- RabbitMQ

### Frontend
- Blazor WebAssembly (.NET 9)
- MudBlazor
- SignalR Client
- Infinite scroll for chat messages and participants
- Local storage for auth persistence

### Testing
- xUnit
- FluentAssertions
- Moq
- MockQueryable
- Microsoft.AspNetCore.Mvc.Testing
- Testcontainers
- Respawn

---
## 🧠 Architecture Overview

GameHub follows a **Clean Architecture** approach, separating concerns across distinct layers and enabling scalability, testability, and maintainability.

### High-Level Design

- The **frontend** is a Single Page Application (SPA) built with **Blazor WebAssembly**
- It communicates with the backend through a **RESTful API**
- The backend is designed using **CQRS** and handles operations via **MediatR**
- Real-time updates are delivered using **SignalR**

---

### 🔄 Request & Messaging Flow

1. The client (Blazor SPA) sends HTTP requests to the **ASP.NET Core Web API**
2. Requests are handled using **Carter endpoints** and delegated to the application layer via **MediatR**
3. Commands trigger domain changes and generate **integration events**
4. Events are stored using the **Outbox Pattern** within the same database transaction
5. A background process publishes events to **RabbitMQ** via **MassTransit**
6. Consumers process events and use the **Inbox Pattern** to ensure idempotency
7. Processed events trigger **SignalR notifications** to connected clients

---

### 📦 Messaging Reliability (Outbox & Inbox)

To guarantee **reliable event delivery** and avoid data inconsistencies, GameHub implements:

#### Outbox Pattern
- Events are persisted in the **same transaction** as domain changes
- Prevents message loss if the system crashes after saving data but before publishing
- A background dispatcher publishes events to RabbitMQ

#### Inbox Pattern
- Ensures **idempotent message processing**
- Prevents duplicate handling of the same event
- Each consumed message is tracked and processed only once

---

### ⚙️ Infrastructure Components

- **SQL Server**  
  Primary relational database used with **Entity Framework Core**

- **Redis**  
  Used as a **SignalR backplane** to support horizontal scaling and distributed real-time messaging

- **RabbitMQ + MassTransit**  
  Enables **asynchronous, event-driven communication** between components

---

### 🧩 Architecture Layers

```text
Presentation (Blazor WebAssembly)
        ↓
Web API (Carter Endpoints)
        ↓
Application Layer (CQRS + MediatR)
        ↓
Domain Layer (Entities, Aggregates, Business Rules)
        ↓
Infrastructure Layer (EF Core, Identity, Messaging, SignalR)
```

## 🏗️ Solution Structure

```text
GameHub.sln
│
├── apps
│   ├── GameHub.WebAPI
│   └── GameHub.Web.UI
│
├── src
│   ├── GameHub.Domain
│   ├── GameHub.Application
│   ├── GameHub.Infrastructure
│   ├── GameHub.Contracts
│   ├── GameHub.Abstractions
│   └── GameHub.EventBus.Contracts
│
└── tests
    ├── GameHub.Domain.UnitTests
    ├── GameHub.Application.UnitTests
    └── GameHub.WebAPI.IntegrationTests
```
---
## ▶️ Running the Project

GameHub can be run in two ways:

- **Without Docker** for local development
- **With Docker** using `docker-compose`

---

### 🖥️ Run Without Docker

For local development, update the backend configuration file:

`apps/GameHub.WebAPI/appsettings.Development.json`

Replace the values with your own local settings:

```json
{
  "ConnectionStrings": {
    "ConnectionString": "YOUR_SQL_SERVER_CONNECTION_STRING",
    "Redis": "YOUR_REDIS_CONNECTION_STRING"
  },
  "Jwt": {
    "SecretKey": "YOUR_JWT_SECRET_KEY",
    "Issuer": "YOUR_API_BASE_URL",
    "Audience": "YOUR_API_BASE_URL"
  },
  "EventBusSettings": {
    "Host": "YOUR_RABBITMQ_HOST",
    "Username": "YOUR_RABBITMQ_USERNAME",
    "Password": "YOUR_RABBITMQ_PASSWORD"
  },
  "Cors": {
    "PolicyName": "YOUR_CORS_POLICY_NAME",
    "AllowedOrigins": [
      "YOUR_BLAZOR_WEBASSEMBLY_URL"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "MassTransit": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```
Then update the frontend configuration file:

`apps/GameHub.Web.UI/wwwroot/appsettings.json`
```json
{
  "ApiSettings": {
    "BaseUrl": "YOUR_LOCAL_API_URL/api/",
    "BaseHubUrl": "YOUR_LOCAL_API_URL/hubs/"
  }
}
```
Make sure the API and hub URLs match your local backend URL:

### 🐳 Run With Docker
To run the project with Docker, create a .env file in the root of the project and replace the values with your own settings:
```text
WEBAPI_ENVIRONMENT=YOUR_WEBAPI_ENVIRONMENT
WEBAPI_HTTP_PORT=YOUR_WEBAPI_CONTAINER_PORT
WEBAPI_HOST_PORT=YOUR_WEBAPI_HOST_PORT

DB_CONNECTION_STRING=YOUR_DATABASE_CONNECTION_STRING
REDIS_CONNECTION_STRING=YOUR_REDIS_CONNECTION_STRING

JWT_SECRET_KEY=YOUR_JWT_SECRET_KEY
JWT_ISSUER=YOUR_JWT_ISSUER
JWT_AUDIENCE=YOUR_JWT_AUDIENCE

RABBITMQ_HOST=YOUR_RABBITMQ_HOST
RABBITMQ_USERNAME=YOUR_RABBITMQ_USERNAME
RABBITMQ_PASSWORD=YOUR_RABBITMQ_PASSWORD

CORS_POLICY_NAME=YOUR_CORS_POLICY_NAME
CORS_ALLOWED_ORIGIN_0=YOUR_WEB_UI_URL

WEBUI_ENVIRONMENT=YOUR_WEBUI_ENVIRONMENT
WEBUI_HOST_PORT=YOUR_WEBUI_HOST_PORT
WEBUI_CONTAINER_PORT=YOUR_WEBUI_CONTAINER_PORT

SQLSERVER_ACCEPT_EULA=Y
SQLSERVER_USER=YOUR_SQLSERVER_USER
SQLSERVER_SA_PASSWORD=YOUR_SQLSERVER_PASSWORD
SQLSERVER_HOST_PORT=YOUR_SQLSERVER_HOST_PORT
SQLSERVER_CONTAINER_PORT=YOUR_SQLSERVER_CONTAINER_PORT
SQLSERVER_HEALTHCHECK_INTERVAL=YOUR_HEALTHCHECK_INTERVAL
SQLSERVER_HEALTHCHECK_TIMEOUT=YOUR_HEALTHCHECK_TIMEOUT
SQLSERVER_HEALTHCHECK_RETRIES=YOUR_HEALTHCHECK_RETRIES
SQLSERVER_HEALTHCHECK_START_PERIOD=YOUR_HEALTHCHECK_START_PERIOD

REDIS_HOST_PORT=YOUR_REDIS_HOST_PORT
REDIS_CONTAINER_PORT=YOUR_REDIS_CONTAINER_PORT

RABBITMQ_AMQP_HOST_PORT=YOUR_RABBITMQ_AMQP_HOST_PORT
RABBITMQ_AMQP_CONTAINER_PORT=YOUR_RABBITMQ_AMQP_CONTAINER_PORT
RABBITMQ_MANAGEMENT_HOST_PORT=YOUR_RABBITMQ_MANAGEMENT_HOST_PORT
RABBITMQ_MANAGEMENT_CONTAINER_PORT=YOUR_RABBITMQ_MANAGEMENT_CONTAINER_PORT
```
Then run the following command from the root of the project:
```bash
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```
Or run it in detached mode:
```bash
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d
```
For Docker development, update the frontend configuration file:

`apps/GameHub.Web.UI/wwwroot/appsettings.Docker.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "YOUR_DOCKER_API_URL/api/",
    "BaseHubUrl": "YOUR_DOCKER_API_URL/hubs/"
  }
}
```
---
## 📸 Screenshots

### Authentication

![GameHub Screenshot 01](assets/images/img01.png)
![GameHub Screenshot 02](assets/images/img02.png)

### Channels and Navigation

![GameHub Screenshot 03](assets/images/img03.png)
![GameHub Screenshot 04](assets/images/img04.png)

### Chat Experience

![GameHub Screenshot 05](assets/images/img05.png)
![GameHub Screenshot 06](assets/images/img06.png)
![GameHub Screenshot 07](assets/images/img07.png)

### Infinite Scroll and Messaging

![GameHub Screenshot 08](assets/images/img08.png)
![GameHub Screenshot 09](assets/images/img09.png)
![GameHub Screenshot 10](assets/images/img010.png)

---
## 🔮 Roadmap

### Improvements
- Create dedicated **builders for domain models** in unit tests to improve readability and maintainability
- Refine the **domain naming across the application** for better consistency and clarity

### Next Features
- Build a **mobile client**
- Add **user presence**
- Add **in-app notifications**
- Add **push notifications for mobile devices**

### Long-Term Goals
- Explore a **microservices-based version** of GameHub
---
## 📚 References
- [Minimal API Vertical Slice Architecture](https://github.com/isaacOjeda/MinimalApiArchitecture) by Issac Ojeda
- [CQRS and MediatR in ASP.NET Core](https://code-maze.com/cqrs-mediatr-in-aspnet-core/) by CodeMaze
- [CQRS Validation Pipeline with MediatR and FluentValidation](https://code-maze.com/cqrs-mediatr-fluentvalidation/) by CodeMaze
- [ASP.NET Core Integration Tests with Test Containers & Postgres](https://www.azureblue.io/asp-net-core-integration-tests-with-test-containers-and-postgres/) by Matthias Güntert
- [Understanding Cursor Pagination and Why It's So Fast (Deep Dive)](https://www.milanjovanovic.tech/blog/understanding-cursor-pagination-and-why-its-so-fast-deep-dive) by Milan Jovanović
- [ASP.NET Core Integration Testing Best Practises](https://antondevtips.com/blog/asp-net-core-integration-testing-best-practises) by Anton Martyniuk
- [How to scale out a SignalR back-end by using Redis](https://sd.blackball.lv/en/articles/read/19361-how-to-scale-out-a-signalr-back-end-by-using-redis) by Fiodar Sazanavets
- [Using sortable UUID / GUIDs in Entity Framework](https://steven-giesel.com/blogPost/d6150b89-a3ef-407e-add2-7afa4a2a8729/using-sortable-uuid-guids-in-entity-framework)
- [Blazor WASM Dockerizing](https://ilovedotnet.org/blogs/blazor-wasm-dockerizing/) by  Abdul Rahman
- [Host and deploy ASP.NET Core Blazor WebAssembly with Nginx](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/nginx?view=aspnetcore-9.0) by Andrii Annenko

