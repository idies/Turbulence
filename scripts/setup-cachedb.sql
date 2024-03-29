USE [master]
GO

/****** Object:  Database [cachedb]    Script Date: 03/24/2011 12:43:28 ******/
CREATE DATABASE [cachedb] ON  PRIMARY 
( NAME = N'cachedb', FILENAME = N'C:\data\tempdb1\sql_db\cachedb\cachedb_PRIMARY.mdf' , SIZE = 4096KB , MAXSIZE = 1GB, FILEGROWTH = 10240KB ), 
 FILEGROUP [CACHEDATA] 
( NAME = N'data1', FILENAME = N'C:\data\tempdb1\sql_db\cachedb\cachedb_data1.ndf' , SIZE = 4096KB , MAXSIZE = 100GB, FILEGROWTH = 524288KB ), 
( NAME = N'data2', FILENAME = N'C:\data\tempdb2\sql_db\cachedb\cachedb_data2.ndf' , SIZE = 4096KB , MAXSIZE = 100GB, FILEGROWTH = 524288KB )
 LOG ON 
( NAME = N'cachedb_log', FILENAME = N'C:\data\tempdb2\sql_db\cachedb\cachedb_log.ldf' , SIZE = 4096KB , MAXSIZE = 10GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [cachedb] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [cachedb].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [cachedb] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [cachedb] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [cachedb] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [cachedb] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [cachedb] SET ARITHABORT OFF 
GO

ALTER DATABASE [cachedb] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [cachedb] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [cachedb] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [cachedb] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [cachedb] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [cachedb] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [cachedb] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [cachedb] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [cachedb] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [cachedb] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [cachedb] SET  DISABLE_BROKER 
GO

ALTER DATABASE [cachedb] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [cachedb] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [cachedb] SET TRUSTWORTHY ON 
GO

ALTER DATABASE [cachedb] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [cachedb] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [cachedb] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [cachedb] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [cachedb] SET  READ_WRITE 
GO

ALTER DATABASE [cachedb] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [cachedb] SET  MULTI_USER 
GO

ALTER DATABASE [cachedb] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [cachedb] SET DB_CHAINING OFF 
GO

ALTER DATABASE [cachedb] SET ALLOW_SNAPSHOT_ISOLATION ON
GO

-- Enable CLR 
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO