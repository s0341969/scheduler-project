IF OBJECT_ID('dbo.DrawingTag', 'U') IS NOT NULL DROP TABLE dbo.DrawingTag;
IF OBJECT_ID('dbo.DrawingProject', 'U') IS NOT NULL DROP TABLE dbo.DrawingProject;
GO

CREATE TABLE dbo.DrawingProject
(
    ProjectId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DrawingProject PRIMARY KEY,
    DrawingNumber NVARCHAR(100) NOT NULL,
    VersionNo     INT NOT NULL,
    ImageBase64   NVARCHAR(MAX) NULL,
    SpecDataJson  NVARCHAR(MAX) NULL,
    CurrentSeq    NVARCHAR(20) NULL,
    CurrentZoom   DECIMAL(10,4) NOT NULL CONSTRAINT DF_DrawingProject_CurrentZoom DEFAULT(1),
    CreatedBy     NVARCHAR(50) NULL,
    CreatedAt     DATETIME NOT NULL CONSTRAINT DF_DrawingProject_CreatedAt DEFAULT(GETDATE()),
    Remark        NVARCHAR(500) NULL
);
GO

CREATE TABLE dbo.DrawingTag
(
    TagId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DrawingTag PRIMARY KEY,
    ProjectId        INT NOT NULL,
    ItemNo           INT NOT NULL,
    ItemText         NVARCHAR(50) NULL,
    InspectionMethod NVARCHAR(50) NULL,
    X                FLOAT NOT NULL,
    Y                FLOAT NOT NULL,
    CreatedAt        DATETIME NOT NULL CONSTRAINT DF_DrawingTag_CreatedAt DEFAULT(GETDATE()),
    CONSTRAINT FK_DrawingTag_DrawingProject FOREIGN KEY(ProjectId) REFERENCES dbo.DrawingProject(ProjectId)
);
GO

CREATE INDEX IX_DrawingProject_DrawingNumber_VersionNo
ON dbo.DrawingProject(DrawingNumber, VersionNo DESC);
GO

CREATE INDEX IX_DrawingTag_ProjectId
ON dbo.DrawingTag(ProjectId, TagId);
GO
