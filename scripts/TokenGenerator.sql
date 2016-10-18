declare @newid nvarchar(max)
declare @email nvarchar(max)
declare @user nvarchar(max)
declare @domain nvarchar(max)
declare @token nvarchar(max)

set @email='user@domain.com'
set @user = substring(@email,1, case charindex('@', @email)
						WHEN 0 
							then LEN(@email)
						ELSE  charindex('@', @email) - 1
						END)
set @domain = substring(@email, case charindex('@', @email)
						WHEN 0 
							then LEN(@email) + 1
						ELSE  charindex('@', @email) + 1
						END, 1000)

SELECT @newid = LOWER(LEFT(NEWID(),8))


declare @part varchar(200)
set @token = ''
WHILE LEN(@domain) > 0
BEGIN
	IF PATINDEX('%.%', @domain) > 0
	BEGIN
		SET @part = SUBSTRING(@domain, 0, PATINDEX('%.%',@domain))
		--SELECT @part
		SET @domain = SUBSTRING(@domain, LEN(@part + '.') + 1, LEN(@domain))		
		SET @token = @part +'.' + @token
    END
    ELSE
    BEGIN
		SET @part = @domain
		set @token = @part + '.' + @token
	    set @domain = NULL
	END
END
set @token = @token + @user + '-' + @newid
print @token		


