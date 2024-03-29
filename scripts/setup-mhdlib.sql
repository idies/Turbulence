USE [master]
GO

/****** Object:  Database [mhdlib]    Script Date: 10/28/2011 10:50:49 ******/
CREATE DATABASE [mhdlib] ON  PRIMARY 
( NAME = N'mhdlib', FILENAME = N'C:\data\data1\sql_db\mhdlib.mdf' , SIZE = 4096KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB ) 
-- FILEGROUP [mhdlib] 
--( NAME = N'data4', FILENAME = N'C:\data\data4\sql_db\mhdlib_data4.ndf' , SIZE = 4096KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB )
 LOG ON 
( NAME = N'mhdlib_log', FILENAME = N'C:\data\data3\sql_db\mhdlib_log3.ldf' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [mhdlib] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [mhdlib].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [mhdlib] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [mhdlib] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [mhdlib] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [mhdlib] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [mhdlib] SET ARITHABORT OFF 
GO

ALTER DATABASE [mhdlib] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [mhdlib] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [mhdlib] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [mhdlib] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [mhdlib] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [mhdlib] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [mhdlib] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [mhdlib] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [mhdlib] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [mhdlib] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [mhdlib] SET  DISABLE_BROKER 
GO

ALTER DATABASE [mhdlib] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [mhdlib] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [mhdlib] SET TRUSTWORTHY ON 
GO

ALTER DATABASE [mhdlib] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [mhdlib] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [mhdlib] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [mhdlib] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [mhdlib] SET  READ_WRITE 
GO

ALTER DATABASE [mhdlib] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [mhdlib] SET  MULTI_USER 
GO

ALTER DATABASE [mhdlib] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [mhdlib] SET DB_CHAINING OFF 
GO

-- Enable CLR 
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO


