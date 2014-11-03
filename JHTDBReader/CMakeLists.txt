cmake_minimum_required(VERSION 2.8)

PROJECT(vtkJHTDBReader)



FIND_PACKAGE(ParaView REQUIRED)
#FIND_PACKAGE(VTK REQUIRED)


set (SRC_LIST
	vtkJHTDBReader.h
	vtkJHTDBReaderTest.cxx	
	vtkJHTDBReader.cxx)

include(${VTK_USE_FILE})



# CXX flags are modified in order to enable the /clr flag
STRING(REPLACE "/EHsc" "/EHa" CMAKE_CXX_FLAGS ${CMAKE_CXX_FLAGS})
STRING(REPLACE "/RTC1" "" CMAKE_CXX_FLAGS_DEBUG ${CMAKE_CXX_FLAGS_DEBUG})
# /clr = common language runtime flag
# for building code with (microsoft) managed objects
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /clr")

add_executable(vtkJHTDBReaderTest ${SRC_LIST})


if(VTK_LIBRARIES)

	target_link_libraries(vtkJHTDBReaderTest ${VTK_LIBRARIES})

else()
  
	target_link_libraries(vtkJHTDBReaderTest vtkHybrid)

endif()



INCLUDE(${PARAVIEW_USE_FILE})

ADD_PARAVIEW_PLUGIN(
  vtkJHTDBReader 	# Name for the plugin
  "1.0"		# Version string
  SERVER_MANAGER_XML vtkJHTDBReader.xml	# Server manager xml
  SERVER_MANAGER_SOURCES vtkJHTDBReader.cxx	# Source files for the new classes
  REQUIRED_ON_SERVER
)