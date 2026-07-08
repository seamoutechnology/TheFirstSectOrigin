# The First Sect Origin

Source code for The First Sect Origin, a microservices-based card game system. This repository contains the backend services, Unity client, and web admin portal.

## Architecture

* **Backend**: Golang microservices (Gateway, Login, World, Combat, Admin) communicating via gRPC.
* **Client**: Unity (C#) using gRPC/Protobuf.
* **Web Admin**: Next.js / React.
* **Infrastructure**: Docker Compose (PostgreSQL, Redis, RabbitMQ).

## Prerequisites

* Git
* Golang 1.22+
* Unity 6000.0.0f1+
* Node.js v18+
* Docker & Docker Compose

## Installation & Setup

### 1. Clone Repository

```bash
git clone https://github.com/seamoutechnology/TheFirstSectOrigin.git
cd TheFirstSectOrigin
```

### 2. Start Infrastructure

```bash
docker-compose up -d
```

### 3. Run Backend Services

```bash
cd server
go mod tidy
go run ./cmd/gateway
go run ./cmd/login-server
go run ./cmd/world-server
go run ./cmd/combat-server
```

### 4. Run Unity Client

1. Open Unity Hub.
2. Add project from disk and select the `client` directory.
3. Open the Main or Login Scene and press Play.

### 5. Run Web Admin

```bash
cd web
npm install
npm run dev
```
Access via `http://localhost:3000`.
