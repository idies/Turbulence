USE [turblib]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[GetAnyCutout]
		@dataset = N'isotropic1024coarse',
		@fields = N'u',
		@authToken = N'edu.jhu.ssh-c11eeb58',
		@ipaddr = N'127.0.0.1',
		@tlow = 10270,
		@xlow = 0,
		@ylow = 516,
		@zlow = 516,
		@x_step = 1,
		@y_step = 1,
		@z_step = 1,
		@t_step = 1,
		@twidth = 1,
		@xwidth = 10,
		@ywidth = 10,
		@zwidth = 10,
		@filter_width = 1,
		@time_step = 1

SELECT	@return_value as 'Return Value'

GO
