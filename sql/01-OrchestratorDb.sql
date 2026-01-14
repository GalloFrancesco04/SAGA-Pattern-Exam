CREATE TABLE [OutboxMessages] (
    [Id] uniqueidentifier NOT NULL,
    [AggregateId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [IsProduced] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [ProducedAt] datetime2 NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SagaInstances] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [PlanId] nvarchar(50) NOT NULL,
    [TenantName] nvarchar(200) NOT NULL,
    [SubscriptionId] uniqueidentifier NULL,
    [TenantId] uniqueidentifier NULL,
    [EmailId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [CurrentStep] nvarchar(50) NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [CompensationNeeded] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CompletedAt] datetime2 NULL,
    CONSTRAINT [PK_SagaInstances] PRIMARY KEY ([Id])
);
GO


CREATE INDEX [IX_OutboxMessages_Pending] ON [OutboxMessages] ([IsProduced], [CreatedAt]);
GO


