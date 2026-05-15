# DistPubSub

A custom distributed Publish-Subscribe framework over raw TCP sockets in .NET 8, plus a standalone task scheduler.

## Project Structure

| Project | Description |
|---------|-------------|
| `lib` | Core library: networking, routing, packet serialization, utilities |
| `Server` | TCP server host listening on port 8080 |
| `Client` | Publisher that sends timestamp data to a path |
| `Client_Sub` | Subscriber that receives published data from a path |
| `DistPubSub` | Simple packet serialization round-trip test |
| `Schedualer` | Standalone task scheduler (no dependency on `lib`) |

## Architecture

### Packet Protocol

The custom binary protocol uses a 20-byte header:

```
[4 bytes] Total packet length (uint32 BE)
[1 byte]  Packet type (EPacketType)
[4 bytes] Key length (uint32 BE)
[4 bytes] Value length (uint32 BE)
[7 bytes] Tail marker "|||||||"
```

**Packet types:** Raw, Name, Path, Type, Sync, Error

**Pub/Sub commands:** Sub, Pub, Uns (unsubscribe), Req, Res, Set, Rem, and various control types (Con/Dis, Hbt, Pig/Png, etc.)

### Message Flow

1. **Server** starts on port 8080, accepts TCP clients
2. **Subscriber** connects and sends a `Sub` command for a path (e.g. `"mess"`)
3. **Publisher** connects and sends `Pub` commands to the same path
4. **Server** routes published messages to all subscribers of that path via a path-based routing tree (`PathingTree`)

### Key Components

- **NetServer** — TCP listener, manages client connections, binds pathed packet type handlers
- **NetClient** — TCP client wrapper with `PublishToPath<T>()`, `SubscribeToPath<T>()`, and write batching (server-side)
- **ClientManager** — Multi-threaded reader/writer/stats threads with `Parallel.ForEach`
- **PathRouting** — Static event dispatcher using `PathingTree<string, Action<NetClient, object>>`
- **PathingTree<K,V>** — Hierarchical tree structure for path-based callback registration and lookup
- **KeyValueStore** — Distributed KV store using Req/Res/Set/Rem packet types with `TaskCompletionSource` for async get
- **BufferLooper** — Circular buffer for handling partial packet reads with dynamic resizing
- **SPacket** — Core packet struct with binary serialization/deserialization

## Tests

Tests are in the `DistPubSub.Tests` project using **xUnit**:

```bash
dotnet test
```

### Unit Tests (`DistPubSub.Tests/`)

- **PacketTests.cs** — Serialization round-trip for `SPacket`, `NamedPacket`, `PathedPacket`, `TypedPacket<T>`, `SerializeUtils`, and `ObjectUtils` helpers
- **RoutingTests.cs** — `PathingTree<K,V>` add/get/clear operations, `PathRouting` invoke/remove
- **BufferTests.cs** — `BufferLooper` write/read/auto-resize behavior

### Integration Tests (executable projects)

### 1. DistPubSub (`DistPubSub/Program.cs`)
A serialization round-trip test:
- Serializes and deserializes a `uint` value (1234)
- Creates a `NamedPacket`, serializes it, deserializes it back, and verifies equality

**Run:** `dotnet run --project DistPubSub/DistPubSub`

### 2. Integration Test (Server + Client + Client_Sub)
End-to-end Pub/Sub message flow test:

1. **Start the server:**
   ```
   dotnet run --project Server
   ```
   Listens on port 8080, accepts clients, routes Pub/Sub messages.

2. **Start the subscriber:**
   ```
   dotnet run --project Client_Sub
   ```
   Connects to the server, subscribes to path `"mess"`, and receives timestamp messages.

3. **Start the publisher:**
   ```
   dotnet run --project Client
   ```
   Connects to the server, continuously publishes `DateTime.Now.TotalMicroseconds` to path `"mess"`.

The server routes published messages to the subscriber, which receives and processes them.

### 3. Schedualer (`Schedualer/Program.cs`)
Independent task scheduler test:
- Reads a `Config.json` file with scheduling entries
- Supports Relative (intervals) and Absolute (days + times) scheduling
- Executes commands at specified times with optional stdout/stderr capture

**Run:** `dotnet run --project Schedualer [config-path]`

### Test data
`lib/ComplexTest.cs` provides a sample serializable struct used for testing type-based packet serialization.

## Running the solution

```bash
# Build all projects
dotnet build

# Run individual projects
dotnet run --project Server
dotnet run --project Client
dotnet run --project Client_Sub
dotnet run --project DistPubSub/DistPubSub
dotnet run --project Schedualer
```

## Notes

- Server synchronization (`SyncPacketRouter`) is stubbed with `NotImplementedException`
- Heartbeat tracking exists but no timeout/cleanup logic is implemented
- Multi-server peering via `Bro` packet type is partially implemented (commented out)
- The Schedualer project is independent and does not reference the `lib` library
