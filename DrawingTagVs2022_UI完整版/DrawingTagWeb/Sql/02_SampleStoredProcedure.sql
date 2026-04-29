/*
用途：
前端按「從資料庫載入規格」時，API 會呼叫你的正式 Stored Procedure：
    dbo.KNV10256_SIP量測表

API 會傳入三個條件，並對應到 SP 參數：
    indwg  -> @INDWG   圖號 / 製卡圖號
    dwgrev -> @DWGREV  版次
    option -> @OPTION  項目：首件 / 製程中 / 終檢

你的正式 SP 宣告：
ALTER PROCEDURE [dbo].[KNV10256_SIP量測表]
    @INDWG   VARCHAR(30),
    @DWGREV  VARCHAR(10),
    @OPTION  VARCHAR(10)
AS
BEGIN
    -- 你的查詢邏輯
END

前端套色至少需要回傳欄位：
    ItemNo 或 項次
    InspectionMethod 或 檢驗方式

其他欄位會原樣顯示在右側 GridView。

注意：如果你的 SP 已經存在，不需要執行這個檔案。
*/

-- 以下只是測試用範例。正式環境如果已有 dbo.KNV10256_SIP量測表，請不要覆蓋。
/*
CREATE OR ALTER PROCEDURE dbo.KNV10256_SIP量測表
    @INDWG   VARCHAR(30),
    @DWGREV  VARCHAR(10),
    @OPTION  VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        1 AS ItemNo,
        'CMM' AS InspectionMethod,
        @INDWG AS INDWG,
        @DWGREV AS DWGREV,
        @OPTION AS [OPTION],
        N'外觀尺寸' AS CheckItem,
        N'±0.01' AS Tolerance
    UNION ALL
    SELECT
        2,
        CASE WHEN @OPTION = N'終檢' THEN 'GI' ELSE 'PM' END,
        @INDWG,
        @DWGREV,
        @OPTION,
        N'孔位檢查',
        N'±0.02';
END
GO
*/
