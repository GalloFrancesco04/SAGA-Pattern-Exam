# SAGA Pattern Exam – SaaS Microservices

This project implements the SAGA pattern using 3 main microservices (Billing, Provisioning, Notification) for SaaS subscription management.

## Quick Start

1. Run the containers:

   ```
   cd docker
   docker compose up -d
   ```

   > All databases, tables, and Kafka topics are created automatically.

2. Access the APIs via Swagger:
   - Orchestrator: http://localhost:5013/swagger
   - Billing: http://localhost:5037/swagger
   - Provisioning: http://localhost:5148/swagger
   - Notification: http://localhost:5214/swagger

## Architecture

- **BillingService**: Manages subscription creation and cancellation.
- **ProvisioningService**: Handles tenant creation and deprovisioning.
- **NotificationService**: Sends welcome emails and notifications.
- **Orchestrator**: Coordinates the SAGA and manages compensation logic.
- **Kafka**: Asynchronous event management between services.
- **SQL Server**: Dedicated database for each microservice.

## Main Features

- SAGA pattern with forward and compensating transactions
- Synchronous (HTTP) and asynchronous (Kafka) communication
- Entity Framework Core and migrations
- Full automation via Docker Compose
- CI/CD and Docker images published on GHCR

## Docker Images
- **saas-orchestrator**: Coordinates the SAGA workflow, manages distributed transactions, and handles compensation logic between services.
- **saas-billing**: Manages subscription creation, billing operations, and compensation (cancellation/refund) in case of failures.
- **saas-provisioning**: Handles tenant/resource provisioning, implements retry logic for transient errors, and supports deprovisioning for rollback.
- **saas-notification**: Simulates sending welcome emails and notifications, logs actions for traceability, and emits events for other services.
- **kafka-init**: Initializes Kafka topics required for the SAGA workflow, ensuring all event channels are available before services start.
- **sql-init**: Creates all required databases and applies SQL migrations automatically, so all microservices have their schema ready at startup.

## Additional notes

- The NotificationService does not send real emails: sending is simulated and logged in the database for traceability. No SMTP or external provider is used.
- The SAGA workflow takes about 30 seconds to complete because the ProvisioningService implements retry logic with exponential backoff and artificial delays to simulate real-world cloud provisioning and transient failures. This is intentional to demonstrate the reliability and compensation mechanisms of the SAGA pattern.
- ProvisioningService and NotificationService use background services to process outbox messages and Kafka events, ensuring reliable event delivery and eventual consistency.
- All external integrations (cloud APIs, email, etc.) are simulated for demonstration and testing purposes; no real resources are created or modified.
