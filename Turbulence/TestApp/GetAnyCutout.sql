USE [turbdev_zw]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[GetAnyCutout]
		@dataset = N'isotropic1024fine',
		@fields = N'u',
		@authToken = N'edu.jhu.pha.turb-dev',
		@ipaddr = N'0.0.0.0',
		@tlow = 1,
		@xlow = 1,
		@ylow = 1,
		@zlow = 1,
		@x_step = 1,
		@y_step = 1,
		@z_step = 1,
		@t_step = 1,
		@twidth = 1,
		@xwidth = 8,
		@ywidth = 8,
		@zwidth = 8,
		@filter_width = 1,
		@time_step = 1

SELECT	@return_value as 'Return Value'

GO