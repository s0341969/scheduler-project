-- 查看某圖號所有版本
SELECT ProjectId, DrawingNumber, VersionNo, CreatedBy, CreatedAt
FROM dbo.DrawingProject
WHERE DrawingNumber = N'M1234'
ORDER BY VersionNo DESC;

-- 查看最新版本與標註明細
DECLARE @DrawingNumber NVARCHAR(100) = N'M1234';
DECLARE @ProjectId INT;

SELECT TOP 1 @ProjectId = ProjectId
FROM dbo.DrawingProject
WHERE DrawingNumber = @DrawingNumber
ORDER BY VersionNo DESC;

SELECT * FROM dbo.DrawingProject WHERE ProjectId = @ProjectId;
SELECT * FROM dbo.DrawingTag WHERE ProjectId = @ProjectId ORDER BY TagId;
