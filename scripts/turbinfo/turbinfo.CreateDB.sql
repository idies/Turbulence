USE [master]
GO

/****** Object:  Database [turbinfo]    Script Date: 3/27/2015 3:51:57 PM ******/
CREATE DATABASE [turbinfo]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'turbinfo', FILENAME = N'C:\data\data1\sql_db\turbinfo.mdf' , SIZE = 213511808KB , MAXSIZE = UNLIMITED, FILEGROWTH = 10%)
 LOG ON 
( NAME = N'turbinfo_log', FILENAME = N'C:\data\data4\sql_db\turbinfo_1.ldf' , SIZE = 514816KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [turbinfo] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [turbinfo].[dbo].[sp_fulltext_database] @action = 'disable'
end
GO

ALTER DATABASE [turbinfo] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [turbinfo] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [turbinfo] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [turbinfo] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [turbinfo] SET ARITHABORT OFF 
GO

ALTER DATABASE [turbinfo] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [turbinfo] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [turbinfo] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [turbinfo] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [turbinfo] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [turbinfo] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [turbinfo] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [turbinfo] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [turbinfo] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [turbinfo] SET  DISABLE_BROKER 
GO

ALTER DATABASE [turbinfo] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [turbinfo] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [turbinfo] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [turbinfo] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [turbinfo] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [turbinfo] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [turbinfo] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [turbinfo] SET RECOVERY FULL 
GO

ALTER DATABASE [turbinfo] SET  MULTI_USER 
GO

ALTER DATABASE [turbinfo] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [turbinfo] SET DB_CHAINING OFF 
GO

ALTER DATABASE [turbinfo] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO

ALTER DATABASE [turbinfo] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO

ALTER DATABASE [turbinfo] SET DELAYED_DURABILITY = DISABLED 
GO

ALTER DATABASE [turbinfo] SET  READ_WRITE 
GO


