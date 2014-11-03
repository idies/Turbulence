USE [master]
GO

/****** Object:  Database [mhddb]    Script Date: 03/24/2011 12:43:28 ******/
CREATE DATABASE [mhddb] ON  PRIMARY 
( NAME = N'mhddb', FILENAME = N'C:\data\data1\mhddb\mhddb.mdf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB ), 
 FILEGROUP [MHDDATA] 
( NAME = N'data1', FILENAME = N'C:\data\data1\mhddb\data1.ndf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB ), 
( NAME = N'data2', FILENAME = N'C:\data\data2\mhddb\data2.ndf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB ), 
( NAME = N'data3', FILENAME = N'C:\data\data3\mhddb\data3.ndf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB ), 
( NAME = N'data4', FILENAME = N'C:\data\data4\mhddb\data4.ndf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 524288KB )
 LOG ON 
( NAME = N'mhddb_log1', FILENAME = N'C:\data\data1\mhddb\mhddb_log1.ldf' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 10%), 
( NAME = N'mhddb_log2', FILENAME = N'C:\data\data2\mhddb\mhddb_log2.ldf' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 10%), 
( NAME = N'mhddb_log3', FILENAME = N'C:\data\data3\mhddb\mhddb_log3.ldf' , SIZE = 2048KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [mhddb] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [mhddb].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [mhddb] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [mhddb] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [mhddb] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [mhddb] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [mhddb] SET ARITHABORT OFF 
GO

ALTER DATABASE [mhddb] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [mhddb] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [mhddb] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [mhddb] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [mhddb] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [mhddb] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [mhddb] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [mhddb] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [mhddb] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [mhddb] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [mhddb] SET  DISABLE_BROKER 
GO

ALTER DATABASE [mhddb] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [mhddb] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [mhddb] SET TRUSTWORTHY ON 
GO

ALTER DATABASE [mhddb] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [mhddb] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [mhddb] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [mhddb] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [mhddb] SET  READ_WRITE 
GO

ALTER DATABASE [mhddb] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [mhddb] SET  MULTI_USER 
GO

ALTER DATABASE [mhddb] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [mhddb] SET DB_CHAINING OFF 
GO

-- Enable CLR 
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO