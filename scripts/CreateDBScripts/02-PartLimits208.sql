/*

02-PartLimits208.sql
6/20/2016 - S. Werner

Create PartLimits table for isotropic turbulence with 16 partitions.

Run this script in newly created turbdb database.  
Each turb db requires this table to set up partitions, filegroups, constraints, etc.

*/



/****** Object:  Table [dbo].[PartLimits208]    Script Date: 6/17/2016 2:47:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartLimits208](
	[sliceNum] [int] NOT NULL,
	[partitionNum] [int] NOT NULL,
	[minLim] [bigint] NOT NULL,
	[maxLim] [bigint] NOT NULL,
	[ordinal] [int] NOT NULL,
 CONSTRAINT [pk_partLimits208] PRIMARY KEY CLUSTERED 
(
	[sliceNum] ASC,
	[partitionNum] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 1, 0, 8388607, 1)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 2, 8388608, 16777215, 2)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 3, 16777216, 25165823, 3)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 4, 25165824, 33554431, 4)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 5, 33554432, 41943039, 5)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 6, 41943040, 50331647, 6)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 7, 50331648, 58720255, 7)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 8, 58720256, 67108863, 8)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 9, 67108864, 75497471, 9)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 10, 75497472, 83886079, 10)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 11, 83886080, 92274687, 11)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 12, 92274688, 100663295, 12)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 13, 100663296, 109051903, 13)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 14, 109051904, 117440511, 14)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 15, 117440512, 125829119, 15)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (101, 16, 125829120, 134217727, 16)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 1, 134217728, 142606335, 17)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 2, 142606336, 150994943, 18)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 3, 150994944, 159383551, 19)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 4, 159383552, 167772159, 20)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 5, 167772160, 176160767, 21)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 6, 176160768, 184549375, 22)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 7, 184549376, 192937983, 23)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 8, 192937984, 201326591, 24)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 9, 201326592, 209715199, 25)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 10, 209715200, 218103807, 26)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 11, 218103808, 226492415, 27)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 12, 226492416, 234881023, 28)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 13, 234881024, 243269631, 29)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 14, 243269632, 251658239, 30)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 15, 251658240, 260046847, 31)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (102, 16, 260046848, 268435455, 32)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 1, 268435456, 276824063, 33)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 2, 276824064, 285212671, 34)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 3, 285212672, 293601279, 35)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 4, 293601280, 301989887, 36)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 5, 301989888, 310378495, 37)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 6, 310378496, 318767103, 38)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 7, 318767104, 327155711, 39)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 8, 327155712, 335544319, 40)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 9, 335544320, 343932927, 41)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 10, 343932928, 352321535, 42)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 11, 352321536, 360710143, 43)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 12, 360710144, 369098751, 44)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 13, 369098752, 377487359, 45)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 14, 377487360, 385875967, 46)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 15, 385875968, 394264575, 47)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (103, 16, 394264576, 402653183, 48)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 1, 402653184, 411041791, 49)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 2, 411041792, 419430399, 50)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 3, 419430400, 427819007, 51)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 4, 427819008, 436207615, 52)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 5, 436207616, 444596223, 53)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 6, 444596224, 452984831, 54)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 7, 452984832, 461373439, 55)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 8, 461373440, 469762047, 56)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 9, 469762048, 478150655, 57)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 10, 478150656, 486539263, 58)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 11, 486539264, 494927871, 59)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 12, 494927872, 503316479, 60)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 13, 503316480, 511705087, 61)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 14, 511705088, 520093695, 62)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 15, 520093696, 528482303, 63)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (104, 16, 528482304, 536870911, 64)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 1, 536870912, 545259519, 65)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 2, 545259520, 553648127, 66)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 3, 553648128, 562036735, 67)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 4, 562036736, 570425343, 68)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 5, 570425344, 578813951, 69)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 6, 578813952, 587202559, 70)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 7, 587202560, 595591167, 71)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 8, 595591168, 603979775, 72)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 9, 603979776, 612368383, 73)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 10, 612368384, 620756991, 74)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 11, 620756992, 629145599, 75)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 12, 629145600, 637534207, 76)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 13, 637534208, 645922815, 77)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 14, 645922816, 654311423, 78)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 15, 654311424, 662700031, 79)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (105, 16, 662700032, 671088639, 80)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 1, 671088640, 679477247, 81)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 2, 679477248, 687865855, 82)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 3, 687865856, 696254463, 83)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 4, 696254464, 704643071, 84)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 5, 704643072, 713031679, 85)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 6, 713031680, 721420287, 86)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 7, 721420288, 729808895, 87)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 8, 729808896, 738197503, 88)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 9, 738197504, 746586111, 89)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 10, 746586112, 754974719, 90)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 11, 754974720, 763363327, 91)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 12, 763363328, 771751935, 92)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 13, 771751936, 780140543, 93)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 14, 780140544, 788529151, 94)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 15, 788529152, 796917759, 95)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (106, 16, 796917760, 805306367, 96)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 1, 805306368, 813694975, 97)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 2, 813694976, 822083583, 98)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 3, 822083584, 830472191, 99)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 4, 830472192, 838860799, 100)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 5, 838860800, 847249407, 101)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 6, 847249408, 855638015, 102)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 7, 855638016, 864026623, 103)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 8, 864026624, 872415231, 104)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 9, 872415232, 880803839, 105)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 10, 880803840, 889192447, 106)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 11, 889192448, 897581055, 107)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 12, 897581056, 905969663, 108)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 13, 905969664, 914358271, 109)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 14, 914358272, 922746879, 110)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 15, 922746880, 931135487, 111)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (107, 16, 931135488, 939524095, 112)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 1, 939524096, 947912703, 113)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 2, 947912704, 956301311, 114)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 3, 956301312, 964689919, 115)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 4, 964689920, 973078527, 116)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 5, 973078528, 981467135, 117)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 6, 981467136, 989855743, 118)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 7, 989855744, 998244351, 119)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 8, 998244352, 1006632959, 120)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 9, 1006632960, 1015021567, 121)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 10, 1015021568, 1023410175, 122)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 11, 1023410176, 1031798783, 123)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 12, 1031798784, 1040187391, 124)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 13, 1040187392, 1048575999, 125)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 14, 1048576000, 1056964607, 126)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 15, 1056964608, 1065353215, 127)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (108, 16, 1065353216, 1073741823, 128)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 1, 0, 8388607, 1)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 2, 8388608, 16777215, 2)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 3, 16777216, 25165823, 3)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 4, 25165824, 33554431, 4)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 5, 33554432, 41943039, 5)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 6, 41943040, 50331647, 6)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 7, 50331648, 58720255, 7)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 8, 58720256, 67108863, 8)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 9, 67108864, 75497471, 9)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 10, 75497472, 83886079, 10)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 11, 83886080, 92274687, 11)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 12, 92274688, 100663295, 12)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 13, 100663296, 109051903, 13)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 14, 109051904, 117440511, 14)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 15, 117440512, 125829119, 15)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (201, 16, 125829120, 134217727, 16)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 1, 134217728, 142606335, 17)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 2, 142606336, 150994943, 18)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 3, 150994944, 159383551, 19)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 4, 159383552, 167772159, 20)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 5, 167772160, 176160767, 21)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 6, 176160768, 184549375, 22)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 7, 184549376, 192937983, 23)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 8, 192937984, 201326591, 24)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 9, 201326592, 209715199, 25)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 10, 209715200, 218103807, 26)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 11, 218103808, 226492415, 27)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 12, 226492416, 234881023, 28)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 13, 234881024, 243269631, 29)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 14, 243269632, 251658239, 30)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 15, 251658240, 260046847, 31)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (202, 16, 260046848, 268435455, 32)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 1, 268435456, 276824063, 33)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 2, 276824064, 285212671, 34)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 3, 285212672, 293601279, 35)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 4, 293601280, 301989887, 36)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 5, 301989888, 310378495, 37)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 6, 310378496, 318767103, 38)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 7, 318767104, 327155711, 39)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 8, 327155712, 335544319, 40)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 9, 335544320, 343932927, 41)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 10, 343932928, 352321535, 42)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 11, 352321536, 360710143, 43)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 12, 360710144, 369098751, 44)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 13, 369098752, 377487359, 45)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 14, 377487360, 385875967, 46)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 15, 385875968, 394264575, 47)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (203, 16, 394264576, 402653183, 48)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 1, 402653184, 411041791, 49)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 2, 411041792, 419430399, 50)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 3, 419430400, 427819007, 51)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 4, 427819008, 436207615, 52)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 5, 436207616, 444596223, 53)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 6, 444596224, 452984831, 54)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 7, 452984832, 461373439, 55)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 8, 461373440, 469762047, 56)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 9, 469762048, 478150655, 57)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 10, 478150656, 486539263, 58)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 11, 486539264, 494927871, 59)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 12, 494927872, 503316479, 60)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 13, 503316480, 511705087, 61)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 14, 511705088, 520093695, 62)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 15, 520093696, 528482303, 63)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (204, 16, 528482304, 536870911, 64)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 1, 536870912, 545259519, 65)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 2, 545259520, 553648127, 66)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 3, 553648128, 562036735, 67)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 4, 562036736, 570425343, 68)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 5, 570425344, 578813951, 69)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 6, 578813952, 587202559, 70)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 7, 587202560, 595591167, 71)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 8, 595591168, 603979775, 72)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 9, 603979776, 612368383, 73)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 10, 612368384, 620756991, 74)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 11, 620756992, 629145599, 75)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 12, 629145600, 637534207, 76)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 13, 637534208, 645922815, 77)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 14, 645922816, 654311423, 78)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 15, 654311424, 662700031, 79)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (205, 16, 662700032, 671088639, 80)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 1, 671088640, 679477247, 81)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 2, 679477248, 687865855, 82)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 3, 687865856, 696254463, 83)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 4, 696254464, 704643071, 84)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 5, 704643072, 713031679, 85)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 6, 713031680, 721420287, 86)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 7, 721420288, 729808895, 87)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 8, 729808896, 738197503, 88)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 9, 738197504, 746586111, 89)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 10, 746586112, 754974719, 90)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 11, 754974720, 763363327, 91)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 12, 763363328, 771751935, 92)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 13, 771751936, 780140543, 93)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 14, 780140544, 788529151, 94)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 15, 788529152, 796917759, 95)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (206, 16, 796917760, 805306367, 96)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 1, 805306368, 813694975, 97)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 2, 813694976, 822083583, 98)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 3, 822083584, 830472191, 99)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 4, 830472192, 838860799, 100)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 5, 838860800, 847249407, 101)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 6, 847249408, 855638015, 102)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 7, 855638016, 864026623, 103)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 8, 864026624, 872415231, 104)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 9, 872415232, 880803839, 105)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 10, 880803840, 889192447, 106)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 11, 889192448, 897581055, 107)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 12, 897581056, 905969663, 108)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 13, 905969664, 914358271, 109)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 14, 914358272, 922746879, 110)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 15, 922746880, 931135487, 111)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (207, 16, 931135488, 939524095, 112)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 1, 939524096, 947912703, 113)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 2, 947912704, 956301311, 114)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 3, 956301312, 964689919, 115)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 4, 964689920, 973078527, 116)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 5, 973078528, 981467135, 117)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 6, 981467136, 989855743, 118)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 7, 989855744, 998244351, 119)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 8, 998244352, 1006632959, 120)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 9, 1006632960, 1015021567, 121)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 10, 1015021568, 1023410175, 122)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 11, 1023410176, 1031798783, 123)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 12, 1031798784, 1040187391, 124)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 13, 1040187392, 1048575999, 125)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 14, 1048576000, 1056964607, 126)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 15, 1056964608, 1065353215, 127)
GO
INSERT [dbo].[PartLimits208] ([sliceNum], [partitionNum], [minLim], [maxLim], [ordinal]) VALUES (208, 16, 1065353216, 1073741823, 128)
GO
