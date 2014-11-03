use master;

CREATE DATABASE [mhddb024] ON  PRIMARY 
( NAME = N'PRIMARY', FILENAME = N'C:\data\data3\sql_db\mhddb024_PRIMARY.mdf' , SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 
 LOG ON 
( NAME = N'mhddb024_log', FILENAME = N'C:\data\data4\sql_db\mhddb024_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
alter database mhddb024 set recovery simple
