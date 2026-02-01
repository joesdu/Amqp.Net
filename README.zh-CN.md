# Amqp.Net

> **本项目完全由 AI (Claude Opus 4.5) 创建**

一个基于 .NET 9 的高性能 AMQP 1.0 消息代理实现，采用 Raft 分布式集群实现高可用。

## 功能特性

- **AMQP 1.0 协议** - 完整实现 AMQP 1.0 规范
- **高性能** - 使用 `System.IO.Pipelines` 实现高效 I/O
- **分布式集群** - 基于 DotNext.Net.Cluster 的 Raft 共识算法实现容错
- **多种交换机类型** - 支持 Direct、Topic、Fanout 和 Headers 交换机
- **持久化队列** - 支持消息持久化存储和可配置的 TTL
- **REST 管理 API** - 完整的 HTTP API，支持 Swagger/OpenAPI
- **现代 .NET** - 基于 .NET 9 和 C# 12+ 特性构建

## 项目结构

```
src/
├── Amqp.Net.Protocol/           # AMQP 1.0 协议实现
│   ├── Framing/                 # 帧编解码
│   ├── Performatives/           # AMQP 执行体 (Open, Begin, Attach 等)
│   ├── Messaging/               # 消息段和属性
│   ├── Types/                   # AMQP 类型系统 (编码器/解码器)
│   └── Security/                # SASL 认证帧
│
├── Amqp.Net.Broker.Core/        # 核心 Broker 功能
│   ├── Exchanges/               # 交换机实现 (Direct, Topic, Fanout)
│   ├── Queues/                  # 基于 Channel<T> 的队列实现
│   ├── Routing/                 # 消息路由和绑定
│   ├── Storage/                 # 消息存储抽象
│   └── Delivery/                # 投递追踪
│
├── Amqp.Net.Broker.Server/      # AMQP 服务器实现
│   ├── Transport/               # TCP 监听器和帧 I/O
│   ├── Connections/             # 连接管理
│   ├── Sessions/                # 会话处理
│   ├── Links/                   # 链接 (发送者/接收者) 管理
│   └── Handlers/                # 协议处理器
│
├── Amqp.Net.Broker.Cluster/     # 分布式集群 (Raft)
│   ├── Configuration/           # 集群配置选项
│   ├── Raft/                    # Raft 状态机和命令
│   └── ClusteredMessageRouter   # 复制的拓扑管理
│
├── Amqp.Net.Broker.Management/  # REST 管理 API
│   ├── Controllers/             # API 控制器
│   └── Models/                  # API 数据传输对象
│
└── Amqp.Net.Broker.Host/        # 独立 Broker 主机
```

## 快速开始

### 运行 Broker

```bash
cd src/Amqp.Net.Broker.Host
dotnet run
```

Broker 将在以下地址监听：
- **AMQP**: `amqp://localhost:5672`
- **管理 API**: `http://localhost:15672`

### 使用管理 API

```bash
# 列出所有队列
curl http://localhost:15672/api/queues

# 创建队列
curl -X POST http://localhost:15672/api/queues \
  -H "Content-Type: application/json" \
  -d '{"name": "my-queue", "durable": true}'

# 创建交换机
curl -X POST http://localhost:15672/api/exchanges \
  -H "Content-Type: application/json" \
  -d '{"name": "my-exchange", "type": "direct", "durable": true}'

# 绑定队列到交换机
curl -X POST http://localhost:15672/api/bindings \
  -H "Content-Type: application/json" \
  -d '{"source": "my-exchange", "destination": "my-queue", "routingKey": "my-key"}'

# 获取 Broker 概览
curl http://localhost:15672/api/overview
```

### Swagger UI

访问交互式 API 文档：`http://localhost:15672/swagger`

## 集群

Amqp.Net 支持使用 Raft 共识算法（通过 DotNext.Net.Cluster）实现分布式集群，提供高可用性。

### 集群配置

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

### 复制内容

以下 Broker 拓扑会在所有集群节点间复制：
- **交换机** - 名称、类型、持久化设置
- **队列** - 名称、选项（TTL、最大长度、死信设置）
- **绑定** - 交换机到队列的绑定及路由键

> **注意**：消息数据不通过 Raft 复制。消息在每个节点本地路由。如需消息复制，请考虑使用队列镜像（未来功能）。

### 启动集群

```bash
# 节点 1
dotnet run -- --urls=https://localhost:5001

# 节点 2
dotnet run -- --urls=https://localhost:5002

# 节点 3
dotnet run -- --urls=https://localhost:5003
```

## 管理 API 端点

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/overview` | Broker 统计和健康状态 |
| GET | `/api/overview/health` | 健康检查端点 |
| GET | `/api/exchanges` | 列出所有交换机 |
| GET | `/api/exchanges/{name}` | 获取交换机详情 |
| POST | `/api/exchanges` | 声明交换机 |
| DELETE | `/api/exchanges/{name}` | 删除交换机 |
| GET | `/api/queues` | 列出所有队列 |
| GET | `/api/queues/{name}` | 获取队列详情 |
| POST | `/api/queues` | 声明队列 |
| DELETE | `/api/queues/{name}` | 删除队列 |
| DELETE | `/api/queues/{name}/contents` | 清空队列消息 |
| GET | `/api/bindings` | 列出所有绑定 |
| POST | `/api/bindings` | 创建绑定 |
| DELETE | `/api/bindings` | 删除绑定 |

## 配置

### 队列选项

| 选项 | 类型 | 描述 |
|------|------|------|
| `Durable` | bool | 在 Broker 重启后保留 |
| `Exclusive` | bool | 仅限声明连接使用 |
| `AutoDelete` | bool | 最后一个消费者断开时删除 |
| `MaxLength` | int? | 最大消息数量 |
| `MaxLengthBytes` | long? | 最大总字节数 |
| `MessageTtl` | TimeSpan? | 默认消息 TTL |
| `DeadLetterExchange` | string? | 拒绝消息的死信交换机 |
| `DeadLetterRoutingKey` | string? | 死信路由键 |
| `MaxPriority` | byte | 最大优先级 (0-255) |

### 交换机类型

| 类型 | 路由行为 |
|------|----------|
| `Direct` | 精确匹配路由键 |
| `Topic` | 使用 `*` 和 `#` 通配符的模式匹配 |
| `Fanout` | 广播到所有绑定的队列 |
| `Headers` | 基于消息头匹配 |

## 构建

```bash
# 构建所有项目
dotnet build

# 运行测试
dotnet test

# 构建发布版本
dotnet build -c Release
```

## 依赖项

- **.NET 9.0** - 运行时和 SDK
- **DotNext.Net.Cluster** - Raft 共识实现
- **DotNext.AspNetCore.Cluster** - Raft 的 ASP.NET Core 集成
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI 支持
- **System.IO.Pipelines** - 高性能 I/O

## 许可证

MIT 许可证 - 详见 [LICENSE](LICENSE)

## 致谢

本项目完全由 **Claude Opus 4.5**（Anthropic 的 AI 助手）创建，展示了 AI 设计和实现复杂分布式系统软件的能力。

---

*由 AI 构建，为消息传递的未来而生。*
