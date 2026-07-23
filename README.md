# .NET Microservices with RabbitMQ & MongoDB

A robust, production-ready event-driven microservices architecture built with **.NET 8**, **RabbitMQ**, and **MongoDB**, fully containerized with **Docker Compose**.

This solution demonstrates asynchronous communication between decoupled microservices using native messaging (`RabbitMQ.Client`), event handling, and MongoDB document persistence.

---

## 🏗️ Architecture Overview

```text
               +-------------------------------------------------+
               |              RabbitMQ Message Broker            |
               |             Exchange: order-exchange            |
               +------------------------+------------------------+
                                        |
                             Publish OrderCreatedEvent
                                        |
                                        v
               +-------------------------------------------------+
               |         inventory-order-created-queue           |
               +------------------------+------------------------+
                                        |
                               Consume Event
                                        |
                                        v
+-------------------------+                         +-------------------------+
|        Order.Api        |                         |      Inventory.Api      |
|  (Port 5001 -> 8080)    |                         |  (Port 5002 -> 8080)    |
+------------+------------+                         +------------+------------+
             |                                                   |
     Inserts Order                                   Updates Stock & Order Logs
             |                                                   |
             v                                                   v
+-------------------------+                         +-------------------------+
|   MongoDB (OrdersDb)    |                         |  MongoDB (InventoryDb)  |
|   Collection: Orders    |                         |  Collections:           |
+-------------------------+                         |   • Inventory           |
                                                    |   • OrderLogs           |
                                                    +-------------------------+
```

---

## 🚀 Microservices Breakdown

### 1. **Order.Api** (`http://localhost:5001`)
* **Role:** Serves as the order entry point.
* **Responsibilities:**
  * Receives HTTP POST requests to create new orders.
  * Persists order records in **MongoDB** (`OrdersDb.Orders`).
  * Publishes an `OrderCreatedMessage` event to the `order-exchange` in RabbitMQ.

### 2. **Inventory.Api** (`http://localhost:5002`)
* **Role:** Background consumer worker and inventory manager.
* **Responsibilities:**
  * Hosts an `OrderConsumerWorker` (`BackgroundService`) listening to `inventory-order-created-queue`.
  * Deducts ordered quantities in **MongoDB** (`InventoryDb.Inventory`).
  * Creates an immutable transaction log in **MongoDB** (`InventoryDb.OrderLogs`).

---

## 🛠️ Tech Stack & Key Libraries

* **Framework:** .NET 8 Web API
* **Messaging:** `RabbitMQ.Client` (v7+ Async Eventing Driver)
* **Database:** MongoDB (`MongoDB.Driver`)
* **Containerization:** Docker & Docker Compose
* **API Documentation:** Swagger / OpenAPI

---

## 📁 Repository Structure

```text
.
├── docker-compose.yml
├── .gitignore
├── README.md
├── Order.Api/
│   ├── Contracts/
│   │   └── OrderCreatedEvent.cs
│   ├── Models/
│   │   └── OrderItem.cs
│   ├── Services/
│   │   └── RabbitMqPublisher.cs
│   ├── Dockerfile
│   └── Program.cs
└── Inventory.Api/
    ├── Contracts/
    │   └── OrderCreatedMessage.cs
    ├── Models/
    │   ├── InventoryItem.cs
    │   └── OrderLog.cs
    ├── Services/
    │   └── OrderConsumerWorker.cs
    ├── Dockerfile
    └── Program.cs
```

---

## 🚦 Getting Started

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional, for local development outside containers).

---

## 🐳 Running with Docker Compose

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/rabbitmq-mongodb-microservices.git
   cd rabbitmq-mongodb-microservices
   ```

2. **Build and launch containers:**
   ```bash
   docker compose up -d --build
   ```

3. **Verify running containers:**
   ```bash
   docker ps
   ```

   You should see 4 active containers:
   * `order-api` on port `5001`
   * `inventory-api` on port `5002`
   * `rabbitmq` on ports `5672` (AMQP) & `15672` (Management Dashboard)
   * `mongodb` on port `27017`

---

## 🧪 Testing the Workflow

### 1. Open Swagger UIs
* **Order API:** [http://localhost:5001/swagger](http://localhost:5001/swagger)
* **Inventory API:** [http://localhost:5002/swagger](http://localhost:5002/swagger)
* **RabbitMQ Dashboard:** [http://localhost:15672](http://localhost:15672) *(Credentials: `guest` / `guest`)*

---

### 2. Create an Order
Send a **POST** request to `http://localhost:5001/api/orders`:

```json
{
  "productId": "PRO-0001",
  "quantity": 15,
  "totalAmount": 150.00
}
```

---

### 3. Check Real-time Consumer Logs
Follow the background worker logs in `inventory-api`:

```bash
docker logs -f inventory-api
```

**Expected Log Output:**
```text
info: Inventory.Api.Services.OrderConsumerWorker[0]
      Received Event in Inventory API: {"OrderId":"66927a42b10a...","ProductId":"PRO-0001","Quantity":15,"TotalAmount":150,"CreatedAt":"2026-07-22T18:00:00Z"}
info: Inventory.Api.Services.OrderConsumerWorker[0]
      Updated MongoDB Inventory for Product PRO-0001. Deducted: 15
info: Inventory.Api.Services.OrderConsumerWorker[0]
      Deducted inventory & logged Order 66927a42b10a... for Product PRO-0001
```

---

## 🧹 Cleanup

To stop and remove containers and volumes:

```bash
docker compose down -v
```

---

## 📜 License
This project is licensed under the MIT License.
