create partition function zindexPFN(bigint) as range left for values(822083583, 838860799, 855638015, 872415231, 889192447, 905969663, 922746879, 939524095, 956301311, 973078527, 989855743, 1006632959, 1023410175, 1040187391, 1056964607)
alter database mhddb024
			add filegroup [FG1]
alter database mhddb024
			add filegroup [FG2]
alter database mhddb024
			add filegroup [FG3]
alter database mhddb024
			add filegroup [FG4]
alter database mhddb024
			add filegroup [FG5]
alter database mhddb024
			add filegroup [FG6]
alter database mhddb024
			add filegroup [FG7]
alter database mhddb024
			add filegroup [FG8]
alter database mhddb024
			add filegroup [FG9]
alter database mhddb024
			add filegroup [FG10]
alter database mhddb024
			add filegroup [FG11]
alter database mhddb024
			add filegroup [FG12]
alter database mhddb024
			add filegroup [FG13]
alter database mhddb024
			add filegroup [FG14]
alter database mhddb024
			add filegroup [FG15]
alter database mhddb024
			add filegroup [FG16]
create partition scheme zindexPartScheme as partition zindexPFN to (FG1,FG2,FG3,FG4,FG5,FG6,FG7,FG8,FG9,FG10,FG11,FG12,FG13,FG14,FG15,FG16)
alter database mhddb024
	add file(
			name='mhddb024_FG1',
			filename='c:\data\data3\sql_db\mhddb024_FG1',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG1]
alter database mhddb024
	add file(
			name='mhddb024_FG2',
			filename='c:\data\data4\sql_db\mhddb024_FG2',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG2]
alter database mhddb024
	add file(
			name='mhddb024_FG3',
			filename='c:\data\data3\sql_db\mhddb024_FG3',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG3]
alter database mhddb024
	add file(
			name='mhddb024_FG4',
			filename='c:\data\data4\sql_db\mhddb024_FG4',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG4]
alter database mhddb024
	add file(
			name='mhddb024_FG5',
			filename='c:\data\data3\sql_db\mhddb024_FG5',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG5]
alter database mhddb024
	add file(
			name='mhddb024_FG6',
			filename='c:\data\data4\sql_db\mhddb024_FG6',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG6]
alter database mhddb024
	add file(
			name='mhddb024_FG7',
			filename='c:\data\data3\sql_db\mhddb024_FG7',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG7]
alter database mhddb024
	add file(
			name='mhddb024_FG8',
			filename='c:\data\data4\sql_db\mhddb024_FG8',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG8]
alter database mhddb024
	add file(
			name='mhddb024_FG9',
			filename='c:\data\data3\sql_db\mhddb024_FG9',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG9]
alter database mhddb024
	add file(
			name='mhddb024_FG10',
			filename='c:\data\data4\sql_db\mhddb024_FG10',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG10]
alter database mhddb024
	add file(
			name='mhddb024_FG11',
			filename='c:\data\data3\sql_db\mhddb024_FG11',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG11]
alter database mhddb024
	add file(
			name='mhddb024_FG12',
			filename='c:\data\data4\sql_db\mhddb024_FG12',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG12]
alter database mhddb024
	add file(
			name='mhddb024_FG13',
			filename='c:\data\data3\sql_db\mhddb024_FG13',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG13]
alter database mhddb024
	add file(
			name='mhddb024_FG14',
			filename='c:\data\data4\sql_db\mhddb024_FG14',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG14]
alter database mhddb024
	add file(
			name='mhddb024_FG15',
			filename='c:\data\data3\sql_db\mhddb024_FG15',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG15]
alter database mhddb024
	add file(
			name='mhddb024_FG16',
			filename='c:\data\data4\sql_db\mhddb024_FG16',
			size=400GB,
			filegrowth=100MB)
		to filegroup [FG16]
