


--drop check constraints

declare @count int = 1
declare @npart int = 16
declare @tab nvarchar(10)

declare @sql  nvarchar(max)

set @tab = 'pr'

while (@count <= @npart)
begin
	set @sql = 'alter table ' + @tab + '_' + RIGHT('00'+rtrim(CAST(@count as nvarchar)),2) + ' 
	drop constraint ck_'+@tab + cast(@count as nvarchar) + '

	'

	print @sql


	set @count = @count + 1
end
	