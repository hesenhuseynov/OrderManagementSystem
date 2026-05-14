IF OBJECT_ID('dbo.OutboxEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxEvents
    (
        OutboxEventId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_OutboxEvents PRIMARY KEY,

        EventId UNIQUEIDENTIFIER NOT NULL,

        EventType NVARCHAR(200) NOT NULL,

        AggregateType NVARCHAR(100) NOT NULL,

        AggregateId NVARCHAR(100) NOT NULL,

        Payload NVARCHAR(MAX) NOT NULL,

        OccurredAt DATETIME2(7) NOT NULL
            CONSTRAINT DF_OutboxEvents_OccurredAt DEFAULT SYSUTCDATETIME(),

        ProcessedAt DATETIME2(7) NULL,

        RetryCount INT NOT NULL
            CONSTRAINT DF_OutboxEvents_RetryCount DEFAULT 0,

        LastError NVARCHAR(MAX) NULL,

        LockedAt DATETIME2(7) NULL,

        LockId UNIQUEIDENTIFIER NULL,

        CreatedAt DATETIME2(7) NOT NULL
            CONSTRAINT DF_OutboxEvents_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_OutboxEvents_EventId'
      AND object_id = OBJECT_ID('dbo.OutboxEvents')
)
BEGIN
    CREATE UNIQUE INDEX UX_OutboxEvents_EventId
    ON dbo.OutboxEvents(EventId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboxEvents_Unprocessed'
      AND object_id = OBJECT_ID('dbo.OutboxEvents')
)
BEGIN
    CREATE INDEX IX_OutboxEvents_Unprocessed
    ON dbo.OutboxEvents(ProcessedAt, OccurredAt)
    WHERE ProcessedAt IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboxEvents_Aggregate'
      AND object_id = OBJECT_ID('dbo.OutboxEvents')
)
BEGIN
    CREATE INDEX IX_OutboxEvents_Aggregate
    ON dbo.OutboxEvents(AggregateType, AggregateId);
END;
GO