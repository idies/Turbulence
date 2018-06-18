﻿USE [turbdev_zw]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[GetAnyCutout]
		@dataset = N'channel5200',
		@fields = N'u',
		@authToken = N'edu.jhu.pha.turbulence-dev',
		@ipaddr = N'0.0.0.0',
		@tlow = 0,
		@xlow = 10000,
		@ylow = 1535,
		@zlow = 0,
		@x_step = 1,
		@y_step = 1,
		@z_step = 1,
		@t_step = 1,
		@twidth = 1,
		@xwidth = 240,
		@ywidth = 1,
		@zwidth = 1,
		@filter_width = 1,
		@time_step = 1

SELECT	@return_value as 'Return Value'

GO