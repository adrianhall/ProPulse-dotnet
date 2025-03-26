-- ArticleTags join table
CREATE TABLE [dbo].[ArticleTags] (
    [ArticleId] UNIQUEIDENTIFIER NOT NULL,
    [TagId] UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY ([ArticleId], [TagId]),
    FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([TagId]) REFERENCES [dbo].[Tags]([Id]) ON DELETE CASCADE
);
GO

-- Indexes
CREATE INDEX IDX_ArticleTags_ArticleId ON [dbo].[ArticleTags]([ArticleId]);
GO
CREATE INDEX IDX_ArticleTags_TagId ON [dbo].[ArticleTags]([TagId]);
GO
