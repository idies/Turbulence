--
-- Alter assembly does not disrupt connections,
--
ALTER ASSEMBLY Turbulence FROM @DLL_Turbulence WITH PERMISSION_SET = UNSAFE DROP FILE 'databasecutout.dll'
GO
