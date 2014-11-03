USE [master]
GO

/****** Object:  Database [channeldb01]    Script Date: 11/14/2012 16:39:37 ******/
CREATE DATABASE [channeldb01] ON  PRIMARY 
( NAME = N'PRIMARY', FILENAME = N'C:\data\ssd2\sql_db\channeldb01_PRIMARY.mdf' , SIZE = 4096KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB ), 
 FILEGROUP [FG01] 
( NAME = N'channeldb01_FG01', FILENAME = N'C:\data\data01\channeldb\channeldb01_FG01.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG02] 
( NAME = N'channeldb01_FG02', FILENAME = N'C:\data\data02\channeldb\channeldb01_FG02.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG03] 
( NAME = N'channeldb01_FG03', FILENAME = N'C:\data\data03\channeldb\channeldb01_FG03.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG04] 
( NAME = N'channeldb01_FG04', FILENAME = N'C:\data\data04\channeldb\channeldb01_FG04.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG05] 
( NAME = N'channeldb01_FG05', FILENAME = N'C:\data\data05\channeldb\channeldb01_FG05.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG06] 
( NAME = N'channeldb01_FG06', FILENAME = N'C:\data\data06\channeldb\channeldb01_FG06.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG07] 
( NAME = N'channeldb01_FG07', FILENAME = N'C:\data\data07\channeldb\channeldb01_FG07.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG08] 
( NAME = N'channeldb01_FG08', FILENAME = N'C:\data\data08\channeldb\channeldb01_FG08.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG09] 
( NAME = N'channeldb01_FG09', FILENAME = N'C:\data\data09\channeldb\channeldb01_FG09.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG10] 
( NAME = N'channeldb01_FG10', FILENAME = N'C:\data\data10\channeldb\channeldb01_FG10.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG11] 
( NAME = N'channeldb01_FG11', FILENAME = N'C:\data\data11\channeldb\channeldb01_FG11.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG12] 
( NAME = N'channeldb01_FG12', FILENAME = N'C:\data\data12\channeldb\channeldb01_FG12.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG13] 
( NAME = N'channeldb01_FG13', FILENAME = N'C:\data\data01\channeldb\channeldb01_FG13.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG14] 
( NAME = N'channeldb01_FG14', FILENAME = N'C:\data\data02\channeldb\channeldb01_FG14.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG15] 
( NAME = N'channeldb01_FG15', FILENAME = N'C:\data\data03\channeldb\channeldb01_FG15.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG16] 
( NAME = N'channeldb01_FG16', FILENAME = N'C:\data\data04\channeldb\channeldb01_FG16.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG17] 
( NAME = N'channeldb01_FG17', FILENAME = N'C:\data\data05\channeldb\channeldb01_FG17.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG18] 
( NAME = N'channeldb01_FG18', FILENAME = N'C:\data\data06\channeldb\channeldb01_FG18.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG19] 
( NAME = N'channeldb01_FG19', FILENAME = N'C:\data\data07\channeldb\channeldb01_FG19.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG20] 
( NAME = N'channeldb01_FG20', FILENAME = N'C:\data\data08\channeldb\channeldb01_FG20.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG21] 
( NAME = N'channeldb01_FG21', FILENAME = N'C:\data\data09\channeldb\channeldb01_FG21.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG22] 
( NAME = N'channeldb01_FG22', FILENAME = N'C:\data\data10\channeldb\channeldb01_FG22.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG23] 
( NAME = N'channeldb01_FG23', FILENAME = N'C:\data\data11\channeldb\channeldb01_FG23.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 ), 
 FILEGROUP [FG24] 
( NAME = N'channeldb01_FG24', FILENAME = N'C:\data\data12\channeldb\channeldb01_FG24.ndf' , SIZE = 76GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0 )
 LOG ON 
( NAME = N'channeldb01_log', FILENAME = N'C:\data\ssd2\sql_db\channeldb01_log.ldf' , SIZE = 4096KB , MAXSIZE = 2048MB , FILEGROWTH = 10%)
GO

ALTER DATABASE [channeldb01] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [channeldb01].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [channeldb01] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [channeldb01] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [channeldb01] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [channeldb01] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [channeldb01] SET ARITHABORT OFF 
GO

ALTER DATABASE [channeldb01] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [channeldb01] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [channeldb01] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [channeldb01] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [channeldb01] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [channeldb01] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [channeldb01] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [channeldb01] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [channeldb01] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [channeldb01] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [channeldb01] SET  DISABLE_BROKER 
GO

ALTER DATABASE [channeldb01] SET AUTO_UPDATE_STATISTICS_ASYNC ON 
GO

ALTER DATABASE [channeldb01] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [channeldb01] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [channeldb01] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO

ALTER DATABASE [channeldb01] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [channeldb01] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [channeldb01] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [channeldb01] SET  READ_WRITE 
GO

ALTER DATABASE [channeldb01] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [channeldb01] SET  MULTI_USER 
GO

ALTER DATABASE [channeldb01] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [channeldb01] SET DB_CHAINING OFF 
GO
