CREATE TABLE [EmailLogs] (
    [Id] uniqueidentifier NOT NULL,
    [SubscriptionId] uniqueidentifier NOT NULL,
    [RecipientEmail] nvarchar(256) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [SentAt] datetime2 NULL,
    CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id])
);
GO


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


CREATE INDEX [IX_OutboxMessages_Pending] ON [OutboxMessages] ([IsProduced], [CreatedAt]);
GO


