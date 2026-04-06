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

