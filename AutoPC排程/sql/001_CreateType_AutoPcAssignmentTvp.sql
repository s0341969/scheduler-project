IF TYPE_ID(N'dbo.AutoPcAssignmentTvp') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE dbo.AutoPcAssignmentTvp AS TABLE
        (
            OrdTp       varchar(1)      NOT NULL,
            OrdNo       varchar(20)     NOT NULL,
            OrdSq       int             NOT NULL,
            OrdSq1      int             NOT NULL,
            OrdSq2      int             NULL,
            InPart      varchar(50)     NULL,
            ProcessCode varchar(50)     NULL,
            ProductName nvarchar(50)    NULL,
            MachineId   varchar(50)     NOT NULL,
            StartTime   datetime2(0)    NOT NULL,
            EndTime     datetime2(0)    NOT NULL,
            WorkHours   decimal(18,4)   NOT NULL,
            Assigner    varchar(50)     NOT NULL,
            Content     varchar(100)    NULL,
            Remark      int             NULL,
            CreatedAt   datetime2(0)    NOT NULL
        );
    ');
END;
