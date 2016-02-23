USE [master]
GO

/****** Object:  Database [channeldb10]    Script Date: 12/08/2015 13:59:52 ******/
CREATE DATABASE [channeldb10] ON  PRIMARY 
( NAME = N'PRIMARY', FILENAME = N'C:\data\ssd0\sql_db\channeldb10_PRIMARY.mdf' , SIZE = 32768KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB ), 
 FILEGROUP [FG01] 
( NAME = N'channeldb10_FG01', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG01.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG01_2', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG01_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG01_3', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG01_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG01_4', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG01_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG02] 
( NAME = N'channeldb10_FG02', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG02.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG02_2', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG02_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG02_3', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG02_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG02_4', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG02_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG03] 
( NAME = N'channeldb10_FG03', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG03.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG03_2', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG03_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG03_3', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG03_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG03_4', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG03_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG04] 
( NAME = N'channeldb10_FG04', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG04.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG04_2', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG04_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG04_3', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG04_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG04_4', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG04_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG05] 
( NAME = N'channeldb10_FG05', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG05.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG05_2', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG05_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG05_3', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG05_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG05_4', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG05_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG06] 
( NAME = N'channeldb10_FG06', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG06.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG06_2', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG06_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG06_3', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG06_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG06_4', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG06_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG07] 
( NAME = N'channeldb10_FG07', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG07.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG07_2', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG07_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG07_3', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG07_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG07_4', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG07_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG08] 
( NAME = N'channeldb10_FG08', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG08.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG08_2', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG08_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG08_3', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG08_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG08_4', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG08_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG09] 
( NAME = N'channeldb10_FG09', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG09.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG09_2', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG09_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG09_3', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG09_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG09_4', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG09_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG10] 
( NAME = N'channeldb10_FG10', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG10.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG10_2', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG10_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG10_3', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG10_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG10_4', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG10_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG11] 
( NAME = N'channeldb10_FG11', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG11.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG11_2', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG11_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG11_3', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG11_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG11_4', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG11_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG12] 
( NAME = N'channeldb10_FG12', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG12.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG12_2', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG12_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG12_3', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG12_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG12_4', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG12_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG13] 
( NAME = N'channeldb10_FG13', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG13.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG13_2', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG13_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG13_3', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG13_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG13_4', FILENAME = N'C:\Data\ssd1\sql_db\channeldb10_FG13_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG14] 
( NAME = N'channeldb10_FG14', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG14.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG14_2', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG14_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG14_3', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG14_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG14_4', FILENAME = N'C:\Data\ssd2\sql_db\channeldb10_FG14_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG15] 
( NAME = N'channeldb10_FG15', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG15.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG15_2', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG15_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG15_3', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG15_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG15_4', FILENAME = N'C:\Data\ssd3\sql_db\channeldb10_FG15_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG16] 
( NAME = N'channeldb10_FG16', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG16.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG16_2', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG16_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG16_3', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG16_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG16_4', FILENAME = N'C:\Data\ssd4\sql_db\channeldb10_FG16_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG17] 
( NAME = N'channeldb10_FG17', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG17.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG17_2', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG17_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG17_3', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG17_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG17_4', FILENAME = N'C:\Data\ssd5\sql_db\channeldb10_FG17_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG18] 
( NAME = N'channeldb10_FG18', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG18.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG18_2', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG18_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG18_3', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG18_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG18_4', FILENAME = N'C:\Data\ssd6\sql_db\channeldb10_FG18_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG19] 
( NAME = N'channeldb10_FG19', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG19.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG19_2', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG19_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG19_3', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG19_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG19_4', FILENAME = N'C:\Data\ssd7\sql_db\channeldb10_FG19_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG20] 
( NAME = N'channeldb10_FG20', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG20.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG20_2', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG20_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG20_3', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG20_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG20_4', FILENAME = N'C:\Data\ssd8\sql_db\channeldb10_FG20_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG21] 
( NAME = N'channeldb10_FG21', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG21.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG21_2', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG21_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG21_3', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG21_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG21_4', FILENAME = N'C:\Data\ssd9\sql_db\channeldb10_FG21_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG22] 
( NAME = N'channeldb10_FG22', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG22.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG22_2', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG22_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG22_3', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG22_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG22_4', FILENAME = N'C:\Data\ssd10\sql_db\channeldb10_FG22_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG23] 
( NAME = N'channeldb10_FG23', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG23.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG23_2', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG23_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG23_3', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG23_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG23_4', FILENAME = N'C:\Data\ssd11\sql_db\channeldb10_FG23_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  ), 
 FILEGROUP [FG24] 
( NAME = N'channeldb10_FG24', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG24.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG24_2', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG24_2.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG24_3', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG24_3.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0), 
( NAME = N'channeldb10_FG24_4', FILENAME = N'C:\Data\ssd12\sql_db\channeldb10_FG24_4.ndf' , SIZE = 120 GB , MAXSIZE = UNLIMITED, FILEGROWTH = 0  )
 LOG ON 
( NAME = N'channeldb10_log', FILENAME = N'C:\Data\ssd13\sql_db\channeldb10_log.ldf' , SIZE = 100GB , MAXSIZE = 2048GB , FILEGROWTH = 153600KB )
GO

--ALTER DATABASE [channeldb10] SET COMPATIBILITY_LEVEL = 100
--GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [channeldb10].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [channeldb10] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [channeldb10] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [channeldb10] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [channeldb10] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [channeldb10] SET ARITHABORT OFF 
GO

ALTER DATABASE [channeldb10] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [channeldb10] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [channeldb10] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [channeldb10] SET AUTO_UPDATE_STATISTICS OFF 
GO

ALTER DATABASE [channeldb10] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [channeldb10] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [channeldb10] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [channeldb10] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [channeldb10] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [channeldb10] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [channeldb10] SET  DISABLE_BROKER 
GO

ALTER DATABASE [channeldb10] SET AUTO_UPDATE_STATISTICS_ASYNC ON 
GO

ALTER DATABASE [channeldb10] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [channeldb10] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [channeldb10] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO

ALTER DATABASE [channeldb10] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [channeldb10] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [channeldb10] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [channeldb10] SET  READ_WRITE 
GO

ALTER DATABASE [channeldb10] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [channeldb10] SET  MULTI_USER 
GO

ALTER DATABASE [channeldb10] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [channeldb10] SET DB_CHAINING OFF 
GO


