//	Copyright 2011 Johns Hopkins University
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.


/* $Id: turblib.c,v 1.13 2009-10-23 17:58:57 eric Exp $ */
#include <stdio.h>
#include <math.h>

#ifdef CUTOUT_SUPPORT
#include "hdf5.h"
#endif//CUTOUT_SUPPORT

#include "soapH.h"
#include "TurbulenceServiceSoap.nsmap"
#include "turblib.h"

/* global gSOAP runtime environment
 * Temporary until we can figure out how to pass a pointer
 * to and from a function correctly in Fortran.
 */
struct soap __jhuturbsoap;

/* Error reporting - C */
char __turblib_err[TURB_ERROR_LENGTH];
int __turblib_errno = 0;
int __turblib_exit_on_error = 1;
int __turblib_prefetching = 0;

#ifdef CUTOUT_SUPPORT

//Linked list of all added cutout files
cutoutFile* __turblib_cutouts = NULL;

set_info DataSets[9] = {
 { 0, 0, 0 },
 { 2.0f * 3.14159265358979f / 1024.0f, .002f,  1024 }, //isotropic1024old
 { 2.0f * 3.14159265358979f / 1024.0f, .0002f,  1024 }, //isotropicfine_old
 { 2.0f * 3.14159265358979f / 1024.0f, .0025f, 1024 },  //mhd1024
 { 2.0f * 3.14159265358979f / 1024.0f, .002f,  1024 }, //isotropic1024coarse
 { 2.0f * 3.14159265358979f / 1024.0f, .0002f, 1024 }, //isotropic1024fine
 { 0, 0, 0 },
 { 2.0f * 3.14159265358979f / 1024.0f, .0025f, 1024 }, //custom_dataset
 { 2.0f * 3.14159265358979f / 1024.0f, .04f, 1024 } //mixing_dataset
};

turb_fn TurbFields[5] =
{
 { 'u', 3}, //velocity
 { 'p', 1}, //pressure
 { 'b', 3}, //magnetic
 { 'a', 3}, //vector potential
 { 'd', 1}  //density
};
#endif//CUTOUT_SUPPORT

char * turblibGetErrorString() {
  return __turblib_err;
}

int turblibGetErrorNumber() {
  return __turblib_errno;
}

void turblibPrintError() {
  fprintf(stderr, "%d: %s\n", turblibGetErrorNumber(), turblibGetErrorString());
}

void turblibSetExitOnError(int v) {
  __turblib_exit_on_error = v;
}


/* Error reporting - Fortran */
void turblibgeterrorstring_ (char *dest, int len) {
  strncpy(dest, __turblib_err, len);
}

int turblibgeterrornumber_() {
  return turblibGetErrorNumber();
}

void turblibprinterror_() {
  turblibPrintError();
}

void turblibsetexitonerror_(int *v) {
  turblibSetExitOnError(*v);
}

/* Determine appropriate error behavior */
void turblibHandleError() {
  if (__turblib_exit_on_error) {
    turblibPrintError();
    exit(1);
  }
}


/* Return the enum relating to the Fortran constant */
enum turb1__SpatialInterpolation SpatialIntToEnum(enum SpatialInterpolation spatial)
{
  switch (spatial)
  {
    case 0:
      return turb1__SpatialInterpolation__None;
    case 4:
      return turb1__SpatialInterpolation__Lag4;
    case 6:
      return turb1__SpatialInterpolation__Lag6;
    case 8:
      return turb1__SpatialInterpolation__Lag8;
    case 40:
      return turb1__SpatialInterpolation__None_USCOREFd4;
    case 44:
      return turb1__SpatialInterpolation__Fd4Lag4;
    case 60:
      return turb1__SpatialInterpolation__None_USCOREFd6;
    case 80:
      return turb1__SpatialInterpolation__None_USCOREFd8;
  case 104:
    return turb1__SpatialInterpolation__M1Q4;
  case 106:
    return turb1__SpatialInterpolation__M1Q6;
  case 108:
    return turb1__SpatialInterpolation__M1Q8;
  case 110:
    return turb1__SpatialInterpolation__M1Q10;
  case 112:
    return turb1__SpatialInterpolation__M1Q12;
  case 114:
    return turb1__SpatialInterpolation__M1Q14;
  case 204:
    return turb1__SpatialInterpolation__M2Q4;
  case 206:
    return turb1__SpatialInterpolation__M2Q6;
  case 208:
    return turb1__SpatialInterpolation__M2Q8;
  case 210:
    return turb1__SpatialInterpolation__M2Q10;
  case 212:
    return turb1__SpatialInterpolation__M2Q12;
  case 214:
    return turb1__SpatialInterpolation__M2Q14;
  case 304:
    return turb1__SpatialInterpolation__M3Q4;
  case 306:
    return turb1__SpatialInterpolation__M3Q6;
  case 308:
    return turb1__SpatialInterpolation__M3Q8;
  case 310:
    return turb1__SpatialInterpolation__M3Q10;
  case 312:
    return turb1__SpatialInterpolation__M3Q12;
  case 314:
    return turb1__SpatialInterpolation__M3Q14;
  case 404:
    return turb1__SpatialInterpolation__M4Q4;
  case 406:
    return turb1__SpatialInterpolation__M4Q6;
  case 408:
    return turb1__SpatialInterpolation__M4Q8;
  case 410:
    return turb1__SpatialInterpolation__M4Q10;
  case 412:
    return turb1__SpatialInterpolation__M4Q12;
  case 414:
    return turb1__SpatialInterpolation__M4Q14;
    default:
      return -1;
  }
  return -1;
}

/* Return the enum relating to the Fortran constant */
enum turb1__TemporalInterpolation TemporalIntToEnum(enum TemporalInterpolation temporal)
{
  switch (temporal)
  {
    case 0:
      return turb1__TemporalInterpolation__None;
    case 1:
      return turb1__TemporalInterpolation__PCHIP;
  }
  return -1;
}

/* Intialize the gSOAP runtime environment */
void soapinit_() {
  soapinit();
}

void soapinit() {
  soap_init(&__jhuturbsoap);
}

/* Destroy the gSOAP environment */
void soapdestroy_ () {
  soap_destroy(&__jhuturbsoap);
}

void soapdestroy () {
  soap_destroy(&__jhuturbsoap);
}

inline int getVelocity (char *authToken,
             char *dataset, float time,
             enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
             int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_velocity, count, datain, time, spatial, temporal))
    return getValueLocal(dataset_, turb_velocity, spatial, temporal, time, count, datain, &dataout[0][0]);

  else
#endif//CUTOUT_SUPPORT
    return getVelocitySoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVelocityAndPressure (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][4])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_vp, count, datain, time, spatial, temporal))
    return getVelocityAndPressureLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getVelocityAndPressureSoap(authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getPressure (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_pressure, count, datain, time, spatial, temporal))
    return getValueLocal(dataset_, turb_pressure, spatial, temporal, time, count, datain, &dataout[0]);

  else
#endif//CUTOUT_SUPPORT
    return getPressureSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getPressureHessian(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][6])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_pressure, count, datain, time, spatial, temporal))
    return getPressureHessianLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getPressureHessianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVelocityGradient(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][9])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_,  turb_velocity, count, datain, time, spatial, temporal))
    return getVelocityGradientLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getVelocityGradientSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVelocityHessian(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][18])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_velocity, count, datain, time, spatial, temporal))
    return getVelocityHessianLocal(dataset_, time, spatial, temporal, count, datain, dataout);
  else
#endif//CUTOUT_SUPPORT
    return getVelocityHessianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVelocityLaplacian (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_velocity, count, datain, time, spatial, temporal))
    return getVelocityLaplacianLocal (dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getVelocityLaplacianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getPressureGradient(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_pressure, count, datain, time, spatial, temporal))
    return getPressureGradientLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getPressureGradientSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getMagneticFieldGradient(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][9])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_magnetic, count, datain, time, spatial, temporal))
    return getMagneticFieldGradientLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getMagneticFieldGradientSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVectorPotentialGradient(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][9])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_potential, count, datain, time, spatial, temporal))
    return getVectorPotentialGradientLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getVectorPotentialGradientSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getMagneticFieldHessian(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][18])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_magnetic, count, datain, time, spatial, temporal))
    return getMagneticFieldHessianLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getMagneticFieldHessianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getMagneticFieldLaplacian (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_magnetic, count, datain, time, spatial, temporal))
    return getMagneticFieldLaplacianLocal (dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getMagneticFieldLaplacianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getMagneticField (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_magnetic, count, datain, time, spatial, temporal))
    return getValueLocal(dataset_, turb_magnetic, spatial, temporal, time, count, datain, &dataout[0][0]);

  else
#endif//CUTOUT_SUPPORT
    return getMagneticFieldSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVectorPotentialHessian(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][18])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_potential, count, datain, time, spatial, temporal))
    return getVectorPotentialHessianLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getVectorPotentialHessianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVectorPotentialLaplacian (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_potential, count, datain, time, spatial, temporal))
    return getVectorPotentialLaplacianLocal (dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getVectorPotentialLaplacianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getVectorPotential (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_potential, count, datain, time, spatial, temporal))
    return getValueLocal(dataset_, turb_potential, spatial, temporal, time, count, datain, &dataout[0][0]);

  else
#endif//CUTOUT_SUPPORT
    return getVectorPotentialSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getDensity (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_density, count, datain, time, spatial, temporal))
    return getValueLocal(dataset_, turb_density, spatial, temporal, time, count, datain, &dataout[0]);

  else
#endif//CUTOUT_SUPPORT
    return getDensitySoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

inline int getDensityGradient(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_density, count, datain, time, spatial, temporal))
    return getDensityGradientLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getDensityGradientSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

int getDensityHessian(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][6])
{
#ifdef CUTOUT_SUPPORT
  TurbDataset dataset_ = getDataSet(dataset);

  if (isDataAvailable(dataset_, turb_density, count, datain, time, spatial, temporal))
    return getDensityHessianLocal(dataset_, time, spatial, temporal, count, datain, dataout);

  else
#endif//CUTOUT_SUPPORT
    return getDensityHessianSoap (authToken, dataset, time, spatial, temporal, count, datain, dataout);
}

int getVelocitySoap (char *authToken,
  char *dataset, float time,
  enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetVelocity input;
  struct _turb1__GetVelocityResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVelocity(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVelocityResult->Vector3,
      output.GetVelocityResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;

  return rc;
}

int getthreshold_ (char *authToken,
		    char *dataset, char *field, float *time, float *threshold,
		    int *spatial,
		    int *X, int *Y, int *Z, int *Xwidth, int *Ywidth, int *Zwidth,
		    ThresholdInfo** dataout, int *result_size)
{
  return getThreshold(authToken, dataset, field, *time, *threshold, *spatial,
		      *X, *Y, *Z, *Xwidth, *Ywidth, *Zwidth, dataout, result_size);
}

void deallocate_array_ (ThresholdInfo **threshold_array)
{
  free(*threshold_array);
}

int getThreshold (char *authToken,
                  char *dataset, char *field, float time, float threshold,
                  enum SpatialInterpolation spatial,
                  int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
		  ThresholdInfo **dataout, int *result_size)
{
  int rc;

  struct _turb1__GetThreshold input;
  struct _turb1__GetThresholdResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.field = field;
  input.time = time;
  input.threshold = threshold;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.X = X;
  input.Y = Y;
  input.Z = Z;
  input.Xwidth = Xwidth;
  input.Ywidth = Ywidth;
  input.Zwidth = Zwidth;

  rc = soap_call___turb2__GetThreshold(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    *result_size = output.GetThresholdResult->__sizeThresholdInfo;
    *dataout = (ThresholdInfo *) malloc(sizeof(ThresholdInfo) * (*result_size));
    memcpy(*dataout, output.GetThresholdResult->ThresholdInfo,
	   output.GetThresholdResult->__sizeThresholdInfo * sizeof(ThresholdInfo));
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); //  detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}


int getboxfilter_ (char *authToken,
                   char *dataset, char *field, float *time, float *filterwidth,
                   int *count, float datain[][3], float dataout[][3],
                   int len_a, int len_d)
{
    return getBoxFilter (authToken,
                         dataset, field, *time, *filterwidth,
                         *count, datain, dataout);
}

int getBoxFilter (char *authToken,
                  char *dataset, char *field, float time, float filterwidth,
                  int count, float datain[][3], float dataout[][3])
{
    int rc;

    struct _turb1__GetBoxFilter input;
    struct _turb1__GetBoxFilterResponse output;

    input.authToken = authToken;
    input.dataset = dataset;
    input.field = field;
    input.time = time;
    input.filterwidth = filterwidth;

    struct turb1__ArrayOfPoint3 pointArray;
    pointArray.__sizePoint3 = count;
    pointArray.Point3 = (void *)datain;
    input.points = &pointArray;

    rc = soap_call___turb2__GetBoxFilter(&__jhuturbsoap, NULL, NULL, &input, &output);
    if (rc == SOAP_OK) {
        memcpy(dataout, output.GetBoxFilterResult->Vector3,
	       output.GetBoxFilterResult->__sizeVector3 * sizeof(float) * 3);
        bzero(__turblib_err, TURB_ERROR_LENGTH);
    } else {
      soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
      turblibHandleError();
    }

    soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
    soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

    __turblib_errno = rc;

    return rc;
}

int getboxfiltersgs_ (char *authToken,
                   char *dataset, char *field, float *time, float *filterwidth,
                   int *count, float datain[][3], float dataout[][6],
                   int len_a, int len_d)
{
    return getBoxFilterSGS (authToken,
                         dataset, field, *time, *filterwidth,
                         *count, datain, dataout);
}

int getBoxFilterSGS (char *authToken,
                  char *dataset, char *field, float time, float filterwidth,
                  int count, float datain[][3], float dataout[][6])
{
    int rc;

    struct _turb1__GetBoxFilterSGS input;
    struct _turb1__GetBoxFilterSGSResponse output;

    input.authToken = authToken;
    input.dataset = dataset;
    input.field = field;
    input.time = time;
    input.filterwidth = filterwidth;

    struct turb1__ArrayOfPoint3 pointArray;
    pointArray.__sizePoint3 = count;
    pointArray.Point3 = (void *)datain;
    input.points = &pointArray;

    rc = soap_call___turb2__GetBoxFilterSGS(&__jhuturbsoap, NULL, NULL, &input, &output);
    if (rc == SOAP_OK) {
        memcpy(dataout, output.GetBoxFilterSGSResult->SGSTensor,
	       output.GetBoxFilterSGSResult->__sizeSGSTensor * sizeof(float) * 6);
        bzero(__turblib_err, TURB_ERROR_LENGTH);
    } else {
      soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
      turblibHandleError();
    }

    soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
    soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

    __turblib_errno = rc;

    return rc;
}

int getboxfiltergradient_(char *authToken,
			  char *dataset, char* field, float *time,
			  float *filterwidth, float *spacing,
			  int *count, float datain[][3], float dataout[][9],
			  int len_a, int len_d)
{
  return getBoxFilterGradient(authToken,
			      dataset, field, *time,
			      *filterwidth, *spacing,
			      *count, datain, dataout);
}

int getBoxFilterGradient(char *authToken,
			char *dataset, char *field, float time,
			float filterwidth, float spacing,
			int count, float datain[][3], float dataout[][9])
{
  int rc;

  struct _turb1__GetBoxFilterGradient input;
  struct _turb1__GetBoxFilterGradientResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.field = field;
  input.time = time;
  input.filterwidth = filterwidth;
  input.spacing = spacing;

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetBoxFilterGradient (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetBoxFilterGradientResult->VelocityGradient,
      output.GetBoxFilterGradientResult->__sizeVelocityGradient * sizeof(float) * 9);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVelocityAndPressureSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][4])
{
  int rc;

  struct _turb1__GetVelocityAndPressure input;
  struct _turb1__GetVelocityAndPressureResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVelocityAndPressure (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVelocityAndPressureResult->Vector3P,
      output.GetVelocityAndPressureResult->__sizeVector3P * sizeof(float) * 4);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getPressureHessianSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][6])
{
  int rc;

  struct _turb1__GetPressureHessian input;
  struct _turb1__GetPressureHessianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetPressureHessian (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetPressureHessianResult->PressureHessian,
      output.GetPressureHessianResult->__sizePressureHessian * sizeof(float) * 6);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVelocityGradientSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][9])
{
  int rc;

  struct _turb1__GetVelocityGradient input;
  struct _turb1__GetVelocityGradientResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVelocityGradient (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVelocityGradientResult->VelocityGradient,
      output.GetVelocityGradientResult->__sizeVelocityGradient * sizeof(float) * 9);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getMagneticFieldGradientSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][9])
{
  int rc;

  struct _turb1__GetMagneticFieldGradient input;
  struct _turb1__GetMagneticFieldGradientResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetMagneticFieldGradient (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetMagneticFieldGradientResult->VelocityGradient,
      output.GetMagneticFieldGradientResult->__sizeVelocityGradient * sizeof(float) * 9);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVectorPotentialGradientSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][9])
{
  int rc;

  struct _turb1__GetVectorPotentialGradient input;
  struct _turb1__GetVectorPotentialGradientResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVectorPotentialGradient (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVectorPotentialGradientResult->VelocityGradient,
      output.GetVectorPotentialGradientResult->__sizeVelocityGradient * sizeof(float) * 9);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getPressureGradientSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetPressureGradient input;
  struct _turb1__GetPressureGradientResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetPressureGradient (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetPressureGradientResult->Vector3,
      output.GetPressureGradientResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVelocityHessianSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][18])
{
  int rc;

  struct _turb1__GetVelocityHessian input;
  struct _turb1__GetVelocityHessianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVelocityHessian (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVelocityHessianResult->VelocityHessian,
      output.GetVelocityHessianResult->__sizeVelocityHessian * sizeof(float) * 18);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVelocityLaplacianSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetVelocityLaplacian input;
  struct _turb1__GetVelocityLaplacianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVelocityLaplacian(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVelocityLaplacianResult->Vector3,
      output.GetVelocityLaplacianResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getMagneticFieldHessianSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][18])
{
  int rc;

  struct _turb1__GetMagneticHessian input;
  struct _turb1__GetMagneticHessianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetMagneticHessian (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetMagneticHessianResult->VelocityHessian,
      output.GetMagneticHessianResult->__sizeVelocityHessian * sizeof(float) * 18);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getMagneticFieldLaplacianSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetMagneticFieldLaplacian input;
  struct _turb1__GetMagneticFieldLaplacianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetMagneticFieldLaplacian(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetMagneticFieldLaplacianResult->Vector3,
      output.GetMagneticFieldLaplacianResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVectorPotentialHessianSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][18])
{
  int rc;

  struct _turb1__GetVectorPotentialHessian input;
  struct _turb1__GetVectorPotentialHessianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVectorPotentialHessian (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVectorPotentialHessianResult->VelocityHessian,
      output.GetVectorPotentialHessianResult->__sizeVelocityHessian * sizeof(float) * 18);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getVectorPotentialLaplacianSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetVectorPotentialLaplacian input;
  struct _turb1__GetVectorPotentialLaplacianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVectorPotentialLaplacian(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVectorPotentialLaplacianResult->Vector3,
      output.GetVectorPotentialLaplacianResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int nullop_ (char *authToken, int *count,
      float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return nullOp (authToken, *count,
    datain, dataout);
}

int nullOp (char *authToken, int count,
      float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__NullOp input;
  struct _turb1__NullOpResponse output;

  input.authToken = authToken;

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__NullOp(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.NullOpResult->Vector3,
      output.NullOpResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getForce(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetForce input;
  struct _turb1__GetForceResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetForce(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetForceResult->Vector3,
      output.GetForceResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getforce_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getForce (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getPosition(char *authToken,
  char *dataset, float startTime, float endTime,
  float dt,
  enum SpatialInterpolation spatial,
  int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetPosition input;
  struct _turb1__GetPositionResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.StartTime = startTime;
  input.EndTime = endTime;
  input.dt = dt;
  input.spatialInterpolation = SpatialIntToEnum(spatial);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetPosition(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetPositionResult->Point3,
      output.GetPositionResult->__sizePoint3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;

  return rc;
}

int getposition_(char *authToken,
      char *dataset, float *startTime, float *endTime,
	  float *dt,
      int *spatial,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getPosition (authToken,
    dataset, *startTime, *endTime,
	*dt,
    *spatial,
    *count, datain, dataout);
}


int getrawvelocity_(char *authToken, char *dataset, float *time,
  int *X, int *Y, int *Z, int *Xwidth, int *Ywidth, int *Zwidth,
  float dataout[])
{
    return getRawVelocity(authToken, dataset, *time, *X, *Y, *Z,
                          *Xwidth, *Ywidth, *Zwidth, (char*)dataout);
}

int getRawVelocity (char *authToken,
      char *dataset, float time,
	  int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, char dataout[])
{
  int rc;

  struct _turb1__GetRawVelocity input;
  struct _turb1__GetRawVelocityResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.X = X;
  input.Y = Y;
  input.Z = Z;
  input.Xwidth = Xwidth;
  input.Ywidth = Ywidth;
  input.Zwidth = Zwidth;

  rc = soap_call___turb2__GetRawVelocity(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetRawVelocityResult->__ptr,
      output.GetRawVelocityResult->__size );
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); //  detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getMagneticFieldSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetMagneticField input;
  struct _turb1__GetMagneticFieldResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetMagneticField(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetMagneticFieldResult->Vector3,
      output.GetMagneticFieldResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  //remove deserialized data and clean up
  soap_done(&__jhuturbsoap); //detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getrawmagneticfield_ (char *authToken,
  char *dataset, float *time,
  int *X, int *Y, int *Z, int *Xwidth, int *Ywidth, int *Zwidth,
  float dataout[])
{
  return getRawMagneticField (authToken, dataset, *time, *X, *Y, *Z,
                              *Xwidth, *Ywidth, *Zwidth, (char*) dataout);
}

int getRawMagneticField (char *authToken,
      char *dataset, float time,
	  int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, char dataout[])
{
  int rc;

  struct _turb1__GetRawMagneticField input;
  struct _turb1__GetRawMagneticFieldResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.X = X;
  input.Y = Y;
  input.Z = Z;
  input.Xwidth = Xwidth;
  input.Ywidth = Ywidth;
  input.Zwidth = Zwidth;

  rc = soap_call___turb2__GetRawMagneticField(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetRawMagneticFieldResult->__ptr,
      output.GetRawMagneticFieldResult->__size );
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); // detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getVectorPotentialSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetVectorPotential input;
  struct _turb1__GetVectorPotentialResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetVectorPotential(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetVectorPotentialResult->Vector3,
      output.GetVectorPotentialResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); //  detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getrawvectorpotential_ (char *authToken,
  char *dataset, float *time,
  int *X, int *Y, int *Z, int *Xwidth, int *Ywidth, int *Zwidth,
  float dataout[])
{
  return getRawVectorPotential (authToken, dataset, *time, *X, *Y, *Z,
                                *Xwidth, *Ywidth, *Zwidth, (char*) dataout);
}

int getRawVectorPotential (char *authToken,
      char *dataset, float time,
	  int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, char dataout[])
{
  int rc;

  struct _turb1__GetRawVectorPotential input;
  struct _turb1__GetRawVectorPotentialResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.X = X;
  input.Y = Y;
  input.Z = Z;
  input.Xwidth = Xwidth;
  input.Ywidth = Ywidth;
  input.Zwidth = Zwidth;

  rc = soap_call___turb2__GetRawVectorPotential(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetRawVectorPotentialResult->__ptr,
      output.GetRawVectorPotentialResult->__size );
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); // detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}


int getPressureSoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[])
{
  int rc;

  struct _turb1__GetPressure input;
  struct _turb1__GetPressureResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetPressure(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetPressureResult->Pressure,
      output.GetPressureResult->__sizePressure * sizeof(float));
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); // detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getrawpressure_ (char *authToken, char *dataset, float *time,
  int *X, int *Y, int *Z, int *Xwidth, int *Ywidth, int *Zwidth,
  float dataout[])
{
  return getRawPressure(authToken, dataset, *time, *X, *Y, *Z,
                        *Xwidth, *Ywidth, *Zwidth,(char*)dataout);
}

int getRawPressure (char *authToken,
      char *dataset, float time,
	  int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, char dataout[])
{
  int rc;

  struct _turb1__GetRawPressure input;
  struct _turb1__GetRawPressureResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.X = X;
  input.Y = Y;
  input.Z = Z;
  input.Xwidth = Xwidth;
  input.Ywidth = Ywidth;
  input.Zwidth = Zwidth;

  rc = soap_call___turb2__GetRawPressure(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetRawPressureResult->__ptr,
      output.GetRawPressureResult->__size );
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); // detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getDensitySoap (char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[])
{
  int rc;

  struct _turb1__GetDensity input;
  struct _turb1__GetDensityResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetDensity(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetDensityResult->Pressure,
      output.GetDensityResult->__sizePressure * sizeof(float));
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); // detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}

int getDensityGradientSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][3])
{
  int rc;

  struct _turb1__GetDensityGradient input;
  struct _turb1__GetDensityGradientResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetDensityGradient (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetDensityGradientResult->Vector3,
      output.GetDensityGradientResult->__sizeVector3 * sizeof(float) * 3);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getDensityHessianSoap(char *authToken,
      char *dataset, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float datain[][3], float dataout[][6])
{
  int rc;

  struct _turb1__GetDensityHessian input;
  struct _turb1__GetDensityHessianResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.spatialInterpolation = SpatialIntToEnum(spatial);
  input.temporalInterpolation = TemporalIntToEnum(temporal);

  struct turb1__ArrayOfPoint3 pointArray;
  pointArray.__sizePoint3 = count;
  pointArray.Point3 = (void *)datain;
  input.points = &pointArray;

  rc = soap_call___turb2__GetDensityHessian (&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetDensityHessianResult->PressureHessian,
      output.GetDensityHessianResult->__sizePressureHessian * sizeof(float) * 6);
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  /* remove deserialized data and clean up */
  soap_done(&__jhuturbsoap); /*  detach the gSOAP environment  */

  __turblib_errno = rc;
  return rc;
}

int getrawdensity_ (char *authToken, char *dataset, float *time,
  int *X, int *Y, int *Z, int *Xwidth, int *Ywidth, int *Zwidth,
  float dataout[])
{
  return getRawDensity(authToken, dataset, *time, *X, *Y, *Z,
                        *Xwidth, *Ywidth, *Zwidth,(char*)dataout);
}

int getRawDensity (char *authToken,
      char *dataset, float time,
	  int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, char dataout[])
{
  int rc;

  struct _turb1__GetRawDensity input;
  struct _turb1__GetRawDensityResponse output;

  input.authToken = authToken;
  input.dataset = dataset;
  input.time = time;
  input.X = X;
  input.Y = Y;
  input.Z = Z;
  input.Xwidth = Xwidth;
  input.Ywidth = Ywidth;
  input.Zwidth = Zwidth;

  rc = soap_call___turb2__GetRawDensity(&__jhuturbsoap, NULL, NULL, &input, &output);
  if (rc == SOAP_OK) {
    memcpy(dataout, output.GetRawDensityResult->__ptr,
      output.GetRawDensityResult->__size );
    bzero(__turblib_err, TURB_ERROR_LENGTH);
  } else {
    soap_sprint_fault(&__jhuturbsoap, __turblib_err, TURB_ERROR_LENGTH);
    turblibHandleError();
  }

  soap_end(&__jhuturbsoap);  // remove deserialized data and clean up
  soap_done(&__jhuturbsoap); // detach the gSOAP environment

  __turblib_errno = rc;

  return rc;
}



////////////////////////////////

/* Local Functions */

#ifdef CUTOUT_SUPPORT


int getValueLocal(TurbDataset dataset, TurbField func, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal, float time, int count, float position[][3], float *result)
{
  if(!validateParams(spatial, dataset, 0)) return -1;

  loadNeededData(dataset, func, count, position, time, spatial, temporal);

  int comps = TurbFields[func].comps;

  float dt = DataSets[dataset].dt;
  int timestep = (int)ceil(time/DataSets[dataset].dt - .5f);

  if(temporal == PCHIPInt)
  {
    float temp[4][3];
    int i, j;
    for (i=0; i < count; i++)
    {
      for(j = 0; j < 4; j++)
      {
        getSingleValue(dataset, func, position[i], timestep+(j-1), spatial, temp[j]);
      }
      pchipInterp(3, temp, time, timestep, dt, result + i*comps);
    }
  }
  else
  {
    int i;
    for (i = 0; i < count; i++)
    {
      getSingleValue(dataset, func, position[i], timestep, spatial, result + i*comps);
    }
  }
  freeLoadedMemory();
  return 0;
}

int getVelocityAndPressureLocal (TurbDataset dataset, float time,
  enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float datain[][3], float dataout[][4])
{
  if(!validateParams(spatial, dataset, 0)) return -1;
  float dt = DataSets[dataset].dt;
  int timestep = (int)ceil(time/DataSets[dataset].dt - .5f);

  if(temporal == PCHIPInt)
  {
    float temp[4][3];
    int i, j;
    for (i=0; i < count; i++)
    {
      for(j = 0; j < 4; j++)
      {
        getSingleValue(dataset, turb_vp, datain[i], timestep+(j-1), spatial, temp[j]);
      }
      pchipInterp(4, temp, time, timestep, dt, dataout[i]);
    }
  }
  else
  {
    float temp[4];
    int i;
    for (i = 0; i < count; i++)
    {
      getSingleValue(dataset, turb_vp, datain[i], timestep, spatial, temp);
      dataout[i][0] = temp[0];
      dataout[i][1] = temp[1];
      dataout[i][2] = temp[2];
      dataout[i][3] = temp[3];
    }
  }
  return 0;
}



int getPressureHessianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][6])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getHessian(dataset, turb_pressure, time, spatial, temporal, count, input, &output[0][0]);
}

int getVelocityGradientLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][9])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getGradient(dataset, turb_velocity, time, spatial, temporal, count, input, &output[0][0]);
}

int getMagneticFieldGradientLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][9])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getGradient(dataset, turb_magnetic, time, spatial, temporal, count, input, &output[0][0]);
}

int getVectorPotentialGradientLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][9])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getGradient(dataset, turb_potential, time, spatial, temporal, count, input, &output[0][0]);
}

int getPressureGradientLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][3])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getGradient(dataset, turb_pressure, time, spatial, temporal, count, input, &output[0][0]);
}

int getVelocityHessianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][18])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getHessian(dataset, turb_velocity, time, spatial, temporal, count, input, &output[0][0]);
}

int getVelocityLaplacianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[][3], float output[][3])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getLaplacian (dataset, turb_velocity, time, spatial, temporal, count, input, output);
}

int getMagneticFieldHessianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][18])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getHessian(dataset, turb_magnetic, time, spatial, temporal, count, input, &output[0][0]);
}

int getMagneticFieldLaplacianLocal(TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[][3], float output[][3])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getLaplacian(dataset, turb_magnetic, time, spatial, temporal, count, input, output);
}

int getVectorPotentialHessianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][18])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getHessian(dataset, turb_potential, time, spatial, temporal, count, input, &output[0][0]);
}

int getVectorPotentialLaplacianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float datain[][3], float dataout[][3])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getLaplacian(dataset, turb_potential, time, spatial, temporal, count, datain, dataout);
}


int getDensityGradientLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][3])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getGradient(dataset, turb_density, time, spatial, temporal, count, input, &output[0][0]);
}


int getDensityHessianLocal (TurbDataset dataset, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float output[count][6])
{
  if(!validateParams(spatial, dataset, 1)) return -1;
  return getHessian(dataset, turb_density, time, spatial, temporal, count, input, &output[0][0]);
}


/* HDF5 file utility functions */


int turblibaddlocalsource_ (char *fname)
{
  return turblibAddLocalSource (fname);
}

int turblibAddLocalSource(char *fname)
{
  fprintf(stderr, "opening %s\n", fname);
  hid_t file = H5Fopen(fname, H5F_ACC_RDONLY, H5P_DEFAULT);
  if (file < 0) return -1;

  cutoutFile *src = malloc(sizeof(cutoutFile));
  src->next = NULL;
  src->file = file;

  //Determine which set
  int dataset;
  hid_t set_name = H5Dopen1(file, "_dataset");
  H5Dread(set_name, H5T_NATIVE_INT, H5S_ALL, H5S_ALL, H5P_DEFAULT, &dataset);

  src->dataset = dataset;

  //Determine bounds
  int start[4];
  hid_t set_start = H5Dopen1(file, "_start");
  H5Dread(set_start, H5T_NATIVE_INT, H5S_ALL, H5S_ALL, H5P_DEFAULT, start);
  src->start[0] = start[0]; src->start[1] = start[1]; src->start[2] = start[2]; src->start[3] = start[3];

  //Determine size
  int size[4];
  hid_t set_size = H5Dopen1(file, "_size");
  H5Dread(set_size, H5T_NATIVE_INT, H5S_ALL, H5S_ALL, H5P_DEFAULT, size);
  src->size[0] = size[0]; src->size[1] = size[1]; src->size[2] = size[2]; src->size[3] = size[3];

  //Determine contents
  int contents;
  hid_t set_contents = H5Dopen1(file, "_contents");
  H5Dread(set_contents, H5T_NATIVE_INT, H5S_ALL, H5S_ALL, H5P_DEFAULT, &contents);

  src->contents[turb_velocity] = (contents & 0x01 ? 1 : 0);
  src->contents[turb_pressure] = (contents & 0x02 ? 1 : 0);
  src->contents[turb_magnetic] = (contents & 0x04 ? 1 : 0);
  src->contents[turb_potential] = (contents & 0x08 ? 1 : 0);
  src->contents[turb_density] = (contents & 0x16 ? 1 : 0);

  memset(src->data, 0, sizeof(float *) * 4096);

  /*
  int f;
  for (f = 0; f < 4; f++)
  {
    int t;
    for (t = src->start[0]; t < src->size[0]; t++)
    {
      if(src->contents[f] == 0) continue;
      src->data[f][t] = malloc(sizeof(float) * TurbFields[f].comps * src->size[1] * src->size[2] * src->size[3]);
      char setname[16];
      sprintf(setname, "%c%.5d", TurbFields[f].prefix, t);
      hid_t datachunk = H5Dopen1(file, setname);
      H5Dread(datachunk, H5T_NATIVE_FLOAT, H5S_ALL, H5S_ALL, H5P_DEFAULT, src->data[f][t]);
      H5Dclose(datachunk);
    }
  }
  */
  H5Dclose(set_name);
  H5Dclose(set_start);
  H5Dclose(set_size);
  H5Dclose(set_contents);

  if (__turblib_cutouts == NULL) {
      __turblib_cutouts = src;
    }
  else {
    cutoutFile* last = __turblib_cutouts;
    while(last->next != NULL)
      last = last->next;

    last->next = src;
  }

  return 0;
}

//Turn prefetching on or off
int turblibSetPrefetching(int prefetch)
{
  return (__turblib_prefetching = prefetch);
}

//Caches a portion of the specified file in memory
int loadDataToMemory(cutoutFile *src, TurbField function, int timestep, int xl, int yl, int zl, int xh, int yh, int zh)
{
  if(src->data[function][timestep] != NULL) return 0;

  hsize_t buff_size[] = { zh - zl + 1, yh - yl + 1, xh - xl + 1, TurbFields[function].comps };
  hid_t mspace = H5Screate_simple(4, buff_size, NULL);

  float* buff = malloc(sizeof(float) * TurbFields[function].comps * buff_size[0] * buff_size[1] * buff_size[2]);
  if(!buff) return -1;


  char setname[16];
  sprintf(setname, "%c%.5d", TurbFields[function].prefix, timestep*10);

  hsize_t start[4] = { zl - src->start[3], yl - src->start[2], xl - src->start[1], 0 },
          scount[4] = { zh - zl + 1, yh - yl + 1, xh - xl + 1, TurbFields[function].comps };

  hid_t dataset = H5Dopen1(src->file, setname);
  hid_t filespace = H5Dget_space(dataset);

  H5Sselect_hyperslab(filespace, H5S_SELECT_SET, start, NULL, scount, NULL);
  H5Dread(dataset, H5T_NATIVE_FLOAT, mspace, filespace, H5P_DEFAULT, buff);

  H5Dclose(dataset);
  H5Sclose(filespace);

  dataBlock *cache = malloc(sizeof(dataBlock));

  cache->data = buff;
  cache->xl = xl;
  cache->yl = yl;
  cache->zl = zl;
  cache->hx = scount[2];
  cache->hy = scount[1];
  cache->hz = scount[0];

  src->data[function][timestep] = cache;

  return 0;
}

int freeLoadedMemory(void)
{
  cutoutFile * file;
  if(!__turblib_prefetching) return 0;
  int j, k;
  for (file = __turblib_cutouts; file != NULL; file = file->next)
  {
    for(j = 0; j < 4; j++)
    {
      for(k = 0; k < 1024; k++)
      {
        if(file->data[j][k] != NULL)
        {
          free(file->data[j][k]->data);
          free(file->data[j][k]);
          file->data[j][k] = NULL;
        }
      }
    }
  }
  return 0;
}

int loadNeededData(TurbDataset set, TurbField function, int count, float position[][3], float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal)
{
  if(!__turblib_prefetching) return 0;

  float dx = DataSets[set].dx, dt = DataSets[set].dt;
  //Determine availability of data locally
  int nOrderLag = spatial % 10;
  int nOrderFD = (int) (spatial / 10);
  int size =  (spatial == NoSInt ? 1 : nOrderLag) + nOrderFD;
  int timestep = (int)ceil(time/dt - .5f);

  int timesteps = 1;
  if (temporal == PCHIPInt) { timestep -= 1; timesteps = 4; }

  int t;
  for (t = 0; t < timesteps; t++)
  {

    cutoutFile *file;
    for (file = __turblib_cutouts; file != NULL; file = file->next)
    {
      int fileUsed = 0;
      int lowestx = file->start[1] + file->size[1] - 1, lowesty = file->start[2] + file->size[2] - 1, lowestz = file->start[3] + file->size[3] - 1,
          highestx = file->start[1], highesty = file->start[2], highestz = file->start[3];
      int i;
      int x, y, z, endx, endy, endz;
      for (i = 0; i < count; i++)
      {
        if (spatial==NoSInt)
        {
	  x = (int) (round(position[i][0]/dx)) - nOrderFD/2;
	  y = (int) (round(position[i][1]/dx)) - nOrderFD/2;
	  z = (int) (round(position[i][2]/dx)) - nOrderFD/2;
        }
        else
        {
          x = (int) (floor(position[i][0]/dx)) - (nOrderLag + nOrderFD) / 2 + 1;
          y = (int) (floor(position[i][1]/dx)) - (nOrderLag + nOrderFD) / 2 + 1;
          z = (int) (floor(position[i][2]/dx)) - (nOrderLag + nOrderFD) / 2 + 1;
        }

	endx = x + size - 1;
	endy = y + size - 1;
	endz = z + size - 1;

	x = (x % 1024 + 1024) % 1024;
	y = (y % 1024 + 1024) % 1024;
	z = (z % 1024 + 1024) % 1024;

	endx = (endx % 1024 + 1024) % 1024;
	endy = (endy % 1024 + 1024) % 1024;
	endz = (endz % 1024 + 1024) % 1024;

        //if(!isWithinFile(set, function, x, y, z, size, size, size, timestep, file)) continue;
	//fileUsed = 1;
	//if(x < lowestx) lowestx = x;
	//if(y < lowesty) lowesty = y;
	//if(z < lowestz) lowestz = z;

	//if(x > highestx) highestx = x;
	//if(y > highesty) highesty = y;
	//if(z > highestz) highestz = z;

	if (file->dataset == set &&
	    (function == turb_vp ? file->contents[turb_pressure] && file->contents[turb_velocity] : file->contents[function]) &&
	    timestep >= file->start[0] && timestep           <= (file->start[0] + file->size[0]-1))
	  {
	    if ((file->start[1] <= x && x < (file->start[1] + file->size[1])) ||
		(file->start[1] <= endx && endx < (file->start[1] + file->size[1])) ||
		(x < file->start[1] && (file->start[1] + file->size[1]) <= endx) ||
		(x < file->start[1] && endx < x) ||
		((file->start[1] + file->size[1]) <= endx && endx < x))
	      if ((file->start[2] <= y && y < (file->start[2] + file->size[2])) ||
		  (file->start[2] <= endy && endy < (file->start[2] + file->size[2])) ||
		  (y < file->start[2] && (file->start[2] + file->size[2]) <= endy) ||
		  (y < file->start[2] && endy < y) ||
		  ((file->start[2] + file->size[2]) <= endy && endy < y))
		if ((file->start[3] <= z && z < (file->start[3] + file->size[3])) ||
		    (file->start[3] <= endz && endz < (file->start[3] + file->size[3])) ||
		    (z < file->start[3] && (file->start[3] + file->size[3]) <= endz) ||
		    (z < file->start[3] && endz < z) ||
		    ((file->start[3] + file->size[3]) <= endz && endz < z))
		  {
		    if (lowestx > x)
		      if (x >= file->start[1])
			lowestx = x;
		      else
			lowestx = file->start[1];
		    else if (x >= file->start[1] + file->size[1] && x > endx)
		      lowestx = file->start[1];

		    if (lowesty > y)
		      if (y >= file->start[2])
			lowesty = y;
		      else
			lowesty = file->start[2];
		    else if (y >= file->start[2] + file->size[2] && y > endy)
		      lowesty = file->start[2];

		    if (lowestz > z)
		      if (z >= file->start[3])
			lowestz = z;
		      else
			lowestz = file->start[3];
		    else if (z >= file->start[3] + file->size[3] && z > endz)
		      lowestz = file->start[3];

		    if (highestx < endx)
		      if (endx < file->start[1] + file->size[1])
			highestx = endx;
		      else
			highestx = file->start[1] + file->size[1] - 1;
		    else if (endx < file->start[1] && x > endx)
		      highestx = file->start[1] + file->size[1] - 1;

		    if (highesty < endy)
		      if (endy < file->start[2] + file->size[2])
			highesty = endy;
		      else
			highesty = file->start[2] + file->size[2] - 1;
		    else if (endy < file->start[2] && y > endy)
		      highesty = file->start[2] + file->size[2] - 1;

		    if (highestz < endz)
		      if (endz < file->start[3] + file->size[3])
			highestz = endz;
		      else
			highestz = file->start[3] + file->size[3] - 1;
		    else if (endz < file->start[3] && z > endz)
		      highestz = file->start[3] + file->size[3] - 1;
		  }
	  }
      }
      if(!fileUsed) continue;

      //highestx = highestx + size - 1;
      //highesty = highesty + size - 1;
      //highestz = highestz + size - 1;

      loadDataToMemory(file, function, timestep, lowestx, lowesty, lowestz, highestx, highesty, highestz);
    }

  }
  return 1;
}

TurbDataset getDataSet(char *name)
{
  if (strcmp("isotropic1024coarse", name) == 0) return isotropic1024coarse;
  if (strcmp("isotropic1024", name) == 0) return isotropic1024coarse;
  if (strcmp("isotropic1024fine", name) == 0) return isotropic1024fine;
  if (strcmp("mhd1024", name) == 0) return mhd1024;
  if (strcmp("channel", name) == 0) return channel;
  if (strcmp("mixing", name) == 0) return mixing;
  if (strcmp("custom", name) == 0) return custom_dataset;

  return -1;
}

int isDataAvailable(TurbDataset set, TurbField function, int count, float position[][3], float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal)
{
  if (set == channel)
    {
      printf("The client library does not currently provide local implementaions"
		 "of the server-side functions for the channel flow dataset."
	     " Redirecting to the server...\n");
      return 0;
    }

  float dx = DataSets[set].dx, dt = DataSets[set].dt;
  //Determine availability of data locally
  int nOrderLag = spatial % 10;
  int nOrderFD = (int) (spatial / 10);
  int size =  (spatial == NoSInt ? 1 : nOrderLag) + nOrderFD;
  int xc, yc, zc;

  int i;
  for (i = 0; i < count; i++)
  {
    int x = (int) (floor(position[i][0]/dx)),
      y = (int) (floor(position[i][1]/dx)),
      z = (int) (floor(position[i][2]/dx));

    if (spatial==NoSInt)
      {
        xc = (int) (round(position[i][0]/dx)) - nOrderFD/2;
        yc = (int) (round(position[i][1]/dx)) - nOrderFD/2;
        zc = (int) (round(position[i][2]/dx)) - nOrderFD/2;
      }
    else
      {
        xc = x - (nOrderLag + nOrderFD) / 2 + 1;
        yc = y - (nOrderLag + nOrderFD) / 2 + 1;
        zc = z - (nOrderLag + nOrderFD) / 2 + 1;
      }

    int timestep = (int)ceil(time/dt - .5f);

    if (temporal == PCHIPInt)
    {
      int t;
      for(t = 0; t < 4; t++)
      {
        if(!isDataComplete(set, function, xc, yc, zc, size, size, size, timestep-1+t)) {  return 0;
        }
      }
    }
    else
    {
      if(!isDataComplete(set, function,  xc, yc, zc, size, size, size, timestep)) {  return 0;
      }
    }
  }
  return 1;
}

//Determines if the given data cube can be completely assembled from all cutout files
//In the case where data may span more than 2 files we call on the function recursively
int isDataComplete(TurbDataset dataset, TurbField function, int x, int y, int z, int xw, int yw, int zw, int timestep)
{
  // Wrap the coordinates into the grid space
  x = (x % 1024 + 1024) % 1024;
  y = (y % 1024 + 1024) % 1024;
  z = (z % 1024 + 1024) % 1024;

  //Is the data available as a contiguous block?
  if(findDataBlock(dataset, function, x, y, z, xw, yw, zw, timestep) != NULL) return 1;

  //Is the data available in pieces across different files? (Individual HDF5 files are restricted to units of 16x16x16)
  cutoutFile* corner = findDataBlock(dataset, function, x, y, z, 1, 1, 1, timestep);
  if (corner == NULL) return 0;

  int sx = ((corner->start[1] + corner->size[1]) - x),
      sy = ((corner->start[2] + corner->size[2]) - y),
      sz = ((corner->start[3] + corner->size[3]) - z);

  int dx = xw - sx,
      dy = yw - sy,
      dz = zw - sz;

  if(findDataBlock(dataset, function, x, y, z, sx, sy, sz, timestep) == NULL) return 0;

  //NOTE: In the case of wrap-around corner->start + corner->size will be wrapped to 0 below
  //Ensure presence of other pieces
  if(dz > 0)
  {
    //    if(findDataBlock(dataset, function, x, y, corner->start[3] + corner->size[3],
    //      (dx > 0 ? size - dx : size), (dy > 0 ? size - dy : size), dz, timestep) == NULL) return 0;
    if(!isDataComplete(dataset, function, x, y, corner->start[3] + corner->size[3],
      (dx > 0 ? xw - dx : xw), (dy > 0 ? yw - dy : yw), dz, timestep)) return 0;

    if (dy > 0)
    {
      //      if(findDataBlock(dataset, function,
      //        x, corner->start[2] + corner->size[2], corner->start[3] + corner->size[3],
      //        (dx > 0 ? size - dx : size), dy, dz, timestep) == NULL) return 0;
      if(!isDataComplete(dataset, function,
        x, corner->start[2] + corner->size[2], corner->start[3] + corner->size[3],
        (dx > 0 ? xw - dx : xw), dy, dz, timestep)) return 0;

      if (dx > 0)
      {
	//        if(findDataBlock(dataset, function,
	//          corner->start[1] + corner->size[1], corner->start[2] + corner->size[2], corner->start[3] + corner->size[3],
	//          dx, dy, dz, timestep) == NULL) return 0;
        if(!isDataComplete(dataset, function,
          corner->start[1] + corner->size[1], corner->start[2] + corner->size[2], corner->start[3] + corner->size[3],
          dx, dy, dz, timestep)) return 0;
      }
    }
    if (dx > 0)
    {
      //      if(findDataBlock(dataset, function,
      //        corner->start[1] + corner->size[1], y, corner->start[3] + corner->size[3],
      //        dx, (dy > 0 ? size - dy : size), dz, timestep) == NULL) return 0;
      if(!isDataComplete(dataset, function,
        corner->start[1] + corner->size[1], y, corner->start[3] + corner->size[3],
        dx, (dy > 0 ? yw - dy : yw), dz, timestep)) return 0;
    }
  }
  if (dy > 0)
  {
    //    if(findDataBlock(dataset, function,
    //      x, corner->start[2] + corner->size[2], z,
    //      (dx > 0 ? size - dx : size), dy, (dz > 0 ? size - dz : size), timestep) == NULL) return 0;
    if(!isDataComplete(dataset, function,
      x, corner->start[2] + corner->size[2], z,
      (dx > 0 ? xw - dx : xw), dy, (dz > 0 ? zw - dz : zw), timestep)) return 0;

    if (dx > 0)
    {
      //      if(findDataBlock(dataset, function,
      //        corner->start[1] + corner->size[1], corner->start[2] + corner->size[2], z,
      //        dx, dy, (dz > 0 ? size - dz : size), timestep) == NULL) return 0;
    if(!isDataComplete(dataset, function,
      x, corner->start[2] + corner->size[2], z,
      (dx > 0 ? xw - dx : xw), dy, (dz > 0 ? zw - dz : zw), timestep)) return 0;
    }
  }
  if (dx > 0)
  {
    //    if(findDataBlock(dataset, function,
    //      corner->start[1] + corner->size[1], y, z,
    //      dx, (dy > 0 ? size - dy : size), (dz > 0 ? size - dz : size), timestep) == NULL) return 0;
    if(!isDataComplete(dataset, function,
      corner->start[1] + corner->size[1], y, z,
      dx, (dy > 0 ? yw - dy : yw), (dz > 0 ? zw - dz : zw), timestep)) return 0;
  }
  return 1;
}
//XYZ
cutoutFile* findDataBlock(TurbDataset dataset, TurbField function, int x, int y, int z, int xw, int yw, int zw, int timestep)
{
  x = (x % 1024 + 1024) % 1024;
  y = (y % 1024 + 1024) % 1024;
  z = (z % 1024 + 1024) % 1024;

  //xw = (xw % 1024 + 1024) % 1024;
  //yw = (yw % 1024 + 1024) % 1024;
  //zw = (zw % 1024 + 1024) % 1024;

  cutoutFile *file;
  for (file = __turblib_cutouts; file != NULL; file = file->next)
  {
    if (isWithinFile(dataset, function, x, y, z, xw, yw, zw, timestep, file))
          return file;
  }
  return NULL;
}

int isWithinFile(TurbDataset dataset, TurbField function, int x, int y, int z, int xw, int yw, int zw, int timestep, cutoutFile* file)
{
  //int result;
  //result = (file->dataset == dataset);
  ////fprintf(stderr,   "\nisWithinFile, %s\n", result ? "true" : "false");
  //result = result &&
  //     (function == turb_vp ? file->contents[turb_pressure] && file->contents[turb_velocity] : file->contents[function]);
  ////fprintf(stderr,   "isWithinFile, %s\n", result ? "true" : "false");
  //result = result &&
  //     timestep >= file->start[0] && timestep           <= (file->start[0] + file->size[0]-1);
  ////fprintf(stderr,   "isWithinFile, %s\n", result ? "true" : "false");
  //result = result &&
  //     x        >= file->start[1] && (x + xw) <= (file->start[1] + file->size[1]);
  ////fprintf(stderr,   "isWithinFile, %s\n", result ? "true" : "false");
  //result = result &&
  //     y        >= file->start[2] && (y + yw) <= (file->start[2] + file->size[2]);
  ////fprintf(stderr,   "isWithinFile, %s\n", result ? "true" : "false");
  //result = result &&
  //     z        >= file->start[3] && (z + zw) <= (file->start[3] + file->size[3]);
  ////fprintf(stderr,   "isWithinFile, %s\n", result ? "true" : "false");
  ////fprintf(stderr,   "isWithinFile, %d %d %d %d\n", z, zw, file->start[3], file->size[3]);
  //if (result)
  if ((file->dataset == dataset) &&
      (function == turb_vp ? file->contents[turb_pressure] && file->contents[turb_velocity] : file->contents[function]) &&
       timestep >= file->start[0] && timestep           <= (file->start[0] + file->size[0]-1) &&
       x        >= file->start[1] && (x + xw) <= (file->start[1] + file->size[1]) &&
       y        >= file->start[2] && (y + yw) <= (file->start[2] + file->size[2]) &&
       z        >= file->start[3] && (z + zw) <= (file->start[3] + file->size[3]))
    return 1;
  return 0;
}

/* zyx order */
dataKernel* getDataCube(TurbDataset dataset, TurbField function, int x, int y, int z, int timestep, int size)
{
  dataKernel* cube = malloc(sizeof(dataKernel));
  cutoutFile *loc = findDataBlock(dataset, function, x, y, z, size, size, size, timestep);
  if(loc != 0 && loc->data[function][timestep] != NULL)
  {
    dataBlock *cache = loc->data[function][timestep];
    cube->data = cache->data;
    cube->x = x - cache->xl;
    cube->y = y - cache->yl;
    cube->z = z - cache->zl;
    cube->hx = cache->hx;
    cube->hy = cache->hy;
    cube->hz = cache->hz;
    cube->comps = TurbFields[function].comps;
    cube->persist = 1;
  }
  else
  {
    //    printf("Cache miss!\n");
    float* buff = malloc(sizeof(float) * TurbFields[function].comps * size * size * size);
    loadDataCube(dataset, function, x, y, z, timestep, size, buff);
    cube->data = buff;
    cube->x = 0;
    cube->y = 0;
    cube->z = 0;
    cube->hx = size;
    cube->hy = size;
    cube->hz = size;
    cube->comps = TurbFields[function].comps;
    cube->persist = 0;
  }
  return cube;
}


int getSinglePoint(TurbDataset dataset, TurbField function, int x, int y, int z, int timestep, float *out)
{
  int comps = TurbFields[function].comps;

  cutoutFile *loc = findDataBlock(dataset, function, x, y, z, 1, 1, 1, timestep);
  if(loc != NULL && loc->data[function][timestep] != NULL)
  {
    dataBlock *cache = loc->data[function][timestep];
    int c = 0, index = x*comps + y*comps*cache->hx + z*comps*cache->hx*cache->hy;
    for (c = 0; c < comps; c++)
      out[c] = cache->data[c + index];
  }
  else
  {
    loadDataCube(dataset, function, x, y, z, timestep, 1, out);
  }
  return 0;
}

void freeDataCube(dataKernel* cube)
{
  if(cube->persist == 0) free(cube->data);
//  free cube;
}

/* zyx order */
//Loads a given block of data into memory, assembled possibly from multiple files
//TODO: The function doesn't handle the situation where the data cube requested
//      spans more than 2 files in each dimension
int loadDataCube(TurbDataset dataset, TurbField function, int x, int y, int z, int timestep, int size, float *buff)
{
  int comps = TurbFields[function].comps;

  hsize_t dim_mem[] = { size, size, size, comps };
  hid_t mspace = H5Screate_simple(4, dim_mem, NULL);

  //Wrap the coordinates into the grid space:
  x = (x % 1024 + 1024) % 1024;
  y = (y % 1024 + 1024) % 1024;
  z = (z % 1024 + 1024) % 1024;

  //Is the data available as a contiguous block?
  cutoutFile* src = findDataBlock(dataset, function, x, y, z, size, size, size, timestep);
  if(src != NULL)
  {
    char setname[16];
    sprintf(setname, "%c%.5d", TurbFields[function].prefix, timestep*10);
    hid_t dataset = H5Dopen1(src->file, setname);
    hid_t filespace = H5Dget_space(dataset);

    //Data selection of the file
    hsize_t start[4]  = { z - src->start[3], y - src->start[2], x - src->start[1], 0 },
            scount[4] = { size, size, size, comps };

    H5Sselect_hyperslab(filespace, H5S_SELECT_SET, start, NULL, scount, NULL);
    H5Dread(dataset, H5T_NATIVE_FLOAT, mspace, filespace, H5P_DEFAULT, (float*)buff);

    H5Dclose(dataset);
    H5Sclose(mspace);
    H5Sclose(filespace);

    return 0;
  }

  //Load the data piece by piece:
  cutoutFile *corner = findDataBlock(dataset, function, x, y, z, 1, 1, 1, timestep);

  int sx = ((corner->start[1] + corner->size[1]) - x),
      sy = ((corner->start[2] + corner->size[2]) - y),
      sz = ((corner->start[3] + corner->size[3]) - z);

  int dx = size - sx,
      dy = size - sy,
      dz = size - sz;

  sx = sx > size ? size : sx;
  sy = sy > size ? size : sy;
  sz = sz > size ? size : sz;

  //First load corner
  loadSubBlock(dataset, function, timestep, mspace, buff, x, y, z, sx, sy, sz, 0, 0, 0);

  //Then load other pieces
  if(dz > 0)
  {
    loadSubBlock(dataset, function, timestep, mspace, buff,
      x, y, corner->start[3] + corner->size[3],
      (dx > 0 ? size - dx : size), (dy > 0 ? size - dy : size), dz,
      0, 0, sz);

    if (dy > 0)
    {
      loadSubBlock(dataset, function, timestep, mspace, buff,
        x, corner->start[2] + corner->size[2], corner->start[3] + corner->size[3],
        (dx > 0 ? size - dx : size), dy, dz,
        0, sy, sz);

      if (dx > 0)
      {
        loadSubBlock(dataset, function, timestep, mspace, buff,
          corner->start[1] + corner->size[1], corner->start[2] + corner->size[2], corner->start[3] + corner->size[3],
          dx, dy, dz,
          sx, sy, sz);
      }
    }
    if (dx > 0)
    {
      loadSubBlock(dataset, function, timestep, mspace, buff,
        corner->start[1] + corner->size[1], y, corner->start[3] + corner->size[3],
        dx, (dy > 0 ? size - dy : size), dz,
        sx, 0, sz);
    }
  }
  if (dy > 0)
  {
    loadSubBlock(dataset, function, timestep, mspace, buff,
      x, corner->start[2] + corner->size[2], z,
      (dx > 0 ? size - dx : size), dy, (dz > 0 ? size - dz : size),
      0, sy, 0);

    if (dx > 0)
    {
      loadSubBlock(dataset, function, timestep, mspace, buff,
        corner->start[1] + corner->size[1], corner->start[2] + corner->size[2], z,
        dx, dy, (dz > 0 ? size - dz : size),
        sx, sy, 0);
    }
  }
  if (dx > 0)
  {
    loadSubBlock(dataset, function, timestep, mspace, buff,
    corner->start[1] + corner->size[1], y, z,
    dx, (dy > 0 ? size - dy : size), (dz > 0 ? size - dz : size),
    sx, 0, 0);
  }

  H5Sclose(mspace);
  return 1;
}

/* xyz order */
int loadSubBlock(TurbDataset dataset, TurbField function, int timestep, hid_t mspace, float *buff,
                 int x, int y, int z, int wx, int wy, int wz, int dest_x, int dest_y, int dest_z)
{
  x = (x % 1024 + 1024) % 1024;
  y = (y % 1024 + 1024) % 1024;
  z = (z % 1024 + 1024) % 1024;

  char setname[16];
  int comps = TurbFields[function].comps;
  sprintf(setname, "%c%.5d", TurbFields[function].prefix, timestep*10);
  cutoutFile *src = findDataBlock(dataset, function, x, y, z, wx, wy, wz, timestep);
  hid_t dataset_ = H5Dopen1(src->file, setname);

  hid_t filespace = H5Dget_space(dataset_);
  //Data selection of the file
  hsize_t start[4]  = { z - src->start[3], y - src->start[2], x - src->start[1], 0 },
          scount[4] = { wz, wy, wx, comps };

  //Data selection of memory
  hsize_t mstart[4] = { dest_z, dest_y, dest_x, 0 };

  H5Sselect_hyperslab(mspace, H5S_SELECT_SET, mstart, NULL, scount, NULL);
  H5Sselect_hyperslab(filespace, H5S_SELECT_SET, start, NULL, scount, NULL);

  H5Dread(dataset_, H5T_NATIVE_FLOAT, mspace, filespace, H5P_DEFAULT, buff);

  H5Sclose(filespace);
  H5Dclose(dataset_);
  return 0;
}

/* Local computation functions */

int validateParams(enum SpatialInterpolation spatial, TurbDataset set, int useFD)
{
  int nOrderFD = (int) spatial / 10;
  int nOrderLag = (int) spatial % 10;
  if( (useFD && nOrderFD != 4 && nOrderFD != 6 && nOrderFD != 8) ||
      (!useFD && nOrderFD != 0) ||
      (nOrderLag != 0 && nOrderLag != 4 && nOrderLag != 6 && nOrderLag != 8) ||
      (set < 0)
    )  { fprintf(stderr, "Error: Invalid interpolation parameter specified\n"); return 0; }

  return 1;
}

//Gets the value of a function at the point, with or without interpolation
int getSingleValue(TurbDataset dataset, TurbField func, float position[3], int timestep, enum SpatialInterpolation spatial, float *output)
{
  if (func == turb_vp)
  {
    getSingleValue(dataset, turb_velocity, position, timestep, spatial, output);
    getSingleValue(dataset, turb_pressure, position, timestep, spatial, &output[3]);
    return 0;
  }

  float dx = DataSets[dataset].dx;
  int nOrder = (int) spatial;
  int comps = TurbFields[func].comps;

  //If no spatial int, just return the closest points and finish
  if (nOrder == 0)
  {
    int x = (int) (round(position[0]/dx)), y = (int) (round(position[1]/dx)), z = (int) (round(position[2]/dx));
      getSinglePoint(dataset, func, x, y, z, timestep, output);
  }
  else
  {
    int x = (int) (floor(position[0]/dx)) - (nOrder/2) + 1,
      y = (int) (floor(position[1]/dx)) - (nOrder/2) + 1,
      z = (int) (floor(position[2]/dx)) - (nOrder/2) + 1;

    dataKernel* cube = getDataCube(dataset, func, x, y, z, timestep, nOrder);
    lagrangianInterp2(comps, cube, position, nOrder, dx, output);
    freeDataCube(cube);
  }
  return 0;
}

int getGradient (TurbDataset dataset, TurbField function, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float *output)
{
  loadNeededData(dataset, function, count, input, time, spatial, temporal);
  float dt = DataSets[dataset].dt;
  int timestep = (int)ceil(time/DataSets[dataset].dt - .5f);
  int comps = TurbFields[function].comps;
  int nOrderLag = spatial % 10, nOrderFD = spatial / 10;
  int gradientSize = (nOrderLag == 0? 1 : nOrderLag);
  int kernelsize = gradientSize + nOrderFD;
  int x, y, z;
  float dx = DataSets[dataset].dx;

  dataKernel* fdkernel;

  //Diff, Lagint, pchipInt
  if(temporal == PCHIPInt)
  {
    float temp[4][comps*3];
    int i, j;


    if (nOrderLag > 0)
    {
      float *lagkernel;
      lagkernel = malloc(sizeof(float) * comps * 3 * gradientSize * gradientSize * gradientSize);

      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx) - (kernelsize/2) + 1;
        y = (int) (input[i][1]/dx) - (kernelsize/2) + 1;
        z = (int) (input[i][2]/dx) - (kernelsize/2) + 1;

        for (j = 0; j < 4; j++)
        {
          fdkernel = getDataCube(dataset, function, x, y, z, timestep+j-1, kernelsize);
          computeGradient(fdkernel, comps, dx, gradientSize, nOrderFD, lagkernel);

          lagrangianInterp(comps * 3, lagkernel, input[i], nOrderLag, dx, temp[j]);
          freeDataCube(fdkernel);
        }
        pchipInterp(comps * 3, temp, time, timestep, dt, output+i*comps*3);
      }
      free(lagkernel);
    }
    else
    {
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx + 0.5f) - nOrderFD/2;
        y = (int) (input[i][1]/dx + 0.5f) - nOrderFD/2;
        z = (int) (input[i][2]/dx + 0.5f) - nOrderFD/2;

        for (j = 0; j < 4; j++)
        {
          fdkernel = getDataCube(dataset, function, x, y, z, timestep+j-1, kernelsize);
          computeGradient(fdkernel, comps, dx, gradientSize, nOrderFD, temp[j]);
          freeDataCube(fdkernel);
        }
        pchipInterp(comps * 3, temp, time, timestep, dt, output+i*comps*3);
      }
    }
  }
  //No temporal int
  else
  {
    int i;
    if (nOrderLag > 0)
    {
      float *lagkernel;
      lagkernel = malloc(sizeof(float) * comps * 3 * gradientSize * gradientSize * gradientSize);

      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx) - (kernelsize/2) + 1;
        y = (int) (input[i][1]/dx) - (kernelsize/2) + 1;
        z = (int) (input[i][2]/dx) - (kernelsize/2) + 1;

        fdkernel = getDataCube(dataset, function, x, y, z, timestep, kernelsize);
        computeGradient(fdkernel, comps, dx, gradientSize, nOrderFD, lagkernel);
        lagrangianInterp(comps * 3, lagkernel, input[i], nOrderLag, dx, output+i*comps*3);
        freeDataCube(fdkernel);
      }
      free(lagkernel);
    }
    else
    {
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx + 0.5f) - nOrderFD/2;
        y = (int) (input[i][1]/dx + 0.5f) - nOrderFD/2;
        z = (int) (input[i][2]/dx + 0.5f) - nOrderFD/2;
        fdkernel = getDataCube(dataset, function, x, y, z, timestep, kernelsize);
        computeGradient(fdkernel, comps, dx, gradientSize, nOrderFD, output+i*comps*3);
        freeDataCube(fdkernel);
      }
    }
  }
  freeLoadedMemory();
  return 0;
}

int getLaplacian (TurbDataset dataset, TurbField function, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[][3], float output[][3])
{
  loadNeededData(dataset, function, count, input, time, spatial, temporal);
  float dt = DataSets[dataset].dt;
  int timestep = (int)ceil(time/DataSets[dataset].dt - .5f);
  int comps = TurbFields[function].comps;
  int nOrderLag = spatial % 10, nOrderFD = spatial / 10;
  int gradientSize = (nOrderLag == 0? 1 : nOrderLag);
  int kernelsize = gradientSize + nOrderFD;
  int x, y, z;
  float dx = DataSets[dataset].dx;

  dataKernel* fdkernel;

  //Diff, Lagint, pchipInt
  if(temporal == PCHIPInt)
  {
    float temp[4][3];
    int i, j;

    if (nOrderLag > 0)
    {
      float *lagkernel;
      lagkernel = malloc(sizeof(float) * comps * gradientSize * gradientSize * gradientSize);

      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx) - (kernelsize/2) + 1;
        y = (int) (input[i][1]/dx) - (kernelsize/2) + 1;
        z = (int) (input[i][2]/dx) - (kernelsize/2) + 1;

        for (j = 0; j < 4; j++)
        {
          fdkernel = getDataCube(dataset, function, x, y, z, timestep+j-1, kernelsize);
          computeLaplacian(fdkernel, comps, dx, gradientSize, nOrderFD, lagkernel);
          lagrangianInterp(comps, lagkernel, input[i], nOrderLag, dx, temp[j]);
          freeDataCube(fdkernel);
        }
        pchipInterp(comps, temp, time, timestep, dt, output[i]);
      }
    }
    else
    {
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx + 0.5f) - nOrderFD/2;
        y = (int) (input[i][1]/dx + 0.5f) - nOrderFD/2;
        z = (int) (input[i][2]/dx + 0.5f) - nOrderFD/2;

        for (j = 0; j < 4; j++)
        {
          fdkernel = getDataCube(dataset, function, x, y, z, timestep+j-1, kernelsize);
          computeLaplacian(fdkernel, comps, dx, gradientSize, nOrderFD, temp[j]);
          freeDataCube(fdkernel);
        }
        pchipInterp(comps, temp, time, timestep, dt, output[i]);
      }
    }
  }
  //No temporal int
  else
  {
    int i;
    if (nOrderLag > 0)
    {
      float *lagkernel;
      lagkernel = malloc(sizeof(float) * comps * gradientSize * gradientSize * gradientSize);
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx) - (kernelsize / 2) + 1;
        y = (int) (input[i][1]/dx) - (kernelsize / 2) + 1;
        z = (int) (input[i][2]/dx) - (kernelsize / 2) + 1;

        fdkernel = getDataCube(dataset, function, x, y, z, timestep, kernelsize);
        computeLaplacian(fdkernel, comps, dx, gradientSize, nOrderFD, lagkernel);
        lagrangianInterp(comps, lagkernel, input[i], nOrderLag, dx, output[i]);
        freeDataCube(fdkernel);
      }
    }
    else
    {
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx + 0.5f) - nOrderFD/2;
        y = (int) (input[i][1]/dx + 0.5f) - nOrderFD/2;
        z = (int) (input[i][2]/dx + 0.5f) - nOrderFD/2;
        fdkernel = getDataCube(dataset, function, x, y, z, timestep, kernelsize);
        computeLaplacian(fdkernel, comps, dx, gradientSize, nOrderFD, output[i]);
        freeDataCube(fdkernel);
      }
    }
  }
  freeLoadedMemory();
  return 0;
}

int getHessian (TurbDataset dataset, TurbField function, float time, enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
  int count, float input[count][3], float* output)
{
  loadNeededData(dataset, function, count, input, time, spatial, temporal);
  float dt = DataSets[dataset].dt;
  int timestep = (int)ceil(time/DataSets[dataset].dt - .5f);
  int nOrderLag = spatial % 10, nOrderFD = spatial / 10;
  int gradientSize = (nOrderLag == 0? 1 : nOrderLag);
  int comps = TurbFields[function].comps;
  int kernelsize = gradientSize + nOrderFD;
  int x, y, z;
  float dx = DataSets[dataset].dx;

  dataKernel* fdkernel;

  //Diff, Lagint, pchipInt
  if(temporal == PCHIPInt)
  {
    float temp[4][18];
    int i, j;

    if (nOrderLag > 0)
    {
      float *lagkernel;
      lagkernel = malloc(sizeof(float) * comps * 6 * gradientSize * gradientSize * gradientSize);

      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx) - (kernelsize/2) + 1;
        y = (int) (input[i][1]/dx) - (kernelsize/2) + 1;
        z = (int) (input[i][2]/dx) - (kernelsize/2) + 1;

        for (j = 0; j < 4; j++)

        {
          fdkernel = getDataCube(dataset, function, x, y, z, timestep+j-1, kernelsize);
          computeHessian(fdkernel, comps, dx, gradientSize, nOrderFD, lagkernel);
          lagrangianInterp(comps * 6, lagkernel, input[i], nOrderLag, dx, temp[j]);
          freeDataCube(fdkernel);
        }
        pchipInterp(comps * 6, temp, time, timestep, dt, output+i*6*comps);
      }
    }
    else
    {
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx + 0.5f) - nOrderFD/2;
        y = (int) (input[i][1]/dx + 0.5f) - nOrderFD/2;
        z = (int) (input[i][2]/dx + 0.5f) - nOrderFD/2;

        for (j = 0; j < 4; j++)
        {
          fdkernel = getDataCube(dataset, function, x, y, z, timestep+j-1, kernelsize);
          computeHessian(fdkernel, comps, dx, gradientSize, nOrderFD, temp[j]);
          freeDataCube(fdkernel);
        }
        pchipInterp(comps * 6, temp, time, timestep, dt, output+i*6*comps);
      }
    }
  }
  //No temporal int
  else
  {
    int i;

    if (nOrderLag > 0)
    {
      float *lagkernel;
      lagkernel = malloc(sizeof(float) * comps * 6 * gradientSize * gradientSize * gradientSize);

      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx) - (kernelsize / 2) + 1;
        y = (int) (input[i][1]/dx) - (kernelsize / 2) + 1;
        z = (int) (input[i][2]/dx) - (kernelsize / 2) + 1;

        fdkernel = getDataCube(dataset, function, x, y, z, timestep, kernelsize);
        computeHessian(fdkernel, comps, dx, gradientSize, nOrderFD, lagkernel);
        lagrangianInterp(comps * 6, lagkernel, input[i], nOrderLag, dx, output+i*6*comps);
        freeDataCube(fdkernel);
      }
    }
    else
    {
      for (i = 0; i < count; i++)
      {
        x = (int) (input[i][0]/dx + 0.5f) - nOrderFD/2;
        y = (int) (input[i][1]/dx + 0.5f) - nOrderFD/2;
        z = (int) (input[i][2]/dx + 0.5f) - nOrderFD/2;
        fdkernel = getDataCube(dataset, function, x, y, z, timestep, kernelsize);
        computeHessian(fdkernel, comps, dx, gradientSize, nOrderFD, output+i*6*comps);
        freeDataCube(fdkernel);
      }
    }
  }
  freeLoadedMemory();
  return 0;
}

/* Interpolation Functions */

int lagrangianInterp(int comps, float *kernel, float position[3], int nOrder, float dx, float result[comps])
{
    int node[3];
    node[0] = (int) (floor(position[0]/dx));
    node[1] = (int) (floor(position[1]/dx));
    node[2] = (int) (floor(position[2]/dx));

      int x, y, z;
      float lagInt[3][8];

      for (x = 0; x < 3; x++)
      {
        float z1 = position[x] / dx - (float) node[x] ;
        float z2 = z1 * z1;
        float z3 = z2 * z1;
        switch(nOrder) {
          case 4:
          {
            lagInt[x][0] = (-2 * z1 + 3 * z2 - z3) / 6;
            lagInt[x][1] = (2 - z1 - 2 * z2 + z3) / 2;
            lagInt[x][2] = (2 * z1 + z2 - z3) / 2;
            lagInt[x][3] = (-z1 + z3) / 6;
            break;
          }
          case 6:
          {
            float z4 = z2 * z2;
            float z5 = z3 * z2;
            lagInt[x][0] = (6 * z1 - 5 * z2 - 5 * z3 + 5 * z4 - z5) / 120;
            lagInt[x][1] = (-12 * z1 + 16 * z2 - z3 - 4 * z4 + z5) / 24;
            lagInt[x][2] = (12 - 4 * z1 - 15 * z2 + 5 * z3 + 3 * z4 - z5) / 12;
            lagInt[x][3] = (12 * z1 + 8 * z2 - 7 * z3 - 2 * z4 + z5) / 12;
            lagInt[x][4] = (-6 * z1 - z2 + 7 * z3 + z4 - z5) / 24;
            lagInt[x][5] = (4 * z1 - 5 * z3 + z5) / 120;
            break;
          }
          case 8:
          {
            float z4 = z3 * z1;
            float z5 = z4 * z1;
            float z6 = z5 * z1;
            float z7 = z6 * z1;
            lagInt[x][0] = -z1 * (z6 - 7 * z5 + 7 * z4 + 35 * z3 - 56 * z2 - 28 * z1 + 48) / 5040;
            lagInt[x][1] = z1 * (z6 - 6 * z5 - 2 * z4 + 60 * z3 - 71 * z2 - 54 * z1 + 72) / 720;
            lagInt[x][2] = -z1 * (z6 - 5 * z5 - 9 * z4 + 65 * z3 - 16 * z2 - 180 * z1 + 144) / 240;
            lagInt[x][3] = (z7 - 4 * z6 - 14 * z5 + 56 * z4 + 49 * z3 - 196 * z2 - 36 * z1 + 144) / 144;
            lagInt[x][4] = -z1 * (z6 - 3 * z5 - 17 * z4 + 39 * z3 + 88 * z2 - 108 * z1 - 144) / 144;
            lagInt[x][5] = z1 * (z6 - 2 * z5 - 18 * z4 + 20 * z3 + 89 * z2 - 18 * z1 - 72) / 240;
            lagInt[x][6] = -z1 * (z6 - z5 - 17 * z4 + 5 * z3 + 64 * z2 - 4 * z1 - 48) / 720;
            lagInt[x][7] = z1 * (z6 - 14 * z4 + 49 * z2 - 36) / 5040;
            break;
          }
        }
      }

      int comp;
      for (comp = 0; comp < comps; comp++)
        result[comp] = 0;
      int index = 0;
      for (z = 0; z < nOrder; z++)
      {
        for (y = 0; y < nOrder; y++)
        {
          for (x = 0; x < nOrder; x++)
          {
            for (comp = 0; comp < comps; comp++)
            {
              result[comp] += kernel[index++] * lagInt[0][x] * lagInt[1][y] * lagInt[2][z];
            }
          }
        }
      }
  return 0;
}

int lagrangianInterp2(int comps, dataKernel* kernel, float position[3], int nOrder, float dx, float result[comps])
{
    int node[3];
    node[0] = (int) (floor(position[0]/dx));
    node[1] = (int) (floor(position[1]/dx));
    node[2] = (int) (floor(position[2]/dx));
      int x, y, z;
      float lagInt[3][8];
      float* data = kernel->data;
      for (x = 0; x < 3; x++)
      {
        float z1 = position[x] / dx - (float) node[x] ;
        float z2 = z1 * z1;
        float z3 = z2 * z1;
        switch(nOrder) {
          case 4:
          {
            lagInt[x][0] = (-2 * z1 + 3 * z2 - z3) / 6;
            lagInt[x][1] = (2 - z1 - 2 * z2 + z3) / 2;
            lagInt[x][2] = (2 * z1 + z2 - z3) / 2;
            lagInt[x][3] = (-z1 + z3) / 6;
            break;
          }
          case 6:
          {
            float z4 = z2 * z2;
            float z5 = z3 * z2;
            lagInt[x][0] = (6 * z1 - 5 * z2 - 5 * z3 + 5 * z4 - z5) / 120;
            lagInt[x][1] = (-12 * z1 + 16 * z2 - z3 - 4 * z4 + z5) / 24;
            lagInt[x][2] = (12 - 4 * z1 - 15 * z2 + 5 * z3 + 3 * z4 - z5) / 12;
            lagInt[x][3] = (12 * z1 + 8 * z2 - 7 * z3 - 2 * z4 + z5) / 12;
            lagInt[x][4] = (-6 * z1 - z2 + 7 * z3 + z4 - z5) / 24;
            lagInt[x][5] = (4 * z1 - 5 * z3 + z5) / 120;
            break;
          }
          case 8:
          {
            float z4 = z3 * z1;
            float z5 = z4 * z1;
            float z6 = z5 * z1;
            float z7 = z6 * z1;
            lagInt[x][0] = -z1 * (z6 - 7 * z5 + 7 * z4 + 35 * z3 - 56 * z2 - 28 * z1 + 48) / 5040;
            lagInt[x][1] = z1 * (z6 - 6 * z5 - 2 * z4 + 60 * z3 - 71 * z2 - 54 * z1 + 72) / 720;
            lagInt[x][2] = -z1 * (z6 - 5 * z5 - 9 * z4 + 65 * z3 - 16 * z2 - 180 * z1 + 144) / 240;
            lagInt[x][3] = (z7 - 4 * z6 - 14 * z5 + 56 * z4 + 49 * z3 - 196 * z2 - 36 * z1 + 144) / 144;
            lagInt[x][4] = -z1 * (z6 - 3 * z5 - 17 * z4 + 39 * z3 + 88 * z2 - 108 * z1 - 144) / 144;
            lagInt[x][5] = z1 * (z6 - 2 * z5 - 18 * z4 + 20 * z3 + 89 * z2 - 18 * z1 - 72) / 240;
            lagInt[x][6] = -z1 * (z6 - z5 - 17 * z4 + 5 * z3 + 64 * z2 - 4 * z1 - 48) / 720;
            lagInt[x][7] = z1 * (z6 - 14 * z4 + 49 * z2 - 36) / 5040;
            break;
          }
        }
      }

      int comp;
      for (comp = 0; comp < comps; comp++)
        result[comp] = 0;
      int index = 0;
      for (z = 0; z < nOrder; z++)
      {
        for (y = 0; y < nOrder; y++)
        {
	  index = (kernel->x)*comps + (y+kernel->y)*kernel->hx*comps +
          (z+kernel->z)*kernel->hx*kernel->hy*comps;
          for (x = 0; x < nOrder; x++)
          {
            for (comp = 0; comp < comps; comp++)
            {
              result[comp] += data[index++] * lagInt[0][x] * lagInt[1][y] * lagInt[2][z];
            }
          }
        }
      }
  return 0;
}

int pchipInterp(int comps, float data[4][comps], float time, int timestep, float dt, float result[comps])
{
      float times[4] = { (timestep - 1) * dt, (timestep) * dt, (timestep + 1) * dt, (timestep + 2) * dt };
      int j;
      for(j = 0; j < comps; j++)
      {
            float a, b, c, d;
            float delta = times[2] - times[1];
            float drv1 = ((data[2][j] - data[1][j]) / (times[2] - times[1]) + (data[1][j] - data[0][j]) / (times[1] - times[0]
            )) / 2;
            float drv2 = ((data[3][j] - data[2][j]) / (times[3] - times[2]) + (data[2][j] - data[1][j]) / (times[2] - times[1]
            )) / 2;

            a = data[1][j];
            b = drv1;
            c = ((data[2][j] - data[1][j]) / delta - drv1) / delta;
            d = 2 / delta / delta * ((drv1 + drv2) / 2 - (data[2][j] - data[1][j]) / (times[2] - times[1]));
            result[j] = a + b * (time - times[1]) + c * (time - times[1]) * (time - times[1]) +
                 d * (time - times[1]) * (time - times[1]) * (time - times[2]);
      }
  return 0;
}

/* Differentiation Functions */

int computeGradient(dataKernel* kernel, int comps, float dx, int size, int nOrder, float *output)
{
      int x, y, z, w;
      //Differentiate each point
      int   hx = comps,   hy = comps*kernel->hx,   hz=comps*kernel->hx*kernel->hy;
      float* data = kernel->data;

      for (z = 0; z < size; z++)
      {
        for (y = 0; y < size; y++)
        {
          for (x = 0; x < size; x++)
          {
            //Each component
            for (w = 0; w < comps; w++)
            {
            	int x_ = nOrder/2+x+kernel->x, y_ = nOrder/2+y+kernel->y, z_ = nOrder/2+z+kernel->z;
            	int index = z*size*size*comps*3 + y*size*comps*3 + x*comps*3 + w*3;
            	int center = z_*hz+ y_*hy + x_*hx + w;
            	switch(nOrder)
            	{
            	case 4: {
                  // dfw/dx
                  output[index] =
                    2.0f / 3.0f / dx *  (data[center +   hx] - data[center -   hx]) -
                    1.0f / 12.0f / dx * (data[center + 2*hx] - data[center - 2*hx]);

                  // dfw/dy
                  output[index+1] =
                    2.0f / 3.0f / dx *  (data[center +   hy] - data[center -   hy]) -
                    1.0f / 12.0f / dx * (data[center + 2*hy] - data[center - 2*hy]);

                  // dfw/dz
                  output[index+2] =
                    2.0f / 3.0f / dx *  (data[center +   hz] - data[center -   hz]) -
                    1.0f / 12.0f / dx * (data[center + 2*hz] - data[center - 2*hz]);
                  break;
                }
                case 6: {
                  // dfw/dx
                  output[index] =
                    3.0f / 4.0f / dx *  (data[center +   hx] - data[center -   hx]) -
                    3.0f / 20.0f / dx * (data[center + 2*hx] - data[center - 2*hx]) +
                    1.0f / 60.0f / dx * (data[center + 3*hx] - data[center - 3*hx]);

                  // dfw/dy
                  output[index+1] =
                    3.0f / 4.0f / dx *  (data[center +   hy] - data[center -   hy]) -
                    3.0f / 20.0f / dx * (data[center + 2*hy] - data[center - 2*hy]) +
                    1.0f / 60.0f / dx * (data[center + 3*hy] - data[center - 3*hy]);

                  // dfw/dz
                  output[index+2] =
                    3.0f / 4.0f / dx *  (data[center +   hz] - data[center -   hz]) -
                    3.0f / 20.0f / dx * (data[center + 2*hz] - data[center - 2*hz]) +
                    1.0f / 60.0f / dx * (data[center + 3*hz] - data[center - 3*hz]);
                  break;
                }
                case 8: {
                  // dfw/dx
                  output[index] =
                    4.0f / 5.0f / dx *  (data[center +   hx] - data[center -   hx]) -
                    1.0f / 5.0f / dx *  (data[center + 2*hx] - data[center - 2*hx]) +
                    4.0f / 105.0f/ dx * (data[center + 3*hx] - data[center - 3*hx]) -
                    1.0f / 280.0f/ dx * (data[center + 4*hx] - data[center - 4*hx]);

                  // dfw/dy
                  output[index+1] =
                    4.0f / 5.0f / dx *  (data[center +   hy] - data[center -   hy]) -
                    1.0f / 5.0f / dx *  (data[center + 2*hy] - data[center - 2*hy]) +
                    4.0f / 105.0f/ dx * (data[center + 3*hy] - data[center - 3*hy]) -
                    1.0f / 280.0f/ dx * (data[center + 4*hy] - data[center - 4*hy]);

                  // dfw/dz
                  output[index+2] =
                    4.0f / 5.0f / dx *  (data[center +   hz] - data[center -   hz]) -
                    1.0f / 5.0f / dx *  (data[center + 2*hz] - data[center - 2*hz]) +
                    4.0f / 105.0f/ dx * (data[center + 3*hz] - data[center - 3*hz]) -
                    1.0f / 280.0f/ dx * (data[center + 4*hz] - data[center - 4*hz]);
                  break;
                }
              }

            }
          }
        }
      }
  return 0;
}

int computeLaplacian(dataKernel* kernel, int comps, float dx, int size, int nOrder, float *output)
{
      int x, y, z, w;
      float* data = kernel->data;
      int   hz = kernel->hy*kernel->hx*comps,   hy = kernel->hx*comps,   hx = comps;
      for (x = 0; x < size; x++)
      {
        for (y = 0; y < size; y++)
        {
          for (z = 0; z < size; z++)
          {
            //Each component
            for (w = 0; w < 3; w++)
            {
                int z_ = nOrder/2+z + kernel->z,
                    y_ = nOrder/2+y + kernel->y,
                    x_ = nOrder/2+x + kernel->x;
            	int index = z*size*size*comps +  y*size*comps + x*comps + w;
            	int center = z_*hz + y_*hy + x_*hx + w;
                switch(nOrder)
                {
                case 4: {
                // du2w/dxdx
                output[index] =
                  SecFiniteDiff4(dx,
                    data[center - 2*hx],
                    data[center -   hx],
                    data[center],
                    data[center +   hx],
                    data[center + 2*hx]) +

                  SecFiniteDiff4(dx,
                    data[center - 2*hy],
                    data[center -   hy],
                    data[center],
                    data[center +   hy],
                    data[center + 2*hy]) +

                  SecFiniteDiff4(dx,
                    data[center - 2*hz],
                    data[center -   hz],
                    data[center],
                    data[center +   hz],
                    data[center + 2*hz]);


                break;
                } case 6: {

                // du2w/dxdx
                output[index + 0] =
                  SecFiniteDiff6(dx,
                    data[center - 3*hx],
                    data[center - 2*hx],
                    data[center -   hx],
                    data[center],
                    data[center +   hx],
                    data[center + 2*hx],
                    data[center + 3*hx])+

                  SecFiniteDiff6(dx,
                    data[center - 3*hy],
                    data[center - 2*hy],
                    data[center -   hy],
                    data[center],
                    data[center +   hy],
                    data[center + 2*hy],
                    data[center + 3*hy])+

                  SecFiniteDiff6(dx,
                    data[center - 3*hz],
                    data[center - 2*hz],
                    data[center -   hz],
                    data[center],
                    data[center +   hz],
                    data[center + 2*hz],
                    data[center + 3*hz]);
                break;
                } case 8: {

                // du2w/dxdx
                output[index + 0] =
                  SecFiniteDiff8(dx,
                    data[center - 4*hx],
                    data[center - 3*hx],
                    data[center - 2*hx],
                    data[center -   hx],
                    data[center],
                    data[center +   hx],
                    data[center + 2*hx],
                    data[center + 3*hx],
                    data[center + 4*hx]) +

                  SecFiniteDiff8(dx,
                    data[center - 4*hy],
                    data[center - 3*hy],
                    data[center - 2*hy],
                    data[center -   hy],
                    data[center],
                    data[center +   hy],
                    data[center + 2*hy],
                    data[center + 3*hy],
                    data[center + 4*hy]) +

                  SecFiniteDiff8(dx,
                    data[center - 4*hz],
                    data[center - 3*hz],
                    data[center - 2*hz],
                    data[center -   hz],
                    data[center],
                    data[center +   hz],
                    data[center + 2*hz],
                    data[center + 3*hz],
                    data[center + 4*hz]);
                break;
                }
              }
            }

          }
        }
      }
  return 0;
}

int computeHessian(dataKernel* kernel, int comps, float dx, int size, int nOrder, float *output)
{
      int x, y, z, w;
      float* data = kernel->data;
      int hz = kernel->hy*kernel->hx*comps, hy = kernel->hx*comps, hx = comps;
      for (x = 0; x < size; x++)
      {
        for (y = 0; y < size; y++)
        {
          for (z = 0; z < size; z++)
          {
            //Each component
            for (w = 0; w < comps; w++)
            {
                int z_ = nOrder/2+z+kernel->z,
                    y_ = nOrder/2+y+kernel->y,
                    x_ = nOrder/2+x+kernel->x;
                int index = z*size*size*comps*6 + y*size*comps*6 + x*comps*6 + w*6;
                int center = z_*hz + y_*hy + x_*hx + w;
                switch(nOrder)
                {
                case 4: {
                // du2w/dxdx
                output[index + 0] =
                  SecFiniteDiff4(dx,
                    data[center - 2*hx],
                    data[center -   hx],
                    data[center],
                    data[center +   hx],
                    data[center + 2*hx]);

                // du2w/dxdy
                output[index + 1] =
                  CrossFiniteDiff4(dx,
                    data[center + 2*hy + 2*hx],
                    data[center + 2*hy - 2*hx],
                    data[center - 2*hy - 2*hx],
                    data[center - 2*hy + 2*hx],
                    data[center +   hy +   hx],
                    data[center +   hy -   hx],
                    data[center -   hy -   hx],
                    data[center -   hy +   hx]);

                // du2w/dxdz
                output[index + 2] =
                  CrossFiniteDiff4(dx,
                    data[center + 2*hz + 2*hx],
                    data[center + 2*hz - 2*hx],
                    data[center - 2*hz - 2*hx],
                    data[center - 2*hz + 2*hx],
                    data[center +   hz +   hx],
                    data[center +   hz -   hx],
                    data[center -   hz -   hx],
                    data[center -   hz +   hx]);

                // du2w/dydy
                output[index +3] =
                  SecFiniteDiff4(dx,
                    data[center - 2*hy],
                    data[center -   hy],
                    data[center],
                    data[center +   hy],
                    data[center + 2*hy]);

                // du2w/dydz
                output[index +4] =
                  CrossFiniteDiff4(dx,
                    data[center + 2*hz + 2*hy],
                    data[center + 2*hz - 2*hy],
                    data[center - 2*hz - 2*hy],
                    data[center - 2*hz + 2*hy],
                    data[center +   hz +   hy],
                    data[center +   hz -   hy],
                    data[center -   hz -   hy],
                    data[center -   hz +   hy]);

                // du2w/dzdz
                output[index +5] =
                  SecFiniteDiff4(dx,
                    data[center - 2*hz],
                    data[center -   hz],
                    data[center],
                    data[center +   hz],
                    data[center + 2*hz]);

                break;
                } case 6: {

                // du2w/dxdx
                output[index + 0] =
                  SecFiniteDiff6(dx,
                    data[center - 3*hx],
                    data[center - 2*hx],
                    data[center -   hx],
                    data[center],
                    data[center +   hx],
                    data[center + 2*hx],
                    data[center + 3*hx]);

                // du2w/dxdy
                output[index + 1] =
                  CrossFiniteDiff6(dx,
                    data[center + 3*hy + 3*hx],
                    data[center + 3*hy - 3*hx],
                    data[center - 3*hy - 3*hx],
                    data[center - 3*hy + 3*hx],
                    data[center + 2*hy + 2*hx],
                    data[center + 2*hy - 2*hx],
                    data[center - 2*hy - 2*hx],
                    data[center - 2*hy + 2*hx],
                    data[center +   hy +   hx],
                    data[center +   hy -   hx],
                    data[center -   hy -   hx],
                    data[center -   hy +   hx]);

                // du2w/dxdz
                output[index + 2] =
                  CrossFiniteDiff6(dx,
                    data[center + 3*hz + 3*hx],
                    data[center + 3*hz - 3*hx],
                    data[center - 3*hz - 3*hx],
                    data[center - 3*hz + 3*hx],
                    data[center + 2*hz + 2*hx],
                    data[center + 2*hz - 2*hx],
                    data[center - 2*hz - 2*hx],
                    data[center - 2*hz + 2*hx],
                    data[center +   hz +   hx],
                    data[center +   hz -   hx],
                    data[center -   hz -   hx],
                    data[center -   hz +   hx]);

                // du2w/dydy
                output[index +3] =
                  SecFiniteDiff6(dx,
                    data[center - 3*hy],
                    data[center - 2*hy],
                    data[center -   hy],
                    data[center],
                    data[center +   hy],
                    data[center + 2*hy],
                    data[center + 3*hy]);

                // du2w/dydz
                output[index +4] =
                  CrossFiniteDiff6(dx,
                    data[center + 3*hz + 3*hy],
                    data[center + 3*hz - 3*hy],
                    data[center - 3*hz - 3*hy],
                    data[center - 3*hz + 3*hy],
                    data[center + 2*hz + 2*hy],
                    data[center + 2*hz - 2*hy],
                    data[center - 2*hz - 2*hy],
                    data[center - 2*hz + 2*hy],
                    data[center +   hz +   hy],
                    data[center +   hz -   hy],
                    data[center -   hz -   hy],
                    data[center -   hz +   hy]);

                // du2w/dzdz
                output[index +5] =
                  SecFiniteDiff6(dx,
                    data[center - 3*hz],
                    data[center - 2*hz],
                    data[center -   hz],
                    data[center],
                    data[center +   hz],
                    data[center + 2*hz],
                    data[center + 3*hz]);
                break;
                } case 8: {

                // du2w/dxdx
                output[index + 0] =
                  SecFiniteDiff8(dx,
                    data[center - 4*hx],
                    data[center - 3*hx],
                    data[center - 2*hx],
                    data[center -   hx],
                    data[center],
                    data[center +   hx],
                    data[center + 2*hx],
                    data[center + 3*hx],
                    data[center + 4*hx]);

                // du2w/dxdy
                output[index + 1] =
                  CrossFiniteDiff8(dx,
                    data[center + 4*hy + 4*hx],
                    data[center + 4*hy - 4*hx],
                    data[center - 4*hy - 4*hx],
                    data[center - 4*hy + 4*hx],
                    data[center + 3*hy + 3*hx],
                    data[center + 3*hy - 3*hx],
                    data[center - 3*hy - 3*hx],
                    data[center - 3*hy + 3*hx],
                    data[center + 2*hy + 2*hx],
                    data[center + 2*hy - 2*hx],
                    data[center - 2*hy - 2*hx],
                    data[center - 2*hy + 2*hx],
                    data[center +   hy +   hx],
                    data[center +   hy -   hx],
                    data[center -   hy -   hx],
                    data[center -   hy +   hx]);

                // du2w/dxdz
                output[index + 2] =
                  CrossFiniteDiff8(dx,
                    data[center + 4*hz + 4*hx],
                    data[center + 4*hz - 4*hx],
                    data[center - 4*hz - 4*hx],
                    data[center - 4*hz + 4*hx],
                    data[center + 3*hz + 3*hx],
                    data[center + 3*hz - 3*hx],
                    data[center - 3*hz - 3*hx],
                    data[center - 3*hz + 3*hx],
                    data[center + 2*hz + 2*hx],
                    data[center + 2*hz - 2*hx],
                    data[center - 2*hz - 2*hx],
                    data[center - 2*hz + 2*hx],
                    data[center +   hz +   hx],
                    data[center +   hz -   hx],
                    data[center -   hz -   hx],
                    data[center -   hz +   hx]);

                // du2w/dydy
                output[index +3] =
                  SecFiniteDiff8(dx,
                    data[center - 4*hy],
                    data[center - 3*hy],
                    data[center - 2*hy],
                    data[center -   hy],
                    data[center],
                    data[center +   hy],
                    data[center + 2*hy],
                    data[center + 3*hy],
                    data[center + 4*hy]);

                // du2w/dydz
                output[index +4] =
                  CrossFiniteDiff8(dx,
                    data[center + 4*hz + 4*hy],
                    data[center + 4*hz - 4*hy],
                    data[center - 4*hz - 4*hy],
                    data[center - 4*hz + 4*hy],
                    data[center + 3*hz + 3*hy],
                    data[center + 3*hz - 3*hy],
                    data[center - 3*hz - 3*hy],
                    data[center - 3*hz + 3*hy],
                    data[center + 2*hz + 2*hy],
                    data[center + 2*hz - 2*hy],
                    data[center - 2*hz - 2*hy],
                    data[center - 2*hz + 2*hy],
                    data[center +   hz +   hy],
                    data[center +   hz -   hy],
                    data[center -   hz -   hy],
                    data[center -   hz +   hy]);

                // du2w/dzdz
                output[index +5] =
                  SecFiniteDiff8(dx,
                    data[center - 4*hz],
                    data[center - 3*hz],
                    data[center - 2*hz],
                    data[center -   hz],
                    data[center],
                    data[center +   hz],
                    data[center + 2*hz],
                    data[center + 3*hz],
                    data[center + 4*hz]);
                break;
                }
              }
            }

          }
        }
      }
  return 0;
}
#endif//CUTOUT_SUPPORT

int getvelocity_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getVelocity (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvelocitygradient_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][9],
      int len_a, int len_d)
{
  return getVelocityGradient(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvelocityhessian_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][18],
      int len_a, int len_d)
{
  return getVelocityHessian(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvelocitylaplacian_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getVelocityLaplacian (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getmagneticfield_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getMagneticField (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvectorpotential_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getVectorPotential (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvelocityandpressure_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][4],
      int len_a, int len_d)
{
  return getVelocityAndPressure (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getpressurehessian_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][6],
      int len_a, int len_d)
{
  return getPressureHessian(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getmagneticfieldlaplacian_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getMagneticFieldLaplacian (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvectorpotentiallaplacian_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getVectorPotentialLaplacian (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getdensityhessian_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][6],
      int len_a, int len_d)
{
  return getDensityHessian(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getdensitygradient_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getDensityGradient(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getdensity_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[],
      int len_a, int len_d)
{
  return getDensity (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getpressure_ (char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[],
      int len_a, int len_d)
{
  return getPressure (authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvectorpotentialhessian_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][18],
      int len_a, int len_d)
{
  return getVectorPotentialHessian(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getpressuregradient_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][3],
      int len_a, int len_d)
{
  return getPressureGradient(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getmagneticfieldgradient_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][9],
      int len_a, int len_d)
{
  return getMagneticFieldGradient(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getvectorpotentialgradient_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][9],
      int len_a, int len_d)
{
  return getVectorPotentialGradient(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

int getmagneticfieldhessian_(char *authToken,
      char *dataset, float *time,
      int *spatial, int *temporal,
      int *count, float datain[][3], float dataout[][18],
      int len_a, int len_d)
{
  return getMagneticFieldHessian(authToken,
    dataset, *time,
    *spatial, *temporal,
    *count, datain, dataout);
}

