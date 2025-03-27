-- Articles table
CREATE TABLE [dbo].[Articles] (
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
    [PublishedAt] DATETIME2(6) NULL,
    [PublishedUntil] DATETIME2(6) NULL,
    [State] VARCHAR(20) NOT NULL DEFAULT 'Draft' CONSTRAINT CHK_Articles_State CHECK ([State] IN ('Draft', 'Published', 'Retired')),
    [Summary] VARCHAR(4096) NULL,
    [Title] VARCHAR(255) NOT NULL
);
GO

-- Indexes
CREATE INDEX IDX_Articles_CreatedAt ON [dbo].[Articles]([CreatedAt]);
GO
CREATE INDEX IDX_Articles_CreatedBy ON [dbo].[Articles]([CreatedBy]);
GO
CREATE INDEX IDX_Articles_PublishedAt ON [dbo].[Articles]([PublishedAt]);
GO
CREATE INDEX IDX_Articles_PublishedUntil ON [dbo].[Articles]([PublishedUntil]);
GO
CREATE INDEX IDX_Articles_State ON [dbo].[Articles]([State]);
GO
CREATE INDEX IDX_Articles_UpdatedAt ON [dbo].[Articles]([UpdatedAt]);
GO
CREATE INDEX IDX_Articles_UpdatedBy ON [dbo].[Articles]([UpdatedBy]);
GO

-- Triggers
CREATE TRIGGER TRG_Articles_PublishedAt ON [dbo].[Articles]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE([State])
    BEGIN
        UPDATE [dbo].[Articles]
        SET [PublishedAt] = CASE 
                WHEN [State] = 'Published' AND [PublishedAt] IS NULL THEN SYSDATETIME() 
                WHEN [State] = 'Draft' THEN NULL 
                ELSE [PublishedAt] 
            END,
            [PublishedUntil] = CASE 
                WHEN [State] = 'Retired' AND [PublishedUntil] IS NULL THEN SYSDATETIME() 
                WHEN [State] = 'Published' OR [State] = 'Draft' THEN NULL 
                ELSE [PublishedUntil] 
            END
        WHERE [Id] IN (SELECT [Id] FROM inserted);
    END;
END;
GO

CREATE TRIGGER TRG_Articles_UpdatedAt ON [dbo].[Articles]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Articles]
    SET [UpdatedAt] = SYSDATETIME()
    WHERE [Id] IN (SELECT [Id] FROM inserted);
END;
GO


