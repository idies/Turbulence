#pragma once

#ifndef WIN32
    #include <unistd.h>
    #include <cstdlib>
    #include <cstring>
    #include <netdb.h>
#else
    #include <winsock2.h>
    #include <ws2tcpip.h>
    #include <wspiapi.h>
#endif

#include <string>
#include <stdio.h>
#include <time.h>
#include <mpi.h>

#include "Morton3D.h"
#include "FileCache.h"
#include "SqlArrayHeader.h"
#include <iostream>

#include "udt.h"

using namespace std;

const static int BUF_SIZE = 500;
const static int UDT_BUFSIZE = 104857600;	//1616379904;
const static int NETWORK_MTU = 4096;
const static int SIZEOF_short = 2;
const static int SIZEOF_int = 4;
const static int SIZEOF_bigint = 8;
const static int SIZEOF_real = 4;
const static int MIN_MSG_SIZE = 4096;
const static int MAX_SQLARRAY_RANK = 8;

#pragma pack(1)
struct dataInfo {
  int processID;
  long long numMsgs;
  int MsgSize;
  char prefix;
  int timestep;
  long long firstBox;
  long long lastBox;
  int recordSize;
};
	
#ifndef WIN32
void* monitor(void *);
#else
DWORD WINAPI monitor(LPVOID);
#endif

class GenerateNativeSql
{
public:
	GenerateNativeSql(void);
	GenerateNativeSql(char* argv[], int num_procs, int ID);
	GenerateNativeSql(char * filename, int num_procs, int ID);
	~GenerateNativeSql(void);

	void Run(int ID);
	void WriteFile(int timestep, int ID);
	void SendData(int timestep, int timeoff, const UDTSOCKET &usock, long &send_time);
	void SendData_SinglePoint(int timestep, int timeoff, const UDTSOCKET &usock);
	void CopyData(unsigned char * data, int cube_size, int X, int Y, int Z, int destinationIndex);
	void SendDataInfo(const UDTSOCKET &usock, const dataInfo &di);

	void CheckValues(int ID, int fBoxValues[3], int lBoxValues[3]);

private:

	void GetSqlArrayHeader(int cube_size, int &headerSize, unsigned char* &header);

	int time_start;
	int time_end;
	int time_inc;
	int timeoff;
	Morton3D firstBox;
	Morton3D lastBox;

	string data_dir;
	string prefix;
	string tableName;
	int resolution;
	int cube_resolution;
	int slices;
	int components;
	int edge;
	FileCache cache;

	sockaddr_in* serv_addr;
};
