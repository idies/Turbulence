USE [turbdev_zw]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[GetAnyCutout]
		@dataset = N'bl_zaki',
		@fields = N'p',
		@authToken = N'edu.jhu.pha.turbulence-dev',
		@ipaddr = N'0.0.0.0',
		@tlow = 30,
		@xlow = 0,
		@ylow = 0,
		@zlow = 0,
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