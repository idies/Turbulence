--=================================================================
--   Install.sql
--	 2007-07-18 Tamas Budavari
-------------------------------------------------------------------
-- needs to be run after Deploy.sql
-------------------------------------------------------------------
-- History:
-- [2012-05-14] Tamas: Modular space-filling curve design w/ new SQL interface
-------------------------------------------------------------------


PRINT '*** INSTALL UDTs AND UDFs ***'
--====================================================================
--                       User Defined Types
--====================================================================

CREATE TYPE Point
EXTERNAL NAME SqlSearch3D.[UserDefinedTypes.Point3D]
GO

--=======================================================================================

CREATE TYPE Box
EXTERNAL NAME SqlSearch3D.[UserDefinedTypes.Box3D]
GO
--=======================================================================================

CREATE TYPE Sphere
EXTERNAL NAME SqlSearch3D.[UserDefinedTypes.Sphere3D]
GO

--=======================================================================================

CREATE TYPE Shape 
EXTERNAL NAME SqlSearch3D.[UserDefinedTypes.Shape3D]
GO
--=======================================================================================

CREATE TYPE Cone
EXTERNAL NAME SqlSearch3D.[UserDefinedTypes.Cone3D]
GO

--=======================================================================================
CREATE FUNCTION dbo.fKey(@curve nvarchar(1), @bits int, @ix int, @iy int, @iz int)
--/H Returns the Peano-Hilbert index at the specified BITS level 
--/H for the cell with position specified by the ix/iy/iz arguments.
--/U --------------------------------------------------
--/T Parameter(s):
--/T <li> @curve varchar: name of the space-filling curve
--/T <li> @bits int: refinement level for which the index is calculated
--/T <li> @ix int: x-index of cell
--/T <li> @iy int: y-index of cell
--/T <li> @iz int: z-index of cell
RETURNS bigint
AS EXTERNAL NAME SqlSearch3D.UserDefinedFunctions.GetKey
GO


--====================================================================
CREATE FUNCTION dbo.fCover(@curve nvarchar(1), @lvl int, @box Box, @periodic bit, @qry Shape)
-------------------------------------------------------
--/H Calculates the cover for the query.
--/U --------------------------------------------------
--/T Parameter(s):
--/T <li> @box: the box to search
--/T <li> @lvl: the maximum resolution level of the cover
--/T <li> @qry: the query shape
-------------------------------------------------------
RETURNS TABLE 
(
	FullOnly bit,
	KeyMin bigint,
	KeyMax bigint,
	ShiftX real,
	ShiftY real,
	ShiftZ real
)
AS EXTERNAL NAME SqlSearch3D.UserDefinedFunctions.Cover3D
GO



----------------------------------------------------------------------------------------------
PRINT '[Install.sql]: UDTs and UDFs installed.'
----------------------------------------------------------------------------------------------
