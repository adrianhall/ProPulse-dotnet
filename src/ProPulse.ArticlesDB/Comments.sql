-- Comments table
CREATE TABLE [dbo].[Comments] (
    -- Primary key
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,

    -- Metadata
    [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [CreatedBy] VARCHAR(64) NULL,
    [UpdatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [UpdatedBy] VARCHAR(64) NULL,
    [Version] ROWVERSION NOT NULL,

    -- Content
    [Content] TEXT NOT NULL,

    -- Relationships
    [ArticleId] UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE
);
GO

-- Indexes
CREATE INDEX IDX_Comments_CreatedBy ON [dbo].[Comments]([CreatedBy]);
GO
CREATE INDEX IDX_Comments_UpdatedBy ON [dbo].[Comments]([UpdatedBy]);
GO
CREATE INDEX IDX_Comments_CreatedAt ON [dbo].[Comments]([CreatedAt]);
GO
CREATE INDEX IDX_Comments_UpdatedAt ON [dbo].[Comments]([UpdatedAt]);
GO
CREATE INDEX IDX_Comments_ArticleId ON [dbo].[Comments]([ArticleId]);
GO

-- Triggers
CREATE TRIGGER TRG_Comments_UpdatedAt ON [dbo].[Comments]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Comments]
    SET [UpdatedAt] = SYSDATETIME()
    WHERE [Id] IN (SELECT [Id] FROM inserted);
END;
GO
