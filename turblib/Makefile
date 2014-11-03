# $Id: Makefile,v 1.9 2009-12-01 19:23:49 eric Exp $

#	Copyright 2011 Johns Hopkins University
#
#   Licensed under the Apache License, Version 2.0 (the "License");
#   you may not use this file except in compliance with the License.
#   You may obtain a copy of the License at
#
#       http://www.apache.org/licenses/LICENSE-2.0
#
#   Unless required by applicable law or agreed to in writing, software
#   distributed under the License is distributed on an "AS IS" BASIS,
#   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#   See the License for the specific language governing permissions and
#   limitations under the License.

define HDF5_ERROR

Error in make!
An hdf5 installation is required for working with cutoutfiles and h5cc was not found!
Please, edit the Makefile with the installation directory of hdf5
endef

OSARCH := $(shell uname -sp)

ifeq ($(OSARCH),Darwin i386)
# Compile both 32- and 64-bit code under MacOS X for Intel
# ARCH_FLAGS = -arch i386 -arch x86_64
else
	ARCH_FLAGS =
endif

RM     = rm -f
CFLAGS = -Wall
LDLIBS =
CP     = cp
MKDIR  = mkdir -p

ifeq ($(CUTOUT_SUPPORT), 1)
#If you built HDF5 from source yourself, fill in the path to your HDF5 installation
   H5DIR  = /usr/local/HDF5
   H5INC  = $(H5DIR)/include
   H5CC   = $(H5DIR)/bin/h5cc
   H5FC   = $(H5DIR)/bin/h5fc
   CC     = $(H5CC) -g $(ARCH_FLAGS)
   FC     = $(H5FC) $(ARCH_FLAGS)
   CFLAGS += -D CUTOUT_SUPPORT -I$(H5INC)
   ifeq ($(wildcard $(H5CC)),)
      $(error $(HDF5_ERROR))
   endif
else
   CC     = gcc -g $(ARCH_FLAGS)
   FC     = gfortran $(ARCH_FLAGS)
endif

OBJ =	soapC.o \
	soapClient.o \
	stdsoap2.o \
        turblib.o

all: turbc turbf mhdc mhdf channelc channelf mixingc mixingf

mhdc : $(OBJ) mhdc.o
	 $(CC) -o $@ $(OBJ) mhdc.o $(LDLIBS)

mhdc.o: compiler_flags

turbc : $(OBJ) turbc.o
	 $(CC) -o $@ $(OBJ) turbc.o $(LDLIBS)

turbc.o: compiler_flags

turbf : $(OBJ) turbf.o
	 $(FC) -o $@ $(OBJ) turbf.o $(LDLIBS)

turbf.o : turbf.f90
	 $(FC) -c turbf.f90

mhdf : $(OBJ) mhdf.o
	 $(FC) -o $@ $(OBJ) mhdf.o $(LDLIBS)

mhdf.o : mhdf.f90
	 $(FC) -c mhdf.f90

channelc : $(OBJ) channelc.o
	 $(CC) -o $@ $(OBJ) channelc.o $(LDLIBS)

channelc.o: compiler_flags

channelf : $(OBJ) channelf.o
	 $(FC) -o $@ $(OBJ) channelf.o $(LDLIBS)

channelf.o : channelf.f90
	 $(FC) -c channelf.f90

mixingc : $(OBJ) mixingc.o
	 $(CC) -o $@ $(OBJ) mixingc.o $(LDLIBS)

mixingc.o: compiler_flags

mixingf : $(OBJ) mixingf.o
	 $(FC) -o $@ $(OBJ) mixingf.o $(LDLIBS)

mixingf.o : mixingf.f90
	 $(FC) -c mixingf.f90

stdsoap2.o: stdsoap2.c
	$(CC) $(CFLAGS) -c $<

static_lib: $(OBJ)
	ar rcs libJHTDB.a $(OBJ)

install: static_lib
	$(MKDIR) $(JHTDB_PREFIX)/include
	$(MKDIR) $(JHTDB_PREFIX)/lib
	$(CP) *.h $(JHTDB_PREFIX)/include/
	$(CP) libJHTDB.a $(JHTDB_PREFIX)/lib/

# Regenerate the gSOAP interfaces if required
TurbulenceService.h : wsdl

# Update the WSDL and gSOAP interfaces
wsdl:
	wsdl2h -o TurbulenceService.h -n turb -c "http://turbulence.pha.jhu.edu/service/turbulence.asmx?WSDL" -s
	soapcpp2 -CLcx -2 TurbulenceService.h

testwsdl:
	wsdl2h -o TurbulenceService.h -n turb -c "http://test.turbulence.pha.jhu.edu/service/turbulence.asmx?WSDL" -s
	soapcpp2 -CLcx -2 TurbulenceService.h

mhdtestwsdl:
	wsdl2h -o TurbulenceService.h -n turb -c "http://mhdtest.turbulence.pha.jhu.edu/service/turbulence.asmx?WSDL" -s
	soapcpp2 -CLcx -2 TurbulenceService.h

devwsdl:
	wsdl2h -o TurbulenceService.h -n turb -c "http://dev.turbulence.pha.jhu.edu/service/turbulence.asmx?WSDL" -s
	soapcpp2 -CLcx -2 TurbulenceService.h

mhddevwsdl:
	wsdl2h -o TurbulenceService.h -n turb -c "http://mhddev.turbulence.pha.jhu.edu/service/turbulence.asmx?WSDL" -s
	soapcpp2 -CLcx -2 TurbulenceService.h

prodtestwsdl:
	wsdl2h -o TurbulenceService.h -n turb -c "http://prodtest.turbulence.pha.jhu.edu/service/turbulence.asmx?WSDL" -s
	soapcpp2 -CLcx -2 TurbulenceService.h

clean:
	$(RM) *.o *.exe turbf turbc mhdc mhdf channelc channelf mixingc mixingf compiler_flags

spotless: clean
	$(RM) soapClient.c TurbulenceServiceSoap.nsmap soapH.h TurbulenceServiceSoap12.nsmap soapStub.h soapC.c TurbulenceService.h

.SUFFIXES: .o .c .x

.c.o:
	$(CC) $(CFLAGS) -c $<

.PHONY: force
compiler_flags: force
	echo '$(CFLAGS)' | cmp -s - $@ || echo '$(CFLAGS)' > $@

$(OBJ): compiler_flags

