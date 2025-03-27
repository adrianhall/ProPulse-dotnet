-- Ratings table
CREATE TABLE [dbo].[Ratings] (
    -- Primary key
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,

    -- Metadata
    [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [CreatedBy] VARCHAR(64) NULL,
    [UpdatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
    [UpdatedBy] VARCHAR(64) NULL,
    [Version] ROWVERSION NOT NULL,

    -- Content
    [Value] INT NOT NULL CONSTRAINT CHK_Ratings_Value CHECK ([Value] BETWEEN 1 AND 5),

    -- Relationships
    [ArticleId] UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE
);
GO

-- Indexes
CREATE INDEX IDX_Ratings_CreatedBy ON [dbo].[Ratings]([CreatedBy]);
GO
CREATE INDEX IDX_Ratings_UpdatedBy ON [dbo].[Ratings]([UpdatedBy]);
GO
CREATE INDEX IDX_Ratings_CreatedAt ON [dbo].[Ratings]([CreatedAt]);
GO
CREATE INDEX IDX_Ratings_UpdatedAt ON [dbo].[Ratings]([UpdatedAt]);
GO
CREATE INDEX IDX_Ratings_ArticleId ON [dbo].[Ratings]([ArticleId]);
GO

-- Triggers
CREATE TRIGGER TRG_Ratings_UpdatedAt ON [dbo].[Ratings]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Ratings]
    SET [UpdatedAt] = SYSDATETIME()
    WHERE [Id] IN (SELECT [Id] FROM inserted);
END;
GO
