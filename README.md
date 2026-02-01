# Amqp.Net

> **This project is entirely created by AI (Claude Opus 4.5)**

A high-performance AMQP 1.0 message broker implementation in .NET 9, featuring Raft-based distributed clustering for high availability.

## Features

- **AMQP 1.0 Protocol** - Full implementation of the AMQP 1.0 specification
- **High Performance** - Built with `System.IO.Pipelines` for efficient I/O
- **Distributed Clustering** - Raft consensus using DotNext.Net.Cluster for fault tolerance
- **Multiple Exchange Types** - Direct, Topic, Fanout, and Headers exchanges
- **Durable Queues** - Persistent message storage with configurable TTL
- **REST Management API** - Full-featured HTTP API with Swagger/OpenAPI support
- **Modern .NET** - Built on .NET 9 with C# 12+ features

## Project Structure

```
src/
├── Amqp.Net.Protocol/           # AMQP 1.0 protocol implementation
│   ├── Framing/                 # Frame encoding/decoding
│   ├── Performatives/           # AMQP performatives (Open, Begin, Attach, etc.)
│   ├── Messaging/               # Message sections and properties
│   ├── Types/                   # AMQP type system (encoder/decoder)
│   └── Security/                # SASL authentication frames
│
├── Amqp.Net.Broker.Core/        # Core broker functionality
│   ├── Exchanges/               # Exchange implementations (Direct, Topic, Fanout)
│   ├── Queues/                  # Queue implementation with Channel<T>
│   ├── Routing/                 # Message routing and bindings
│   ├── Storage/                 # Message storage abstractions
│   └── Delivery/                # Delivery tracking
│
├── Amqp.Net.Broker.Server/      # AMQP server implementation
│   ├── Transport/               # TCP listener and frame I/O
│   ├── Connections/             # Connection management
│   ├── Sessions/                # Session handling
│   ├── Links/                   # Link (sender/receiver) management
│   └── Handlers/                # Protocol handlers
│
├── Amqp.Net.Broker.Cluster/     # Distributed clustering (Raft)
│   ├── Configuration/           # Cluster configuration options
│   ├── Raft/                    # Raft state machine and commands
│   └── ClusteredMessageRouter   # Replicated topology management
│
├── Amqp.Net.Broker.Management/  # REST Management API
│   ├── Controllers/             # API controllers
│   └── Models/                  # DTOs for API
│
└── Amqp.Net.Broker.Host/        # Standalone broker host
```

## Quick Start

### Running the Broker

```bash
cd src/Amqp.Net.Broker.Host
dotnet run
```

The broker will start listening on:

- **AMQP**: `amqp://localhost:5672`
- **Management API**: `http://localhost:15672`

### Using the Management API

```bash
# List all queues
curl http://localhost:15672/api/queues

# Create a queue
curl -X POST http://localhost:15672/api/queues \
  -H "Content-Type: application/json" \
  -d '{"name": "my-queue", "durable": true}'

# Create an exchange
curl -X POST http://localhost:15672/api/exchanges \
  -H "Content-Type: application/json" \
  -d '{"name": "my-exchange", "type": "direct", "durable": true}'

# Bind queue to exchange
curl -X POST http://localhost:15672/api/bindings \
  -H "Content-Type: application/json" \
  -d '{"source": "my-exchange", "destination": "my-queue", "routingKey": "my-key"}'

# Get broker overview
curl http://localhost:15672/api/overview
```

### Swagger UI

Access the interactive API documentation at: `http://localhost:15672/swagger`

## Clustering

Amqp.Net supports distributed clustering using the Raft consensus algorithm (via DotNext.Net.Cluster) for high availability.

### Cluster Configuration

```json
{
    "BrokerCluster": {
        "NodeId": "node1",
        "ListenAddress": "https://localhost:5001",
        "PublicAddress": "https://node1.example.com:5001",
        "SeedNodes": [
            "https://localhost:5001",
            "https://localhost:5002",
            "https://localhost:5003"
        ],
        "DataPath": "./data/raft",
        "ElectionTimeoutLowerMs": 150,
        "ElectionTimeoutUpperMs": 300,
        "HeartbeatIntervalMs": 50,
        "ColdStart": false
    }
}
```

### What Gets Replicated

The following broker topology is replicated across all cluster nodes:

- **Exchanges** - Name, type, durability settings
- **Queues** - Name, options (TTL, max length, dead letter settings)
- **Bindings** - Exchange-to-queue bindings with routing keys

> **Note**: Message data is NOT replicated through Raft. Messages are routed locally on each node. For message replication, consider using queue mirroring (future feature).

### Starting a Cluster

```bash
# Node 1
dotnet run -- --urls=https://localhost:5001

# Node 2
dotnet run -- --urls=https://localhost:5002

# Node 3
dotnet run -- --urls=https://localhost:5003
```

## Management API Endpoints

| Method | Endpoint                      | Description                  |
| ------ | ----------------------------- | ---------------------------- |
| GET    | `/api/overview`               | Broker statistics and health |
| GET    | `/api/overview/health`        | Health check endpoint        |
| GET    | `/api/exchanges`              | List all exchanges           |
| GET    | `/api/exchanges/{name}`       | Get exchange details         |
| POST   | `/api/exchanges`              | Declare an exchange          |
| DELETE | `/api/exchanges/{name}`       | Delete an exchange           |
| GET    | `/api/queues`                 | List all queues              |
| GET    | `/api/queues/{name}`          | Get queue details            |
| POST   | `/api/queues`                 | Declare a queue              |
| DELETE | `/api/queues/{name}`          | Delete a queue               |
| DELETE | `/api/queues/{name}/contents` | Purge queue messages         |
| GET    | `/api/bindings`               | List all bindings            |
| POST   | `/api/bindings`               | Create a binding             |
| DELETE | `/api/bindings`               | Delete a binding             |

## Configuration

### Queue Options

| Option                 | Type      | Description                            |
| ---------------------- | --------- | -------------------------------------- |
| `Durable`              | bool      | Survives broker restart                |
| `Exclusive`            | bool      | Exclusive to declaring connection      |
| `AutoDelete`           | bool      | Deleted when last consumer disconnects |
| `MaxLength`            | int?      | Maximum number of messages             |
| `MaxLengthBytes`       | long?     | Maximum total size in bytes            |
| `MessageTtl`           | TimeSpan? | Default message TTL                    |
| `DeadLetterExchange`   | string?   | DLX for rejected messages              |
| `DeadLetterRoutingKey` | string?   | Routing key for DLX                    |
| `MaxPriority`          | byte      | Maximum priority level (0-255)         |

### Exchange Types

| Type      | Routing Behavior                            |
| --------- | ------------------------------------------- |
| `Direct`  | Exact routing key match                     |
| `Topic`   | Pattern matching with `*` and `#` wildcards |
| `Fanout`  | Broadcast to all bound queues               |
| `Headers` | Match on message headers                    |

## Building

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Build release
dotnet build -c Release
```

## Dependencies

- **.NET 10.0** - Runtime and SDK
- **DotNext.Net.Cluster** - Raft consensus implementation
- **DotNext.AspNetCore.Cluster** - ASP.NET Core integration for Raft
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI support
- **System.IO.Pipelines** - High-performance I/O

## License

MIT License - See [LICENSE](LICENSE) for details.

## Acknowledgments

This project was entirely created by **Claude Opus 4.5** (Anthropic's AI assistant), demonstrating the capability of AI to design and implement complex distributed systems software.

---

_Built with AI, for the future of messaging._

