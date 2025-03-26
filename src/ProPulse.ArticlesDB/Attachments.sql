-- Attachments table
CREATE TABLE [dbo].[Attachments] (
    -- Primary key
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,

    -- Metadata
    [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [CreatedBy] VARCHAR(64) NULL,
    [UpdatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [UpdatedBy] VARCHAR(64) NULL,
    [Version] ROWVERSION NOT NULL,

    -- Content
    [ContentType] VARCHAR(255) NOT NULL,
    [Locator] VARCHAR(255) NOT NULL,
    [Path] VARCHAR(255) NOT NULL,

    -- Relationships
    [ArticleId] UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE
);
GO

-- Indexes
CREATE INDEX IX_Attachments_ContentType ON [dbo].[Attachments]([ContentType]);
GO
CREATE INDEX IX_Attachments_Path ON [dbo].[Attachments]([Path]);
GO
CREATE INDEX IX_Attachments_Locator ON [dbo].[Attachments]([Locator]);
GO
CREATE INDEX IX_Attachments_CreatedBy ON [dbo].[Attachments]([CreatedBy]);
GO
CREATE INDEX IX_Attachments_UpdatedBy ON [dbo].[Attachments]([UpdatedBy]);
GO
CREATE INDEX IDX_Attachments_CreatedAt ON [dbo].[Attachments]([CreatedAt]);
GO
CREATE INDEX IDX_Attachments_UpdatedAt ON [dbo].[Attachments]([UpdatedAt]);
GO
CREATE INDEX IDX_Attachments_ArticleId ON [dbo].[Attachments]([ArticleId]);
GO

-- Triggers
CREATE TRIGGER trg_Attachments_UpdatedAt ON [dbo].[Attachments]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Attachments]
    SET [UpdatedAt] = SYSDATETIME()
    WHERE [Id] IN (SELECT [Id] FROM inserted);
END;
GO
