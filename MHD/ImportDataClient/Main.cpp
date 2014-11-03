
#include "GenerateNativeSql.h"

int main(int argc, char* argv[])
{
	if (argc == 10)
	{
		int num_procs;
		int ID;

		if (MPI_Init(&argc, &argv) != MPI_SUCCESS)
		{
			printf("Error initializing MPI!");
		}

		MPI_Comm_size(MPI_COMM_WORLD, &num_procs);
		MPI_Comm_rank(MPI_COMM_WORLD, &ID);

		GenerateNativeSql gns = GenerateNativeSql(argv, num_procs, ID);
		gns.Run(ID);
		
		MPI_Finalize();
	}
	else if (argc == 2)
	{
		int num_procs;
		int ID;

		if (MPI_Init(&argc, &argv) != MPI_SUCCESS)
		{
			printf("Error initializing MPI!");
		}

		MPI_Comm_size(MPI_COMM_WORLD, &num_procs);
		MPI_Comm_rank(MPI_COMM_WORLD, &ID);

		GenerateNativeSql gns = GenerateNativeSql(argv[1], num_procs, ID);
		printf ("calling Run with ID %i\n", ID);
		gns.Run(ID);
		
		//printf("%i: Calling MPI_Finalize...\n", ID);
		MPI_Finalize();
	}
	else
	{
		printf("Usage: ImportData <path> <prefix> <components> <time_start> <time_end> <timeinc> <timeoff> <firstbox> <lastbox>\n");
		printf("Timesteps [time_start,time_end] (inclusive) are imported, with an increment of <timeinc>, loaded as time+timeoff.\n");
		printf("Blocks in the range [firstbox, lastbox] are imported, where firstbox and lastbox are specified by their Morton keys.\n");
		//printf("Press any key to continue\n");
		//getchar();
	}

	//printf("Press Enter to finish.");
	//getchar();
	return 0;
}

void udt_test()
{
	UDTSOCKET client = UDT::socket(AF_INET, SOCK_STREAM, 0);

	sockaddr_in serv_addr;
	serv_addr.sin_family = AF_INET;
	serv_addr.sin_port = htons(10021);
	//inet_pton(AF_INET, "127.0.0.1", &serv_addr.sin_addr);
	long serv_addr_ip = 127 << 24 | 0 << 16 | 0 << 8 | 1;
	serv_addr.sin_addr.s_addr = htonl(serv_addr_ip);

	//hostent* p_h_ent;
	//hostent  h_ent;
	//p_h_ent = gethostbyname("ugrad1.cs.jhu.edu");
	//if (p_h_ent == NULL)
	//{
	//	printf("gethostbyname() error...\n");
	//	exit(1);
	//}
	//memcpy(&h_ent, p_h_ent, sizeof(h_ent));
	//memcpy(&serv_addr_ip, h_ent.h_addr_list[0], sizeof(serv_addr_ip));
	//serv_addr.sin_addr.S_un.S_addr = serv_addr_ip;

	memset(&(serv_addr.sin_zero), '\0', 8);

	// connect to the server, implict bind
	if (UDT::ERROR == UDT::connect(client, (sockaddr*)&serv_addr, sizeof(serv_addr)))
	{
	  printf("connect: %s\n", UDT::getlasterror().getErrorMessage());
	  printf("connect: %i\n", UDT::getlasterror().getErrorCode());
	  printf("connect: Is the server running?\n");
	}

	char* hello = "hello world!\n";
	if (UDT::ERROR == UDT::send(client, hello, strlen(hello) + 1, 0))
	{
	  printf("send: %s\n", UDT::getlasterror().getErrorMessage());
	  printf("send: %i\n", UDT::getlasterror().getErrorCode());
	  printf("send: Is the server running?\n");
	}

	UDT::close(client);
}
