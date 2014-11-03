
#include "GenerateNativeSql.h"

#define HEADER

GenerateNativeSql::GenerateNativeSql(void)
{
	time_start = 1;
	time_end = 1;
	time_inc = 1;
	timeoff = 0;
	firstBox = Morton3D(0);
	lastBox = Morton3D(1073741312);

	data_dir = "";
	prefix = "V";
	tableName = "data";
	resolution = 1024;
	cube_resolution = 8;
	slices = 128;
	components = 3;
	edge = 0;
}

GenerateNativeSql::GenerateNativeSql(char* argv[], int num_procs, int ID)
{
	resolution = 1024;
	cube_resolution = 8;
	slices = 128;
	components = 3;
	edge = 0;

	data_dir = argv[1];
	prefix = argv[2];
	components = atoi(argv[3]);
	time_start = atoi(argv[4]);
	time_end = atoi(argv[5]);
	time_inc = atoi(argv[6]);
	timeoff = atoi(argv[7]);

#ifndef WIN32
	long long fBoxKey = atoll(argv[8]);
	long long lBoxKey = atoll(argv[9]);
#else
	//atoll() is not supported in Visual C++, and we must use _atoi64()
	long long fBoxKey = _atoi64(argv[8]);
	long long lBoxKey = _atoi64(argv[9]);
#endif
	firstBox.Key(fBoxKey);
	lastBox.Key(lBoxKey);

	this->cache = FileCache(data_dir, prefix, resolution, resolution/slices, components, ID);

	//We need to determine the range of cubes that this process will be responsible for
	int partitions[3] = {1,1,1}; //This array stores the number of times the data will be split along z,y,x
	int index = 0;
	while (num_procs != 1)
	{
		num_procs = num_procs/2;
		partitions[index] *= 2;
		index++;
		index = index % 3;
	}

	if (ID >= partitions[0] * partitions[1] * partitions[2])
	{
		printf("Only processes that are a power of 2 are supported!\n");
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}

	int fBoxValues[3] = {-1,-1,-1};
	firstBox.GetValues(fBoxValues);
	int lBoxValues[3] = {-1,-1,-1};
	lastBox.GetValues(lBoxValues);

	//the ID of the process determines, which cubes it is responsible for
	//the processes are first divided according to the number of times the data is split along z
	int myID = ID;
	int z = ID % partitions[0];
	ID = ID / partitions[0];
	//the z-values of the first and last boxes are adjusted based on the width of the partition region
	int zwidth = (lBoxValues[0] + cube_resolution - fBoxValues[0])/partitions[0];
	fBoxValues[0] += z * zwidth;
	lBoxValues[0] -= ((partitions[0] -1) - z) * zwidth;

	//the processes are then divided according to the number of times the data is split along y
	int y = ID % partitions[1];
	ID = ID / partitions[1];
	//the y-values of the first and last boxes are adjusted based on the width of the partition region
	int ywidth = (lBoxValues[1] + cube_resolution - fBoxValues[1])/partitions[1];
	fBoxValues[1] += y * ywidth;
	lBoxValues[1] -= ((partitions[1] -1) - y) * ywidth;

	//finally, the processes are first divided according to the number of times the data is split along x
	int x = ID % partitions[2];
	ID = ID / partitions[2];
	//the x-values of the first and last boxes are adjusted based on the width of the partition region
	int xwidth = (lBoxValues[2] + cube_resolution - fBoxValues[2])/partitions[2];
	fBoxValues[2] += x * xwidth;
	lBoxValues[2] -= ((partitions[2] -1) - x) * xwidth;

	CheckValues(myID, fBoxValues, lBoxValues);

	firstBox = Morton3D(fBoxValues[0], fBoxValues[1], fBoxValues[2]);
	lastBox = Morton3D(lBoxValues[0], lBoxValues[1], lBoxValues[2]);
}

GenerateNativeSql::GenerateNativeSql(char * filename, int num_procs, int ID)
{
	char keyword[BUF_SIZE];
	FILE *fp;
	fp = fopen(filename,"r");
	if (fp == NULL)
	{
		printf("Error: could not open file...\n");
		exit(1);
	}
	int nKeywords = 0;

	while(fscanf(fp," #%s",keyword) == 1){
		nKeywords++;
		
		//We try to read out the parameters from the config file
		
		if(!strcmp(keyword, "resolution"))
		{
			if (fscanf(fp, " %i", &resolution) != 1)
				printf("Error: failed to parse resoltuion...\n");
		}
		else if(!strcmp(keyword, "cube_resolution"))
		{
			if (fscanf(fp, " %i", &cube_resolution) != 1)
				printf("Error: failed to parse cube_resolution...\n");
		}
		else if(!strcmp(keyword, "slices"))
		{
			if (fscanf(fp, " %i", &slices) != 1)
				printf("Error: failed to parse slices...\n");
		}
		else if(!strcmp(keyword, "components"))
		{
			if (fscanf(fp, " %i", &components) != 1)
				printf("Error: failed to parse components...\n");
		}
		else if(!strcmp(keyword, "edge"))
		{
			if (fscanf(fp, " %i", &edge) != 1)
				printf("Error: failed to parse edge...\n");
		}
		else if(!strcmp(keyword, "data_dir"))
		{
			char dir[BUF_SIZE];
			//get the space character
			fgetc(fp);
			//get the directory path
			if (fgets(dir, BUF_SIZE, fp) == NULL)
				printf("Error: failed to parse data_dir...\n");
			else
			{
				char *p;
				if ((p = strchr(dir, '\n')) != NULL)
				  *p = '\0';
				data_dir = dir;
				data_dir.erase(data_dir.length() - 1);
			}
		}
		else if(!strcmp(keyword, "prefix"))
		{
			char pref[BUF_SIZE];
			//get the space character
			fgetc(fp);
			//get the prefix
			if (fgets(pref, BUF_SIZE, fp) == NULL)
				printf("Error: failed to parse prefix...\n");
			else
			{
				char *p;
				if ((p = strchr(pref, '\n')) != NULL)
				  *p = '\0';
				prefix = pref;
				prefix.erase(prefix.length() - 1);
			}
		}
		else if(!strcmp(keyword, "time_start"))
		{
			if (fscanf(fp, " %i", &time_start) != 1)
				printf("Error: failed to parse time_start...\n");
		}
		else if(!strcmp(keyword, "time_end"))
		{
			if (fscanf(fp, " %i", &time_end) != 1)
				printf("Error: failed to parse time_end...\n");
		}
		else if(!strcmp(keyword, "time_inc"))
		{
			if (fscanf(fp, " %i", &time_inc) != 1)
				printf("Error: failed to parse time_inc...\n");
		}
		else if(!strcmp(keyword, "timeoff"))
		{
			if (fscanf(fp, " %i", &timeoff) != 1)
				printf("Error: failed to parse timeoff...\n");
		}
		else if(!strcmp(keyword, "firstBox"))
		{
			long long fBoxKey;
			if (fscanf(fp, " %lli", &fBoxKey) != 1)
				printf("Error: failed to parse firstBox...\n");
			else
				firstBox.Key(fBoxKey);
		}
		else if(!strcmp(keyword, "lastBox"))
		{
			long long lBoxKey;
			if (fscanf(fp, " %lli", &lBoxKey) != 1)
				printf("Error: failed to parse lastBox...\n");
			else
				lastBox.Key(lBoxKey);
		}
		else if(!strcmp(keyword, "nServers"))
		{
			int nServers;
			if (fscanf(fp, " %i", &nServers) != 1)
				printf("Error: failed to parse the number of servers...\n");
			else
			{
				if (nServers < num_procs)
				{
					printf("The number of servers is less than the number of processes running! This is not yet supported!\n");
					MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
					exit(1);
				}

				serv_addr = new sockaddr_in[nServers];
				for (int i = 0; i < nServers; i++)
				{
					int ip[4], port;
					if (fscanf(fp, "%i.%i.%i.%i:%i", &ip[0], &ip[1], &ip[2], &ip[3], &port) != 5)
						printf("Error: failed to parse ip and port of server %i...\n", i);
					else
					{
						serv_addr[i].sin_family = AF_INET;
						serv_addr[i].sin_port = htons(port);
						long serv_addr_ip = ip[0] << 24 | ip[1] << 16 | ip[2] << 8 | ip[3];
						//In Visual C++ use the following:
						//serv_addr[i].sin_addr.S_un.S_addr = htonl(serv_addr_ip);
						serv_addr[i].sin_addr.s_addr = htonl(serv_addr_ip);
						memset(&(serv_addr[i].sin_zero), '\0', 8);
					}
				}
			}
		}
		else
			printf("Unrecognized parameter...\n");
	}

	if (nKeywords < 14)
	{
		printf("Error: not all parameters were specified in the config file...\n");
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}

	this->cache = FileCache(data_dir, prefix, resolution, resolution/slices, components, ID);

	long long fBoxKey = firstBox.Key();
	long long lBoxKey = lastBox.Key();
	if ((lBoxKey + cube_resolution - fBoxKey) % num_procs != 0)
	{
		printf("Invalid input! The z-index range is not evenly divisible by the number of processes!\n");
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}
	int cube_size = cube_resolution * cube_resolution * cube_resolution;
	long long zIndexRangePerProc = (lBoxKey + cube_size - fBoxKey)/num_procs;
	firstBox.Key(fBoxKey + zIndexRangePerProc * ID);
	fBoxKey = firstBox.Key();
	lastBox.Key(fBoxKey + zIndexRangePerProc - cube_size);

	int fBoxValues[3] = {-1,-1,-1};
	firstBox.GetValues(fBoxValues);
	int lBoxValues[3] = {-1,-1,-1};
	lastBox.GetValues(lBoxValues);
	CheckValues(ID, fBoxValues, lBoxValues);

	/*
	//We need to determine the range of cubes that this process will be responsible for
	int partitions[3] = {1,1,1}; //This array stores the number of times the data will be split along z,y,x
	int index = 0;
	while (num_procs != 1)
	{
		num_procs = num_procs/2;
		partitions[index] *= 2;
		index++;
		index = index % 3;
	}

	if (ID >= partitions[0] * partitions[1] * partitions[2])
	{
		printf("Only processes that are a power of 2 are supported!\n");
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}

	//the ID of the process determines, which cubes it is responsible for
	//the processes are divided according to the number of times the data is split along x
	int myID = ID;
	int x = ID % partitions[2];
	ID = ID / partitions[2];
	//the x-values of the first and last boxes are adjusted based on the width of the partition region
	int xwidth = (lBoxValues[2] + cube_resolution - fBoxValues[2])/partitions[2];
	fBoxValues[2] += x * xwidth;
	lBoxValues[2] -= ((partitions[2] -1) - x) * xwidth;

	//the processes are then divided according to the number of times the data is split along y
	int y = ID % partitions[1];
	ID = ID / partitions[1];
	//the y-values of the first and last boxes are adjusted based on the width of the partition region
	int ywidth = (lBoxValues[1] + cube_resolution - fBoxValues[1])/partitions[1];
	fBoxValues[1] += y * ywidth;
	lBoxValues[1] -= ((partitions[1] -1) - y) * ywidth;

	//the processes are then divided according to the number of times the data is split along z
	int z = ID % partitions[0];
	ID = ID / partitions[0];
	//the z-values of the first and last boxes are adjusted based on the width of the partition region
	int zwidth = (lBoxValues[0] + cube_resolution - fBoxValues[0])/partitions[0];
	fBoxValues[0] += z * zwidth;
	lBoxValues[0] -= ((partitions[0] -1) - z) * zwidth;

	CheckValues(myID, fBoxValues, lBoxValues);

	firstBox = Morton3D(fBoxValues[0], fBoxValues[1], fBoxValues[2]);
	lastBox = Morton3D(lBoxValues[0], lBoxValues[1], lBoxValues[2]);
	*/
}

GenerateNativeSql::~GenerateNativeSql(void)
{
	if (serv_addr)
	{
		delete [] serv_addr;
	}
}

void GenerateNativeSql::Run(int ID)
{
  time_t start, end;
  long total = 0, send_time = 0;

	if (UDT::ERROR == UDT::startup())
	{
		printf("startup: %s\n", UDT::getlasterror().getErrorMessage());
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}

	UDTSOCKET usock = UDT::socket(AF_INET, SOCK_DGRAM, 0);
	//Send in non-blocking mode
	//bool block = false;
	//UDT::setsockopt(usock, 0, UDT_SNDSYN, &block, sizeof(bool));
	
	//Increase the maximum packet size
	int mssSize = NETWORK_MTU;
	UDT::setsockopt(usock, 0, UDT_MSS, &mssSize, sizeof(int));
	
	//Increase the size of the sending buffer
	//int bufSize = UDT_BUFSIZE;
	//UDT::setsockopt(usock, 0, UDT_SNDBUF, &bufSize, sizeof(int));
	
	//Increase the linger time
	//linger l;
	//l.l_onoff = 1;
	//l.l_linger = 180;
	//UDT::setsockopt(usock, 0, UDT_LINGER, &l, sizeof(l));

	if (UDT::ERROR == UDT::connect(usock, (sockaddr*)&serv_addr[ID], sizeof(serv_addr[ID])))
	{
		printf("connect: %s\n", UDT::getlasterror().getErrorMessage());
		printf("connect: %i\n", UDT::getlasterror().getErrorCode());
		printf("connect: Is the server running? Is the port available?\n");
	}
	else
	  {
	    printf("%i: connected\n", ID);
	  }

	#ifndef WIN32
	//    pthread_create(new pthread_t, NULL, monitor, &usock);
	#else
	//    CreateThread(NULL, 0, monitor, &client, 0, NULL);
	#endif

	dataInfo di;

	//int X_resolution = (lastBox.X() - firstBox.X() + cube_resolution);
	//int Y_resolution = (lastBox.Y() - firstBox.Y() + cube_resolution);
	//int Z_resolution = (lastBox.Z() - firstBox.Z() + cube_resolution);
	//long long msgs = ( X_resolution / cube_resolution ) * ( Y_resolution / cube_resolution ) * ( Z_resolution / cube_resolution );
	long long msgs = (lastBox.Key() - firstBox.Key()) / (cube_resolution * cube_resolution * cube_resolution) + 1;

	int cube_size = components * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * sizeof(float);

	int msg_size = 0;
	int record_size = 0;
	if (cube_size > 8000)
	{
	  record_size = SIZEOF_int + SIZEOF_bigint + SIZEOF_bigint + cube_size;
	  // Add the size of the SqlArrayHeader and the list of rank lengths
#ifdef HEADER
	  record_size += sizeof(SqlArrayHeader<int>) + 8 * SIZEOF_int;
#endif
	}
	else if (cube_size > 1) 
	{
	  record_size = SIZEOF_int + SIZEOF_bigint + SIZEOF_short + cube_size;
	  // Add the size of the SqlArrayHeader and the list of rank lengths
#ifdef HEADER
	  //record_size += sizeof(SqlArrayHeader<int>) + 3 * SIZEOF_short;
	  //record_size += components > 1 ? SIZEOF_short : 0;
	  record_size += sizeof(SqlArrayHeader<int>) + 8 * SIZEOF_short;
#endif
	}
	else
	{
	  record_size = SIZEOF_int + SIZEOF_bigint + components * SIZEOF_real;
	}
	msg_size = record_size;

	while (msg_size < MIN_MSG_SIZE)
	{
	  msg_size *= 2;
	  msgs /= 2;
	}

	for (int timeStep = time_start; timeStep <= time_end; timeStep += time_inc)
	{
	  start = time(NULL);
	  
	  di.processID = ID;
	  di.numMsgs = msgs;
	  di.MsgSize = msg_size;
	  memcpy(&di.prefix, this->prefix.c_str(), 1);
	  di.timestep = timeStep - timeoff;
	  di.firstBox = this->firstBox.Key();
	  di.lastBox = this->lastBox.Key();
	  di.recordSize = record_size;
	  
	  SendDataInfo(usock, di);
	  
	  if (cube_resolution > 1)
	  {
	    SendData(timeStep, timeoff, usock, send_time);
	  }
	  else
	  {
	    SendData_SinglePoint(timeStep, timeoff, usock);
	  }
	  
	  end = time(NULL);
	  total += (long)(end - start);
	  printf("%i: Time step [%i] Elapsed time is %ld seconds...\n", ID, timeStep, (long)(end - start));
	  printf("%i: Time step [%i] I/O time is %f seconds...\n", ID, timeStep, cache.io_time);
	  printf("%i: Time step [%i] Sending time is %ld seconds...\n", ID, timeStep, send_time);
	}

	printf("%i: Total running time is %ld seconds...\n", ID, total);

	di.processID = ID;
	di.numMsgs = 0;
	di.MsgSize = -1;
	memcpy(&di.prefix, this->prefix.c_str(), 1);
	di.timestep = -1;
	di.firstBox = -1;
	di.lastBox = -1;
	di.recordSize = -1;

	SendDataInfo(usock, di);

	char *msg = new char[BUF_SIZE];
	if (UDT::ERROR == UDT::recvmsg(usock, msg, BUF_SIZE))
	  printf("recv from server: %s\n", UDT::getlasterror().getErrorMessage());
	else
	  printf("msg from server: %s\n", msg);

	if (UDT::ERROR == UDT::close(usock))
	  printf("close: %s\n", UDT::getlasterror().getErrorMessage());
	else
	  printf("Closed the connection with the server...\n");

	if (UDT::ERROR == UDT::cleanup())
	  printf("cleanup: %s\n", UDT::getlasterror().getErrorMessage());
}

//TODO: This function has not been updated to include the SqlArrayHeader
void GenerateNativeSql::WriteFile(int timestep, int ID)
{
	char *filename = new char[BUF_SIZE];
	sprintf(filename, "%i_%s_t%i_fB%lli_lB%lli", ID, prefix.c_str(), timestep, firstBox.Key(), lastBox.Key());
	//On Windows the file needs to be open as binary, as otherwise whenever a "new line" character is to be written to the file
	//a "carriage return" character will also be written
	FILE *fd = fopen(filename,"ab");
	
	int X_resolution = (lastBox.X() - firstBox.X() + cube_resolution);
	int Y_resolution = (lastBox.Y() - firstBox.Y() + cube_resolution);
	int Z_resolution = (lastBox.Z() - firstBox.Z() + cube_resolution);

	size_t records = ( X_resolution / cube_resolution ) * ( Y_resolution / cube_resolution ) * ( Z_resolution / cube_resolution );

	int cube_size = components * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * sizeof(float);
	
	int record_size = 0;
	if (cube_size > 8000) 
	{
	  record_size = SIZEOF_int + SIZEOF_bigint + SIZEOF_bigint + cube_size;
	}
	else
	{
	  record_size = SIZEOF_int + SIZEOF_bigint + SIZEOF_short + cube_size;
	}
	size_t dataToWrite = records * record_size;
	unsigned char * data;

	try
	{
		data = new unsigned char [dataToWrite];
	}
	catch (bad_alloc&)
	{
		printf("Could not allocate enough memory for the data array!\n");
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}

	//We repeatedly call FileCache's GetData() method for the first box
	//until the call is successfull, i.e. the array to hold the cached data cube is allocated
	int base[3] = {-1,-1,-1};
	firstBox.GetValues(base);
	while(cache.GetData(timestep, base, X_resolution, Y_resolution, Z_resolution, cube_resolution, edge) == 0)
	{
		if (dataToWrite == record_size)
		{
			printf("Could not allocate enough memory for file cache and the data array to be written to file! Consider reducing the size of the data cube to be cached!\n");
			MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
			exit(1);
		}
		else{
			printf("Could not allocate memory for the file cache! Reducing the size of the data to write!\n");
			delete [] data;
			dataToWrite /= 2;
			data = new unsigned char [dataToWrite];
		}
	}

	int dataOffset = 0;
	//we iterate from the first cube to the last (inclusive)
	for (long long i = firstBox.Key(); i <= lastBox.Key(); i += cube_resolution * cube_resolution * cube_resolution)
	{
		Morton3D(i).GetValues(base);
		cache.GetData(timestep, base, X_resolution, Y_resolution, Z_resolution, cube_resolution, edge);

		//this is the representation of a record in the native SQL format for SQL Server
		memcpy(data + dataOffset, &timestep, SIZEOF_int);
		dataOffset += SIZEOF_int;
		memcpy(data + dataOffset, &i, SIZEOF_bigint);
		dataOffset += SIZEOF_bigint;
		//the representation is different if the cube_size is bigger than 8000
		if (cube_size > 8000) {
		  long long cube_size2 = (long long)cube_size;
		  memcpy(data + dataOffset, &cube_size2, SIZEOF_bigint);
		  dataOffset += SIZEOF_bigint;
		}
		else {
		  memcpy(data + dataOffset, &cube_size, SIZEOF_short);
		  dataOffset += SIZEOF_short;
		}
		CopyData(data, cube_size, base[2], base[1], base[0], dataOffset);
		dataOffset += cube_size;

		if (dataOffset == dataToWrite)
		{
			size_t bytes_written = fwrite(data, 1, dataToWrite, fd);
			if (bytes_written < dataToWrite)
				perror("Error: writing to \"native sql\" file");
			dataOffset = 0;
		}
	}

	delete [] data;
	fclose(fd);
}

void GenerateNativeSql::SendData(int timestep, int timeoff, const UDTSOCKET &usock, long &send_time)
{
  time_t start, end;

        int X_resolution = (lastBox.X() - firstBox.X() + cube_resolution);
	int Y_resolution = (lastBox.Y() - firstBox.Y() + cube_resolution);
	int Z_resolution = (lastBox.Z() - firstBox.Z() + cube_resolution);
        //int X_resolution = 512;
        //int Y_resolution = 512;
        //int Z_resolution = 256;

	int db_timestep = timestep - timeoff;

#ifdef HEADER
	int SqlArrayHeaderSize = 0;
	unsigned char * header = NULL;

	int cube_size = components * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * sizeof(float);

	GetSqlArrayHeader(cube_size, SqlArrayHeaderSize, header);
#else
	int cube_size = components * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * sizeof(float);
#endif

	int record_size = 0;
	if (cube_size > 8000) 
	{
#ifdef HEADER
	  record_size = SqlArrayHeaderSize + SIZEOF_int + SIZEOF_bigint + SIZEOF_bigint + cube_size;
#else
	  record_size = SIZEOF_int + SIZEOF_bigint + SIZEOF_bigint + cube_size;
#endif
	}
	else
	{
#ifdef HEADER
	  record_size = SqlArrayHeaderSize + SIZEOF_int + SIZEOF_bigint + SIZEOF_short + cube_size;
#else
	  record_size = SIZEOF_int + SIZEOF_bigint + SIZEOF_short + cube_size;
#endif
	}

	int dataToSend = record_size;
	while (dataToSend < MIN_MSG_SIZE)
	  dataToSend *= 2;

	unsigned char * data = new unsigned char [dataToSend];

	int base[3] = {-1,-1,-1};

	int dataOffset = 0;
	//we iterate from the first cube to the last (inclusive)
	for (long long i = firstBox.Key(); i <= lastBox.Key(); i += cube_resolution * cube_resolution * cube_resolution)
	{
		Morton3D(i).GetValues(base);
		if (cache.GetData(timestep, base, X_resolution, Y_resolution, Z_resolution, cube_resolution, edge) == 0)
		{
			printf("Could not allocate memory for the file cache!\n");
			MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
			exit(1);
		}

		//this is the representation of a record in the native SQL format for SQL Server
		memcpy(data + dataOffset, &db_timestep, SIZEOF_int);
		dataOffset += SIZEOF_int;
		memcpy(data + dataOffset, &i, SIZEOF_bigint);
		dataOffset += SIZEOF_bigint;
		//the representation is different if the cube_size is bigger than 8000
		if (cube_size > 8000) {
#ifdef HEADER
			long long cube_size2 = (long long)(cube_size + SqlArrayHeaderSize);
			memcpy(data + dataOffset, &cube_size2, SIZEOF_bigint);
			dataOffset += SIZEOF_bigint;
#else
			memcpy(data + dataOffset, &cube_size, SIZEOF_bigint);
			dataOffset += SIZEOF_bigint;
#endif
		}
		else {
#ifdef HEADER
			short cube_size2 = (short)(cube_size + SqlArrayHeaderSize);
			memcpy(data + dataOffset, &cube_size2, SIZEOF_short);
			dataOffset += SIZEOF_short;
#else
			memcpy(data + dataOffset, &cube_size, SIZEOF_short);
			dataOffset += SIZEOF_short;
#endif
		}
#ifdef HEADER
		memcpy(data + dataOffset, header, SqlArrayHeaderSize);
		dataOffset += SqlArrayHeaderSize;
#endif

		CopyData(data, cube_size, base[2], base[1], base[0], dataOffset);
		dataOffset += cube_size;

		if (dataOffset == dataToSend)
		{
		  int ssize = 0;
		  start = time(NULL);
		  if (UDT::ERROR == (ssize = UDT::sendmsg(usock, (char *)data, dataToSend, -1, true)))
		  {
		    printf("send: %s\n", UDT::getlasterror().getErrorMessage());
		    MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		    exit(1);
		  }
		  end = time(NULL);
		  send_time += (long)(end - start);
		  //printf("Sent record %lli..\n", i/(cube_resolution * cube_resolution * cube_resolution));
		  dataOffset = 0;
		}
	}

	delete [] data;
}

void GenerateNativeSql::SendData_SinglePoint(int timestep, int timeoff, const UDTSOCKET &usock)
{
	int X_resolution = (lastBox.X() - firstBox.X() + cube_resolution);
	int Y_resolution = (lastBox.Y() - firstBox.Y() + cube_resolution);
	int Z_resolution = (lastBox.Z() - firstBox.Z() + cube_resolution);

	int db_timestep = timestep - timeoff;
	
	int record_size = SIZEOF_int + SIZEOF_bigint + components * SIZEOF_real;

	int dataToSend = record_size;
	while (dataToSend < MIN_MSG_SIZE)
	  dataToSend *= 2;

	unsigned char * data = new unsigned char [dataToSend];

	int base[3] = {-1,-1,-1};
	int sourceIndex[3] = {-1,-1,-1};

	int dataOffset = 0;
	//we iterate from the first cube to the last (inclusive)
	for (long long i = firstBox.Key(); i <= lastBox.Key(); i += cube_resolution * cube_resolution * cube_resolution)
	{
		Morton3D(i).GetValues(base);
		if (cache.GetData(timestep, base, X_resolution, Y_resolution, Z_resolution, cube_resolution, edge) == 0)
		{
			printf("Could not allocate memory for the file cache!\n");
			MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
			exit(1);
		}

		//this is the representation of a record in the native SQL format for SQL Server
		memcpy(data + dataOffset, &db_timestep, SIZEOF_int);
		dataOffset += SIZEOF_int;
		memcpy(data + dataOffset, &i, SIZEOF_bigint);
		dataOffset += SIZEOF_bigint;

		sourceIndex[0] = base[2] - cache.GetZBaseX();
		sourceIndex[1] = base[1] - cache.GetZBaseY();
		sourceIndex[2] = base[0] - cache.GetZBaseZ();
		for (int j = 0; j < components; j++)
		{
		    memcpy(data + dataOffset, cache.data[j] + (sourceIndex[0] +
			   cache.XResolution() * sourceIndex[1] + 
			   cache.XResolution() * cache.YResolution() * sourceIndex[2]) * sizeof(float), 
			   SIZEOF_real);
		    dataOffset += SIZEOF_real;
		}

		if (dataOffset == dataToSend)
		{
		  int ssize = 0;
		  if (UDT::ERROR == (ssize = UDT::sendmsg(usock, (char *)data, dataToSend, -1, true)))
		  {
		      printf("send: %s\n", UDT::getlasterror().getErrorMessage());
		      MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		      exit(1);
		  }
		  dataOffset = 0;
		}
	}

	delete [] data;
}

void GenerateNativeSql::CopyData(unsigned char * data, int cube_size, int X, int Y, int Z, int destinationIndex)
{
  // Version that store values such as Vx, Vy, Vz together
  int sourceIndex_x = X - cache.GetZBaseX();
  int sourceIndex_y = Y - cache.GetZBaseY();
  int sourceIndex_z = Z - cache.GetZBaseZ();
  int sourceIndex0 = sourceIndex_x + 
    (cache.XResolution() + 2 * edge) * sourceIndex_y +
    (cache.XResolution() + 2 * edge) * (cache.YResolution() + 2 * edge) * 
		      sourceIndex_z;

  for (int k = 0; k < cube_resolution + 2 * edge; k++)
    {
    int sourceIndex1 = sourceIndex0 + 
      (cache.XResolution() + 2 * edge) * (cache.YResolution() + 2 * edge) * k;
    for (int j = 0; j < cube_resolution + 2 * edge; j++)
      {
	int sourceIndex = sourceIndex1 + 
	  (cache.XResolution() + 2 * edge) * j;
	sourceIndex *= sizeof(float);
       for (int i = 0; i < cube_resolution + 2 * edge; i++)
	 {
	   for (int c = 0; c < components; c++)
	     {
	       memcpy(data + destinationIndex, 
		      cache.data[c] + sourceIndex,
		      sizeof(float));
	       destinationIndex += sizeof(float);
	     }
	   sourceIndex += sizeof(float);
	 }
      }
    }


  //Version that stores an array for Vx, 
  //then an array for Vy, then an array for Vz
  /*    
  for (int i = 0; i < components; i++)
    {
      int bytesCopied = 0;
      int count = 0;
      int sourceIndex_x = X - cache.GetZBaseX();
      int sourceIndex_y = Y - cache.GetZBaseY();
      int sourceIndex_z = Z - cache.GetZBaseZ();
      
      while (bytesCopied < cube_size / components)
	{
	  // Assign bytes for the cached data cube along x for the appropriate component.
	  memcpy(data + destinationIndex, 
		 cache.data[i] + (sourceIndex_x + 
				  (cache.XResolution() + 2 * edge) * sourceIndex_y + 
				  (cache.XResolution() + 2 * edge) * (cache.YResolution() + 2 * edge) * sourceIndex_z) * sizeof(float), 
		 (cube_resolution + 2 * edge) * sizeof(float));
	  destinationIndex += (cube_resolution + 2 * edge) * sizeof(float);
	  // Adjust the array index, so that we start from the beginning for the next row (along y).
	  sourceIndex_y += 1;
	  count++;
	  // Once we have assigned enough bytes for an entire sheet, move to the next (along z).
	  // We also need to adjust the y-index to start from the beginning.
	  if (count == (cube_resolution + 2 * edge))
	    {
	      sourceIndex_z++;
	      sourceIndex_y  = Y - cache.GetZBaseY();
	      count = 0;
	    }
	  bytesCopied += (cube_resolution + 2 * edge) * sizeof(float);
	}
    }
  */
}

void GenerateNativeSql::SendDataInfo(const UDTSOCKET &usock, const dataInfo &di)
{
	char *data = new char [sizeof(dataInfo)];
	memcpy(data, &di, sizeof(dataInfo));
	int ssize;
	if (UDT::ERROR == (ssize = UDT::sendmsg(usock, data, sizeof(dataInfo), -1, true)))
	{
		printf("send: %s\n", UDT::getlasterror().getErrorMessage());
		MPI_Abort(MPI_COMM_WORLD, MPI_ERR_UNKNOWN);
		exit(1);
	}
	printf("Sent data info sturcture...\n");
}

void GenerateNativeSql::CheckValues(int ID, int fBoxValues[3], int lBoxValues[3])
{
	int x_width = lBoxValues[2] + cube_resolution - fBoxValues[2];
	int y_width = lBoxValues[1] + cube_resolution - fBoxValues[1];
	int z_width = lBoxValues[0] + cube_resolution - fBoxValues[0];
	if ((fBoxValues[0] != 0 && ((fBoxValues[0] & -fBoxValues[0]) != fBoxValues[0])) ||
		(fBoxValues[1] != 0 && ((fBoxValues[1] & -fBoxValues[1]) != fBoxValues[1])) ||
		(fBoxValues[2] != 0 && ((fBoxValues[2] & -fBoxValues[2]) != fBoxValues[2])))
		printf("%i: The first Box values are not a power of 2: [%i, %i, %i]\n", ID, fBoxValues[0], fBoxValues[1], fBoxValues[2]);
	if ((((lBoxValues[0] + cube_resolution) & -(lBoxValues[0] + cube_resolution)) != lBoxValues[0] + cube_resolution) ||
	    (((lBoxValues[1] + cube_resolution) & -(lBoxValues[1] + cube_resolution)) != lBoxValues[1] + cube_resolution) ||
	    (((lBoxValues[2] + cube_resolution) & -(lBoxValues[2] + cube_resolution)) != lBoxValues[2] + cube_resolution))
		printf("%i: The last Box values are not a power of 2: [%i, %i, %i]\n", ID, lBoxValues[0], lBoxValues[1], lBoxValues[2]);
	if (((x_width & -(x_width)) != x_width) ||
		((y_width & -(y_width)) != y_width) ||
		((z_width & -(z_width)) != z_width))
		printf("%i: One of the sides of the Box to be processed is not a power of 2: [%i, %i, %i]\n", ID, z_width, y_width, x_width);
}

void GenerateNativeSql::GetSqlArrayHeader(int cube_size, int &headerSize, unsigned char* &header)
{
	SqlArrayHeader<int> sqlHeader;
	if (cube_size > 8000)
	{
		int cube_width = cube_resolution + 2*edge;
		sqlHeader.HeaderType = 1;
		sqlHeader.ColumnMajor = 0;
		// The data are in single precision floating point format
		// For double the DataType is 6
		sqlHeader.DataType = 5;
		//We have 3-dimensional arrays of either scalar or vector fields
		sqlHeader.Rank =  components == 1 ? 3 : 4;
		sqlHeader.Reserved2 = 0;
		sqlHeader.Length = components * (cube_width * cube_width * cube_width);
		headerSize = sizeof(SqlArrayHeader<int>) + MAX_SQLARRAY_RANK*sizeof(int);
		header = new unsigned char[headerSize];
		memset(header, 0, headerSize);
		memcpy(header, &sqlHeader, sizeof(SqlArrayHeader<int>));
		//We have a 3 dimensional cube with width equal to cube_resolution + 2*edge
		if (components > 1)
		  memcpy(header + sizeof(SqlArrayHeader<int>) + 0*sizeof(int), &components, sizeof(int));
		memcpy(header + sizeof(SqlArrayHeader<int>) + 1*sizeof(int), &cube_width, sizeof(int));
		memcpy(header + sizeof(SqlArrayHeader<int>) + 2*sizeof(int), &cube_width, sizeof(int));
		memcpy(header + sizeof(SqlArrayHeader<int>) + 3*sizeof(int), &cube_width, sizeof(int));
	}
	else
	{
	        //short cube_width = cube_resolution + 2*edge;
	        int cube_width = cube_resolution + 2*edge;
		sqlHeader.HeaderType = 1;
		sqlHeader.ColumnMajor = 0;
		sqlHeader.Reserved = 0;
		// The data are in single precision floating point format
		// For double the DataType is 6
		sqlHeader.DataType = 5;
		//We have 3-dimensional arrays of either scalar or vector fields
		sqlHeader.Rank =  components == 1 ? 3 : 4;
		sqlHeader.Reserved2 = 0;
		sqlHeader.Length = components * (cube_width * cube_width * cube_width);
		//headerSize = sizeof(SqlArrayHeader<int>) + sqlHeader.Rank*sizeof(short);
		headerSize = sizeof(SqlArrayHeader<int>) + MAX_SQLARRAY_RANK*sizeof(short);
		header = new unsigned char[headerSize];
		memset(header, 0, headerSize);
		memcpy(header, &sqlHeader, sizeof(SqlArrayHeader<int>));
		//We have a 3 dimensional cube with width equal to cube_resolution + 2*edge
		if (components > 1)
		  {
		    memcpy(header + sizeof(SqlArrayHeader<int>), &components, sizeof(short));
		    memcpy(header + sizeof(SqlArrayHeader<int>) + 1*sizeof(short), &cube_width, sizeof(short));
		    memcpy(header + sizeof(SqlArrayHeader<int>) + 2*sizeof(short), &cube_width, sizeof(short));
		    memcpy(header + sizeof(SqlArrayHeader<int>) + 3*sizeof(short), &cube_width, sizeof(short));
		  }
		else
		  {
		    memcpy(header + sizeof(SqlArrayHeader<int>), &cube_width, sizeof(short));
		    memcpy(header + sizeof(SqlArrayHeader<int>) + 1*sizeof(short), &cube_width, sizeof(short));
		    memcpy(header + sizeof(SqlArrayHeader<int>) + 2*sizeof(short), &cube_width, sizeof(short));
		  }
		
	}
}

#ifndef WIN32
void* monitor(void *s)
#else
DWORD WINAPI monitor(LPVOID s)
#endif
{
  UDTSOCKET u = *(UDTSOCKET*)s;
  UDT::TRACEINFO perf;

  while(true)
    {
#ifndef WIN32
      sleep(2);
#else
      Sleep(1000);
#endif
      if (UDT::ERROR == UDT::perfmon(u, &perf))
	{
	  cout << "perfom: " << UDT::getlasterror().getErrorMessage() << endl;
	  break;
	}


      cout <<"SendRate(Mb/s)\tRTT(ms)\tCWnd\tPktSndPeriod(us)\tRecvACK\tRecvNACK"<<endl;
      cout << perf.mbpsSendRate << "\t\t"
	   << perf.msRTT << "\t"
	   << perf.pktCongestionWindow << "\t"
	   << perf.usPktSndPeriod << "\t\t\t"
	   << perf.pktRecvACK << "\t"
	   << perf.pktRecvNAK << endl;
    }

#ifndef WIN32
  return NULL;
#else
  return 0;
#endif
}
