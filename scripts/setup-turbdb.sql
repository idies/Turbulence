--
-- One-time configuration settings for the Turbulence Database
-- NOTE: The database needs to be created seperately.
--

USE master
GO
-- Enable CLR 
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO
-- Grant unsafe assembly access to your own user
-- GRANT UNSAFE ASSEMBLY TO [sa]
GRANT UNSAFE ASSEMBLY TO [BUILTIN\Administrators]
-- GRANT UNSAFE ASSEMBLY TO [SDSS\eric]
GO

-- Allow unsafe assemblies
ALTER DATABASE turbdb003 SET TRUSTWORTHY ON
GO
ALTER DATABASE turbdb009 SET TRUSTWORTHY ON
GO