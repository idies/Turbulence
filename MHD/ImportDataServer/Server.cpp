
#include "udt.h"
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

using namespace std;
using namespace System;
using namespace System::Runtime::InteropServices;

const static int UDT_BUFSIZE = 104857600;	//1616379904;
const static int NETWORK_MTU = 4096;
const static int MAX_ALLOC = 1650000;		//1048576;
static int PORT = 10021;
static string DBSERVER = "localhost";
static string DBNAME = "mhddb";
static string TABLENAME = "velocity";

char *data;
size_t dataToWrite;

#pragma pack(1)
struct dataInfo {
	int processID;
	long long numMsgs;
	int msgSize;
	char prefix;
	int timestep;
	long long firstBox;
	long long lastBox;
	int recordSize;
};

dataInfo GetDataInfo(const UDTSOCKET &recver);

void GetData(const UDTSOCKET &recver, const dataInfo &di);

void GetInsertData(const UDTSOCKET &recver, const dataInfo &di);

void GetInsertData_wDataReader(const UDTSOCKET &recver, const dataInfo &di, long &insert_time, long &recv_time);

char* GetFileName(const dataInfo &di);

char* GetConnectionString();

int main(int argc, char* argv[])
{
	if (argc != 5) {
		printf("Usage: ImportDataServer.exe [DBSERVER] [DBNAME] [TABLENAME] [PORT]\n");
		return 1;
	}
	else {
		DBSERVER = argv[1];
		DBNAME = argv[2];
		TABLENAME = argv[3];
		PORT = atoi(argv[4]);
	}

	data = NULL;
	long total_insert_time, insert_time, total_recv_time, recv_time;

	if (UDT::ERROR == UDT::startup())
	{
		printf("startup: %s\n", UDT::getlasterror().getErrorMessage());
		return 1;
	}

	UDTSOCKET serv = UDT::socket(AF_INET, SOCK_DGRAM, 0);

	sockaddr_in my_addr;
	my_addr.sin_family = AF_INET;
	my_addr.sin_port = htons(PORT);
	my_addr.sin_addr.s_addr = INADDR_ANY;
	memset(&(my_addr.sin_zero), '\0', 8);

	if (UDT::ERROR == UDT::bind(serv, (sockaddr*)&my_addr, sizeof(my_addr)))
	{
		printf("bind: %s\n", UDT::getlasterror().getErrorMessage());
		return 1;
	}

	printf("Server is ready at port: %i\n", PORT);

	UDT::listen(serv, 10);

	int namelen;
	sockaddr_in their_addr;

	//the server runs forever and continues to listen for new connections
	while(true)
	{
		UDTSOCKET recver = UDT::accept(serv, (sockaddr*)&their_addr, &namelen);

		//Increase the maximum packet size
		int msgSize = NETWORK_MTU;
		UDT::setsockopt(recver, 0, UDT_MSS, &msgSize, sizeof(int));

		//Increase the size of the receiving buffer
		//int bufSize = UDT_BUFSIZE;
		//UDT::setsockopt(recver, 0, UDT_RCVBUF, &bufSize, sizeof(int));

		printf("new connection: %s:%hi\n", inet_ntoa(their_addr.sin_addr), ntohs(their_addr.sin_port));

		total_insert_time = 0;
		total_recv_time = 0;

		while(true){
			dataInfo di = GetDataInfo(recver);

			//Check if the data transfer size is 0 and if so exit the while loop
			//this indicates that the data transfer has completed
			if (di.numMsgs == 0)
				break;
			else
			{
				//GetData(recver, di);
				//GetInsertData(recver, di);
				insert_time = 0;
				recv_time = 0;
				GetInsertData_wDataReader(recver, di, insert_time, recv_time);
				printf("Bulk insert time is %ld seconds...\n", insert_time);
				printf("Receiving time is %ld seconds...\n", recv_time);
				total_insert_time += insert_time;
				total_recv_time += recv_time;
			}
		}
		printf("Total bulk insert time is %ld seconds...\n", total_insert_time);
		printf("Total receiving time is %ld seconds...\n", total_recv_time);
		printf("Data transfer finished...\n");

		char* msg = "Good bye!\n";
		if (UDT::ERROR == UDT::sendmsg(recver, msg, strlen(msg) + 1))
			printf("send: %s\n", UDT::getlasterror().getErrorMessage());

		UDT::close(recver);

		delete [] data;
		data = NULL;
	}

	UDT::close(serv);

	if (UDT::ERROR == UDT::cleanup())
	{
		printf("cleanup: %s\n", UDT::getlasterror().getErrorMessage());
		return 1;
	}

	printf("Press Enter to finish.");
	getchar();
	return 0;
} 

dataInfo GetDataInfo(const UDTSOCKET &recver)
{
	dataInfo di;
	char * data;
	data = new char [sizeof(dataInfo)];

	int rsize = 0;
	if (UDT::ERROR == (rsize = UDT::recvmsg(recver, data, sizeof(dataInfo))))
	{
		printf("recv: %s\n", UDT::getlasterror().getErrorMessage());
		printf("recv: %i\n", UDT::getlasterror().getErrorCode());
		di.processID = -1;
		di.numMsgs = 0;
		di.msgSize = -1;
		di.prefix = '\0';
		di.timestep = -1;
		di.firstBox = -1;
		di.lastBox = -1;
		di.recordSize = -1;
		delete [] data;
		return di;
	}
	printf("recv: Received dataInfo structure of %i bytes...\n", rsize);

	memcpy(&di, data, sizeof(dataInfo));
	delete [] data;

	printf("\tProcess ID of sender: %i\n", di.processID);
	printf("\tNumber of Messages to receive: %lli\n", di.numMsgs);
	printf("\tMsg size: %i\n", di.msgSize);
	printf("\tData Prefix: %c\n", di.prefix);
	printf("\tTimestep: %i\n", di.timestep);
	printf("\tMorton index of the first box: %lli\n", di.firstBox);
	printf("\tMorton index of the last box: %lli\n", di.lastBox);
	printf("\tThe size of a record is: %i\n", di.recordSize);
	return di;
}

void GetData(const UDTSOCKET &recver, const dataInfo &di)
{
	bool flag = true;
	char *recvBuffer = new char [di.msgSize];

	if (!data)
	{
		long long allocSize = di.numMsgs * di.msgSize;
		
		//Maybe we could check the amount of available memory, and use that as the allocation size
		while (allocSize > MAX_ALLOC && allocSize > 2 * di.msgSize)
			allocSize /= 2;
		dataToWrite = allocSize;
	
		if (dataToWrite % di.msgSize != 0)
			printf("recv: The number of msgs to write is not a power of 2! This may cause problems!\n");

		while (flag)
		{
			try
			{
				data = new char [dataToWrite];
				flag = false;
			}
			catch (bad_alloc&)
			{
				printf("Could not allocate enough memory for the receiving data array! Reducing the allocation size!\n");
				dataToWrite /= 2;
				flag = true;
			}
		}
	}

	long long rmsgs = 0;
	long long dataOffset = 0;
	int rsize = 0;

	FILE *fp;
	char * filename = GetFileName(di);
	fp = fopen(filename, "wb");

	while (rmsgs < di.numMsgs)
	{
		if (UDT::ERROR == (rsize = UDT::recvmsg(recver, recvBuffer, di.msgSize)))
		{
			printf("recv: %s\n", UDT::getlasterror().getErrorMessage());
			printf("recv: %i\n", UDT::getlasterror().getErrorCode());
			exit(1);
		}
		memcpy(data + dataOffset, recvBuffer, di.msgSize);
		//printf("recv: Received msg %lli...\n", rmsgs);
		rmsgs++;
		dataOffset += di.msgSize;
		
		if (dataOffset == dataToWrite)
		{
			size_t bytes_written = fwrite(data, 1, dataToWrite, fp);
			if (bytes_written < dataToWrite)
				perror("Error: writing to \"native sql\" file...\n");
			else if (rmsgs % (di.numMsgs / 8) == 0)
				printf("recv: Wrote %lli bytes...\n", rmsgs * di.msgSize);
			dataOffset = 0;
		}
	} 

	if (dataOffset > 0)
	{
		printf("Some received data may not have been written to the file system! This should not happen for number of msgs that is a power of 2!\n");
	}

	fclose(fp);
}

void GetInsertData(const UDTSOCKET &recver, const dataInfo &di)
{
	bool flag = true;
	char *recvBuffer = new char [di.msgSize];
	array<unsigned char>^ data;

	long long allocSize = di.numMsgs * di.msgSize;
		
	//Maybe we could check the amount of available memory, and use that as the allocation size
	while (allocSize > MAX_ALLOC && allocSize > 2 * di.msgSize)
		allocSize /= 2;
	dataToWrite = allocSize;

	if (dataToWrite % di.msgSize != 0)
		printf("recv: The number of msgs to write is not a power of 2! This may cause problems!\n");

	while (flag)
	{
		try
		{
			data = gcnew array<unsigned char>(dataToWrite);
			flag = false;
		}
		catch (bad_alloc&)
		{
			printf("Could not allocate enough memory for the receiving data array! Reducing the allocation size!\n");
			dataToWrite /= 2;
			flag = true;
		}
	}

	long long rmsgs = 0;
	long long dataOffset = 0;
	int rsize = 0;

	String^ connectionString = gcnew String(GetConnectionString());
	ImportData::Database ^ db = gcnew ImportData::Database(connectionString, di.recordSize, 0);
	db->Open();

	while (rmsgs < di.numMsgs)
	{
		if (UDT::ERROR == (rsize = UDT::recvmsg(recver, recvBuffer, di.msgSize)))
		{
			printf("recv: %s\n", UDT::getlasterror().getErrorMessage());
			printf("recv: %i\n", UDT::getlasterror().getErrorCode());
			exit(1);
		}
		//memcpy(data + dataOffset, recvBuffer, di.msgSize);
		Marshal::Copy((System::IntPtr)recvBuffer, data, dataOffset, di.msgSize);
		//printf("recv: Received msg %lli...\n", rmsgs);
		rmsgs++;
		dataOffset += di.msgSize;
		
		if (dataOffset == dataToWrite)
		{
			//size_t bytes_written = fwrite(data, 1, dataToWrite, fp);
			//size_t bytes_written = db->InsertIntoTable(tableName, data, di.msgSize);
			String ^ tableName = gcnew String(TABLENAME.c_str());
			size_t bytes_written = db->InsertIntoTable(tableName, data, di.recordSize);
			if (bytes_written < dataToWrite)
			{
				perror("Error: writing to database...\n");
				exit(0);
			}
			else if (di.numMsgs <= 8 || rmsgs % (di.numMsgs / 8) == 0)
				printf("recv: Wrote %lli bytes...\n", rmsgs * di.msgSize);
			dataOffset = 0;
		}
	} 
	delete [] data;

	if (dataOffset > 0)
	{
		printf("Some received data may not have been written to the file system! This should not happen for number of msgs that is a power of 2!\n");
	}
}

void GetInsertData_wDataReader(const UDTSOCKET &recver, const dataInfo &di, long &insert_time, long &recv_time)
{
	time_t start, end;
	bool flag = true;
	char *recvBuffer = new char [di.msgSize];

	long long allocSize = di.numMsgs * di.msgSize;
		
	//Maybe we could check the amount of available memory, and use that as the allocation size
	while (allocSize > MAX_ALLOC && allocSize > 2 * di.msgSize)
		allocSize /= 2;
	dataToWrite = allocSize;

	if (dataToWrite % di.msgSize != 0)
		printf("recv: The number of msgs to write is not a power of 2! This may cause problems!\n");

	long long rmsgs = 0;
	long long dataOffset = 0;
	int rsize = 0;

	String ^ connectionString = gcnew String(GetConnectionString());
	ImportData::Database ^ db = gcnew ImportData::Database(connectionString, di.recordSize, dataToWrite);
	db->Open();

	while (rmsgs < di.numMsgs)
	{
		start = time(NULL);
		if (UDT::ERROR == (rsize = UDT::recvmsg(recver, recvBuffer, di.msgSize)))
		{
			printf("recv: %s\n", UDT::getlasterror().getErrorMessage());
			printf("recv: %i\n", UDT::getlasterror().getErrorCode());
			printf("recv: last timestep, zindex processed was (%i, %lli)\n", (int)db->dataReader[0], (long long)db->dataReader[1]);
			break;
			//exit(1);
		}
		end = time(NULL);
		recv_time += (long)(end - start);

		//memcpy(data + dataOffset, recvBuffer, di.msgSize);
		Marshal::Copy((System::IntPtr)recvBuffer, db->dataReader->data, dataOffset, di.msgSize);
		//printf("recv: Received msg %lli...\n", rmsgs);
		rmsgs++;
		dataOffset += di.msgSize;
		
		if (dataOffset == dataToWrite)
		{
			//size_t bytes_written = fwrite(data, 1, dataToWrite, fp);
			//size_t bytes_written = db->InsertIntoTable(tableName, data, di.msgSize);
			String ^ tableName = gcnew String(TABLENAME.c_str());
			start = time(NULL);
			size_t bytes_written = db->InsertIntoTable_wDataReader(tableName);
			end = time(NULL);
			insert_time += (long)(end - start);
			if (bytes_written < dataToWrite)
			{
				perror("Error: writing to database...\n");
				exit(0);
			}
			else if (di.numMsgs <= 8 || rmsgs % (di.numMsgs / 8) == 0)
				printf("recv: Wrote %lli bytes...\n", rmsgs * di.msgSize);
			dataOffset = 0;
		}
	} 

	if (dataOffset > 0)
	{
		printf("Some received data may not have been written to the file system! This should not happen for number of msgs that is a power of 2!\n");
	}
}

char* GetFileName(const dataInfo &di)
{
	char *ret = new char[100];
	sprintf(ret, "%c%05i_%lli_%lli.%03i", di.prefix, di.timestep, di.firstBox, di.lastBox, di.processID);
    return ret;
}

char* GetConnectionString()
{
	char *ret = new char[500];
	sprintf(ret, "Data Source=%s;Initial Catalog=%s;Integrated Security=True",DBSERVER.c_str(), DBNAME.c_str());
	return ret;
}