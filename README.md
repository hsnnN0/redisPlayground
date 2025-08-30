# Redis Playground 🚀

A comprehensive .NET 9 demonstration of Redis operations using clean architecture with Microsoft Aspire.

## 📋 Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)  
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Testing](#testing)
- [Configuration](#configuration)

## Overview

Redis Playground showcases **15 Redis feature groups** through a clean, modular ASP.NET Core API:

### 🏗️ Tech Stack
- **.NET 9** with Minimal APIs
- **Microsoft Aspire** for cloud-native development  
- **StackExchange.Redis** for Redis operations
- **OpenAPI/Swagger** for documentation

### 📁 Project Structure
```
RedisPlayground/
├── RedisPlayground.AppHost/       # Aspire orchestration
├── RedisPlayground.ApiService/    # Main API service
│   ├── Endpoints/                 # 15 feature modules
│   ├── Models/                    # Request/response DTOs
│   ├── Extensions/                # Endpoint registration
│   └── HttpTests/                 # HTTP test collections
├── RedisPlayground.ServiceDefaults/ # Shared configuration
└── RedisPlayground.sln
```

## Architecture

### 🔧 Core Features
| Category | Features | Use Cases |
|----------|----------|-----------|
| **Basic** | Cache, Strings | Session storage, counters |
| **Collections** | Lists, Sets, Sorted Sets, Hashes | Queues, leaderboards, user profiles |
| **Advanced** | Pub/Sub, Streams, Geospatial | Real-time messaging, location services |
| **Modern** | JSON, Bitfields, Analytics | Document storage, bit operations, analytics |
| **Tools** | Locks, Rate Limiting, Dev Tools | Distributed locking, API limiting |

## Quick Start

### Prerequisites
- .NET 9 SDK
- Docker Desktop (for Redis container)

### Running with Aspire (Recommended)
```bash
git clone <repository-url>
cd RedisPlayground
dotnet run --project RedisPlayground.AppHost
```

**Access Points:**
- 🌐 **Aspire Dashboard**: http://localhost:15000  
- 📋 **API Documentation**: Check dashboard for API service URL + `/swagger`
- 🧪 **HTTP Tests**: Use `.http` files in `HttpTests/` folder

### Manual Setup (Alternative)
```bash
# Start Redis manually
docker run -d -p 6379:6379 redis:latest

# Run API service directly  
dotnet run --project RedisPlayground.ApiService
# API available at: http://localhost:5528
```

## API Reference

### 🔧 Core Operations
```http
# Basic Cache (IDistributedCache)
GET    /cache/{key}              # Get cached value
POST   /cache/{key}              # Set with 5min TTL
DELETE /cache/{key}              # Remove from cache

# Strings & Counters  
GET    /strings/{key}            # Get string value
POST   /strings/{key}            # Set string value
POST   /strings/{key}/increment  # Atomic increment

# Collections
GET    /hashes/{key}             # Get hash fields
GET    /lists/{key}              # Get list range  
GET    /sets/{key}               # Get set members
GET    /sorted-sets/{key}/leaderboard # Get rankings
```

### 🚀 Advanced Features
```http
# Real-time & Streaming
POST   /pubsub/publish/{channel} # Publish message
POST   /streams/{stream}/add     # Add stream entry

# Location Services  
POST   /geo/{key}/add            # Add geo location
GET    /geo/{key}/radius         # Find nearby points

# Modern Redis Stack
GET    /json/{key}               # Get JSON document
POST   /json/{key}/path          # JSONPath operations
POST   /analytics/bloom/{key}/add # Bloom filter ops
```

### 🛠️ Developer Tools
```http
# Sample Data & Utilities
POST   /devtools/sample-data/generate # Create test data
DELETE /devtools/keys/pattern         # Bulk key deletion  
GET    /devtools/keys/info            # Database statistics
POST   /devtools/command             # Execute raw Redis commands
```

## Testing

### Quick Testing with HTTP Files
1. **Install REST Client** extension in VS Code
2. **Open** any `.http` file in `HttpTests/` folder  
3. **Click "Send Request"** above HTTP requests
4. **View responses** in adjacent panel

### Generate Sample Data
```bash
curl -X POST "http://localhost:5528/devtools/sample-data/generate" \
  -H "Content-Type: application/json" \
  -d '{"keyPrefix": "demo", "count": 20}'
```

Creates comprehensive test data across all Redis data types.

### Cleanup Test Data  
```bash
curl -X DELETE "http://localhost:5528/devtools/keys/pattern" \
  -H "Content-Type: application/json" \
  -d '{"pattern": "demo:*"}'
```

## Configuration

### Aspire Integration
```csharp
// AppHost.cs - Automatic Redis provisioning
var redis = builder.AddRedis("cache");
var apiService = builder.AddProject<Projects.RedisPlayground_ApiService>("apiservice")
    .WithReference(redis);
```

### Connection String (Manual Setup)
```json
{
  "ConnectionStrings": {
    "cache": "localhost:6379"  
  }
}
```
