C++ = mpiCC

ifndef os
   os = LINUX
endif

ifndef arch
   arch = IA32
endif

CCFLAGS = -Wall -D$(os) -I/home/kalin/udt4/src -O2 -finline-functions

ifeq ($(arch), IA32)
   CCFLAGS += -DIA32 #-mcpu=pentiumpro -march=pentiumpro -mmmx -msse
endif

ifeq ($(arch), POWERPC)
   CCFLAGS += -mcpu=powerpc
endif

ifeq ($(arch), IA64)
   CCFLAGS += -DIA64
endif

LDFLAGS = -L/home/kalin/udt4/src -ludt #-lstdc++ -lpthread -lm

ifeq ($(os), UNIX)
   LDFLAGS += -lsocket
endif

OBJS = Main.o

DIR = $(shell pwd)

all: importDataClient

%.o: %.cpp
	$(C++) $(CCFLAGS) $< -c

importDataClient: Main.o Morton3D.o FileCache.o GenerateNativeSql.o
	$(C++) $^ -o $@ $(LDFLAGS)

clean:
	rm -f *.o
	rm -f importDataClient

install:
	export PATH=$(DIR):$$PATH
