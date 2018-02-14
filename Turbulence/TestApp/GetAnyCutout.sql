USE [turbdev_zw]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[GetAnyCutout]
		@dataset = N'strat4096',
		@fields = N't',
		@authToken = N'edu.jhu.pha.turbulence-dev',
		@ipaddr = N'0.0.0.0',
		@tlow = 0,
		@xlow = 0,
		@ylow = 0,
		@zlow = 510,
		@x_step = 1,
		@y_step = 1,
		@z_step = 1,
		@t_step = 1,
		@twidth = 1,
		@xwidth = 256,
		@ywidth = 256,
		@zwidth = 5,
		@filter_width = 1,
		@time_step = 1

SELECT	@return_value as 'Return Value'

GO