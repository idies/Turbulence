SELECT LOWER(LEFT(NEWID(),8))

BEGIN TRANSACTION t1

DECLARE @id INT

INSERT INTO [turbinfo].[dbo].[users]
          ([authkey]
          ,[limit])
    VALUES
		('edu.user.here-aaaa0000', 0)
SET @ID = SCOPE_IDENTITY()

INSERT INTO [turbinfo].[dbo].[particlecount]
          ([uid]
          ,[records])
    VALUES
          ( @ID, 0)

IF @@error <> 0
BEGIN
	ROLLBACK TRANSACTION
END

COMMIT TRANSACTION t1