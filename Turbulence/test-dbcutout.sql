USE [turblib]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[GetAnyCutout]
		@dataset = N'isotropic1024coarse',
		@fields = N'u',
		@authToken = N'edu.jhu.ssh-c11eeb58',
		@ipaddr = N'127.0.0.1',
		@tlow = 10245,
		@xlow = 0,
		@ylow = 0,
		@zlow = 0,
		@x_step = 0,
		@y_step = 1,
		@z_step = 1,
		@t_step = 1,
		@twidth = 1,
		@xwidth = 1,
		@ywidth = 1,
		@zwidth = 1,
		@filter_width = 1,
		@time_step = 1

SELECT	@return_value as 'Return Value'

GO
