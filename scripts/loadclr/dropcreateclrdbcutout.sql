DROP PROCEDURE [dbo].[GetAnyCutout]
DROP ASSEMBLY Databasecutout
CREATE ASSEMBLY DatabaseCutout FROM @DLL_Databasecutout WITH PERMISSION_SET = UNSAFE 
GO
CREATE PROCEDURE [dbo].[GetAnyCutout] (
	@dataset [nvarchar](4000),
	@fields [nvarchar](4000),
	@authToken [nvarchar](4000),
	@ipaddr [nvarchar](4000),
	@tlow [int],
	@xlow [int],
	@ylow [int],
	@zlow [int],
	@x_step [int],
	@y_step [int],
	@z_step [int],
	@t_step [int],
	@twidth [int],
	@xwidth [int],
	@ywidth [int],
	@zwidth [int],
	@filter_width [int],
	@time_step [int]
) AS
EXTERNAL NAME [DatabaseCutout].[StoredProcedures].[GetAnyCutout]
GO
GRANT EXECUTE ON [dbo].[GetAnyCutout] TO [turbquery]
GO


