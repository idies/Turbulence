


--=====================================
-- View test for isotropic turbulence
--
--
-- Test performance using a view / union all 
-- to add timesteps a turbdb
-- using first 10 timesteps of isotropic turbulence to test
-- isotropic1024fine -- datasetID = 5
--=====================================

-- SETUP
--===========================
-- sciserver02.sue10timesteps 
--=========================== 
-- DB with first 10 timesteps of turbdb101
-- (slice 101 - prod version is on gw01
-- velocity table has been renamed to 
-- dbo.velsue10timesteps
-- VIEWS:
-- dbo.vel10timesteps -- view on top of velocity table, all timesteps in same DB
-- dbo.vel_union_sameserver -- timesteps are in different DBs but same server
-- dbo.vel_union_distributed -- first 5 timesteps are on a different server

--===========================
-- sciserver02.ts0to4
--===========================
-- DB with first 5 timesteps
-- velocity is named vel

--===========================
-- sciserver02.ts5to9
--===========================
-- DB with second 5 timesteps
-- velocity is named vel

--===========================
-- dsp090.ts0to4
--===========================
-- DB with first 5 timesteps
-- velocity is named vel


--use sp_rename to rename the view you want to test to "vel" in the sue10timesteps DB

sp_rename 'vel_union_distributed', 'vel' --for example

--===================================
-- CREATE VIEW statements
--===================================
USE [sue10timesteps]
GO

/****** Object:  View [dbo].[vel_union_distributed]    Script Date: 1/7/2016 4:01:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create view [dbo].[vel_union_distributed] 
as
select * from dsp090.ts0to4.dbo.vel
union all
select * from ts5to9.dbo.vel
GO


USE [sue10timesteps]
GO

/****** Object:  View [dbo].[vel_union_sameserver]    Script Date: 1/7/2016 4:01:16 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create view [dbo].[vel_union_sameserver]
as select * from ts0to4.dbo.vel_ts0to4
union all
select * from ts5to9.dbo.vel_ts5to9
GO

USE [sue10timesteps]
GO

/****** Object:  View [dbo].[vel10timesteps]    Script Date: 1/7/2016 4:01:29 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


create view [dbo].[vel10timesteps]
as select * from velsue10timesteps
GO



