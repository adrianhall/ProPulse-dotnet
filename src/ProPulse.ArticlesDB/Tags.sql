-- Tags table
CREATE TABLE [dbo].[Tags] (
    -- Primary key
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,

    -- Metadata
    [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [CreatedBy] VARCHAR(64) NULL,
    [UpdatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [UpdatedBy] VARCHAR(64) NULL,
    [Version] ROWVERSION NOT NULL,

    -- Content
    [Name] VARCHAR(255) NOT NULL,
    [NormalizedName] VARCHAR(255) NOT NULL,

    -- Relationships
    [ArticleId] UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE
);
GO

-- Indexes
CREATE INDEX IDX_Tags_Name ON [dbo].[Tags]([Name]);
GO
CREATE INDEX IDX_Tags_NormalizedName ON [dbo].[Tags]([NormalizedName]);
GO
CREATE INDEX IDX_Tags_CreatedBy ON [dbo].[Tags]([CreatedBy]);
GO
CREATE INDEX IDX_Tags_UpdatedBy ON [dbo].[Tags]([UpdatedBy]);
GO
CREATE INDEX IDX_Tags_CreatedAt ON [dbo].[Tags]([CreatedAt]);
GO
CREATE INDEX IDX_Tags_UpdatedAt ON [dbo].[Tags]([UpdatedAt]);
GO
CREATE INDEX IDX_Tags_ArticleId ON [dbo].[Tags]([ArticleId]);
GO

-- Triggers
CREATE TRIGGER TRG_Tags_UpdatedAt ON [dbo].[Tags]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Tags]
    SET [UpdatedAt] = SYSDATETIME()
    WHERE [Id] IN (SELECT [Id] FROM inserted);
END;
GO
