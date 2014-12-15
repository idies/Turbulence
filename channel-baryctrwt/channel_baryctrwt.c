
/* 
 * Program to compute the barycentric weights for channel flow DB interpolation
 * and differencing matrices. The weight calculation are based on Prof. Greg
 * Eyink's Matlab scripts.
 *
 * Date: 2013-11-04
 *
 * Written by:
 *   Jason Graham <jgraha8@gmail.com>
 */


#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "hdf5.h"

#define H5_GRID_FILE "grid.h5"

/*
 * min()/max() macros copied from linux/kernel.h header
 */
#define min(x, y) ({				\
	typeof(x) _min1 = (x);			\
	typeof(y) _min2 = (y);			\
	(void) (&_min1 == &_min2);		\
	_min1 < _min2 ? _min1 : _min2; })

#define max(x, y) ({				\
	typeof(x) _max1 = (x);			\
	typeof(y) _max2 = (y);			\
	(void) (&_max1 == &_max2);		\
	_max1 > _max2 ? _max1 : _max2; })

/* Local function prototypes */
int index_offset_y( const int N, const int q, const int i );
int index_start_y( const int N, const int q, const int i, const int i_o );
inline int index_end(const int q, const int i_s){ return (int)(i_s + q - 1);}
int cell_index_local_y(const int N, const int q, const int i, const int i_o );
int cell_index_local_uniform(const int q);

void baryctrwt_uniform( const int q, const double dx, double *w );
void baryctrwt( const int q, const double *x, double *w );

void baryctrwt_diffmat1_uniform( const int q, const int i, const double dx, double *D1 );
void baryctrwt_diffmat2_uniform( const int q, const int i, const double dx, double *D2 );
void baryctrwt_diffmat1( const int q, const int i, const double *x, double *D1 );
void baryctrwt_diffmat2( const int q, const int i, const double *x, double *D2 );

void baryctrwt_uniform_write( const char *fname, const int q, const double *w );
void baryctrwt_write_header( FILE *fid, const int q );
void baryctrwt_write_line( FILE *fid, const int i, const int i_o, const int i_s, const int i_e, const int q, const double *w, const unsigned char newline );

void baryctrwt_diffmat_uniform_write( const char *fname, const int q, const double *D );
void baryctrwt_diffmat_write_header( FILE *fid, const int q );

const hid_t h5_file_open( const char *fname );
void h5_file_close( const hid_t fid );
size_t h5_dataset_npoints( const hid_t fid, const char *dataset );
void h5_dataset_read( const hid_t fid, const char *dataset, double *field );

/*********************************************************************/
int main() 
/*********************************************************************/
{

	const hid_t grid_fid = h5_file_open( H5_GRID_FILE );

	/* Check the grid sizes */
	const size_t Nx = h5_dataset_npoints( grid_fid, "x" );
	const size_t Ny = h5_dataset_npoints( grid_fid, "y" );
	const size_t Nz = h5_dataset_npoints( grid_fid, "z" );

	printf("Nx, Ny, Nz = %zd, %zd, %zd\n", Nx, Ny, Nz);

	// Allocate the grid arrays
	double *x = (double *)malloc(sizeof(double)*Nx);
	double *y = (double *)malloc(sizeof(double)*Ny);
	double *z = (double *)malloc(sizeof(double)*Nz);

	// Read the grids from file
	h5_dataset_read( grid_fid, "x", x );
	h5_dataset_read( grid_fid, "y", y );
	h5_dataset_read( grid_fid, "z", z );

	h5_file_close( grid_fid );

	// Set the uniform grid spacing
	const double dx = x[1] - x[0];
	const double dz = z[1] - z[0];

	/*
	 * First compute the barycentric interpolation weights for the Lag4,
	 * Lag6, and Lag8 methods.
	 */
	int q=0;
	for (q=4; q<=8; q+=2 ) {

		char fname[80];

		// Stencil size equals q
		double *w = (double*)malloc(sizeof(double) * q );
		
		// X barycentric weights
		sprintf( fname, "baryctrwt-x-lag%d.dat", q);
		baryctrwt_uniform( q, dx, w );		
		baryctrwt_uniform_write( fname, q, w );
		
		// Z barycentric weights
		sprintf( fname, "baryctrwt-z-lag%d.dat", q);
		baryctrwt_uniform( q, dz, w );		
		baryctrwt_uniform_write( fname, q, w );

		// Y barycentric weights
		sprintf( fname, "baryctrwt-y-lag%d.dat", q);
		FILE *fid = fopen( fname, "w" );
		baryctrwt_write_header( fid, q );

		int i=0;
		for( i=0; i<Ny; i++ ) {

			const int i_o = index_offset_y(Ny,q,i);
			const int i_s = index_start_y(Ny,q,i,i_o);
			const int i_e = index_end(q, i_s);

			assert( i_s >= 0 && i_s <= Ny - q);
			
			// Write newline characters for all except last line
			const unsigned char newline = ( i < Ny-1 ? 1 : 0 );

			baryctrwt( q, y+i_s, w );
			baryctrwt_write_line( fid, i, i_o, i_s, i_e, q, w, newline );

		}
		fclose(fid);
		free(w);
	}

	/*
	 * Next compute the differencing matrices for the FD4, FD6, and FD8 methods.
	 */
	int q_prime=0; // Numerical order of the finite difference scheme
	for (q_prime=4; q_prime<=8; q_prime+=2 ) {

		char fname[80];
		int q = 0;

		// First derivatives along the uniform directions
		q=q_prime + 1;
		// cell index for the local stencil
		int i_local = cell_index_local_uniform(q); // Starting index is 0 for the local stencil

		double *D = (double*)malloc(sizeof(double) * q);
		
		// X barycentric weights
		// first derivative
		sprintf( fname, "baryctrwt-diffmat-x-r-1-fd%d.dat", q_prime);		
		baryctrwt_diffmat1_uniform( q, i_local, dx, D );		
		baryctrwt_diffmat_uniform_write( fname, q, D );
		// second derivative
		sprintf( fname, "baryctrwt-diffmat-x-r-2-fd%d.dat", q_prime);		
		baryctrwt_diffmat2_uniform( q, i_local, dx, D );		
		baryctrwt_diffmat_uniform_write( fname, q, D );


		// Z barycentric weights
		// first derivative
		sprintf( fname, "baryctrwt-diffmat-z-r-1-fd%d.dat", q_prime);		
		baryctrwt_diffmat1_uniform( q, i_local, dz, D );		
		baryctrwt_diffmat_uniform_write( fname, q, D );
		// second derivative
		sprintf( fname, "baryctrwt-diffmat-z-r-2-fd%d.dat", q_prime);		
		baryctrwt_diffmat2_uniform( q, i_local, dz, D );		
		baryctrwt_diffmat_uniform_write( fname, q, D );

		free(D);

		// Y barycentric weights
		sprintf( fname, "baryctrwt-diffmat-y-r-1-fd%d.dat", q_prime);		
		FILE *fid1 = fopen( fname, "w" );
		sprintf( fname, "baryctrwt-diffmat-y-r-2-fd%d.dat", q_prime);		
		FILE *fid2 = fopen( fname, "w" );

		// open the output files
		baryctrwt_diffmat_write_header( fid1, q_prime+1 );
		baryctrwt_diffmat_write_header( fid2, q_prime+2 );

		int i=0;
		for( i=0; i<Ny; i++ ) {

			// Compute the weights for the first and second
			// derivatives
			int r=0;
			for(r=1; r<=2; r++ ) {

				// Adjust the stencil size based on the order of
				// the differencing method and the derivative
				// order.
				q = q_prime + r;
				D = (double*)malloc(sizeof(double) * q);

				int i_o = index_offset_y(Ny,q,i);
				int i_s = index_start_y(Ny,q,i,i_o);
				int i_e = index_end(q, i_s);
				int i_local = cell_index_local_y(Ny,q, i, i_o ); // Local cell index

				assert( i_s >= 0 && i_s <= Ny - q);
			
				// Write newline characters for all except last line
				const unsigned char newline = ( i < Ny-1 ? 1 : 0 );

				if( r==1 ) {
					baryctrwt_diffmat1( q, i_local, y+i_s, D );
					baryctrwt_write_line( fid1, i, i_o, i_s, i_e, q, D, newline );
				} else if( r==2 ) {
					baryctrwt_diffmat2( q, i_local, y+i_s, D );
					baryctrwt_write_line( fid2, i, i_o, i_s, i_e, q, D, newline );
				} else {
					printf("bad r value for y derivatives\n");
					exit(EXIT_FAILURE);
				}
				free(D);
			}
			
		}
		fclose(fid1);
		fclose(fid2);
	}
	
	free(x);
	free(y);
	free(z);

	return 0;
	
}

/**********************************************************************/ 
int index_offset_y( const int N, const int q, const int i )
/**********************************************************************/ 
{
	const int q2  = (int)ceil((double)q/2);
	const int q2f = (int)floor((double)q/2);
	
	// Make sure the logic is correct
	assert( q == q2 + q2f );

	// Compute index offset
	int i_o=0;
	if( i <= N/2 - 1 ) {
		// Bottom channel half
		i_o = max((int)(q2-i-1),0);
	} else {
		// Top channel half
		i_o = min((int)(N-i-q2),0);
	}
	return i_o;
}

/**********************************************************************/ 
int index_start_y(const int N, const int q,const int i, const int i_o )
/**********************************************************************/ 
{
	const int q2  = (int)ceil((double)q/2);
	const int q2f = (int)floor((double)q/2);

	// Compute starting index
	if( i <= N/2 - 1 ) { 
		// Bottom channel half
		return (int)(i - q2 + 1 + i_o);
	} else { 
		// Top channel half
		return (int)(i - q2f + i_o);
	}
}

/*
 * Returns the local cell index for a given stencil size. 
 */
/**********************************************************************/ 
int cell_index_local_uniform(const int q)
/**********************************************************************/ 
{
	const int q2 = (int)ceil((double)q/2);
	return (int)(q2 - 1);
}

/*
 * Returns the local cell index for a given stencil size, global index, and
 * index offset.
 */
/**********************************************************************/ 
int cell_index_local_y(const int N, const int q, const int i, const int i_o )
/**********************************************************************/ 
{
	if( i <= N/2 - 1 ) { 
		// Bottom channel half
		const int q2 = (int)ceil((double)q/2);	
		return (int)(q2 - 1 - i_o);
	} else {
		// Top channel half
		const int q2f = (int)floor((double)q/2);
		return (int)(q2f - i_o);
	}
}

/*
 * Computes the barycenter interpolation weights for an arbitrarily spaced mesh.
 */
/********************************************************************/
void baryctrwt( const int q, const double *x, double *w ) 
/********************************************************************/
{
	int i,j;

	for( i=0; i<q; i++ ) w[i]=1.0L;

	for (i=1; i<q; i++ ) {
		for( j=0; j<i; j++ ) {
			w[j] = (x[j] - x[i])*w[j];
			w[i] = (x[i] - x[j])*w[i];
		}
	}
	for( i=0; i<q; i++ ) w[i]=1.0L / w[i];

	return;
}

/*
 * Computes the barycenter interpolation weights for a uniform mesh. Generates
 * an equispaced stencil and calls baryctrwt to compute the weights.
 */
/********************************************************************/
void baryctrwt_uniform( const int q, const double dx, double *w ) 
/********************************************************************/
{
	int i;

	double *x = (double *)malloc(sizeof(double) * q );
	for(i=0; i<q; i++) x[i] = i*dx;

	baryctrwt(q, x, w);

	free(x);
	/* for( i=0; i<q; i++ ) w[i]=1.0L; */

	/* for (i=1; i<q; i++ ) { */
	/* 	for( j=0; j<i; j++ ) { */
	/* 		w[j] = (j - i)*dx*w[j]; */
	/* 		w[i] = (i - j)*dx*w[i]; */
	/* 	} */
	/* } */
	/* for( i=0; i<q; i++ ) w[i]=1.0L / w[i]; */

	return;
}

/*
 * Computes the barycenter differencing matrix for the first derivative of a
 * function on an arbitrarily spaced mesh.
 */ 
/*********************************************************************/
void baryctrwt_diffmat1( const int q, const int i, const double *x, double *D )
/*********************************************************************/
{

	double *w = (double*)malloc(sizeof(double) * q );

	// First compute the barycentric weights for the stencil
	baryctrwt( q, x, w );

	const double wi = w[i]; // Save the weight at the cell index

	D[i] = 0.0L; // Will sum to get the weight for the cell index; initializing to 0

	int j=0;
	for(j=0; j<q; j++) {
		if( j != i ) {
			D[j] = (w[j] / wi) / (x[i] - x[j]);
			D[i] -= D[j];
		}
	}
}


/*
 * Computes the barycenter differencing matrix for the first derivative of a
 * function on a uniform mesh. Generates a equispaced stencil and calls
 * baryctrwt_diffmat1.
 */ 
/*********************************************************************/
void baryctrwt_diffmat1_uniform( const int q, const int i, const double dx, double *D )
/*********************************************************************/
{

	double *x = (double *)malloc(sizeof(double) * q );
	int j=0;
	for(j=0; j<q; j++) x[j] = j*dx;

	baryctrwt_diffmat1( q, i, x, D);
		
	free(x);

	/* double *w = (double*)malloc(sizeof(double) * q );	 */
	/* // First compute the barycentric weights for the stencil */
	/* baryctrwt_uniform( q, dx, w ); */

	/* const double wi = w[i]; // Save the weight at the cell index	 */

	/* D[i] = 0.0L; // Will sum to get the weight for the cell index; initializing to 0 */

	/* int j=0; */
	/* for(j=0; j<q; j++) { */
	/* 	if( j != i ) { */
	/* 		D[j] = (w[j] / wi) / (( i - j )*dx); */
	/* 		D[i] -= D[j]; */
	/* 	} */
	/* } */
}

/*
 * Computes the barycenter differencing matrix for the second derivative of a
 * function on an arbitrarily spaced mesh.
 */ 
/*********************************************************************/
void baryctrwt_diffmat2( const int q, const int i, const double *x, double *D2 )
/*********************************************************************/
{
	double *w = (double*)malloc(sizeof(double) * q );	
	// First compute the barycentric weights for the stencil
	baryctrwt( q, x, w );

	/*
	 * Get the differencing matrix for the first derivative
	 */ 
	double *D1 = (double*)malloc(sizeof(double) * q );
	baryctrwt_diffmat1( q, i, x, D1 );

	int j=0;
	double D1_sum = 0.0L;
	for(j=0; j<q; j++) {
		if( j != i ) 
			D1_sum += D1[j];		
	}

	D2[i] = 0.0L; // Will sum to get the weight for the cell index; initializing to 0
	for(j=0; j<q; j++) {
		if( j != i ) {
			const double ds = 1.0L / (x[i] - x[j]);
			D2[j] = -2 * D1[j] * ( D1_sum + ds );
			D2[i] -= D2[j];
		}
	}
	free(D1);
}

/*
 * Computes the barycenter differencing matrix for the second derivative of a
 * function on a uniform mesh. Generates a equispaced stencil and calls
 * baryctrwt_diffmat2.
 */ 
/*********************************************************************/
void baryctrwt_diffmat2_uniform( const int q, const int i, const double dx, double *D2 )
/*********************************************************************/
{

	double *x = (double *)malloc(sizeof(double) * q );
	int j=0;
	for(j=0; j<q; j++) x[j] = j*dx;

	baryctrwt_diffmat2(q, i, x, D2);
	free(x);

	/* double *w = (double*)malloc(sizeof(double) * q );	 */
	/* // First compute the barycentric weights for the stencil */
	/* baryctrwt_uniform( q, dx, w ); */

	/* /\* */
	/*  * Get the differencing matrix for the first derivative */
	/*  *\/  */
	/* double *D1 = (double*)malloc(sizeof(double) * q ); */
	/* baryctrwt_diffmat1_uniform( q, i, dx, D1 ); */

	/* int j=0; */
	/* double D1_sum = 0.0L; */
	/* for(j=0; j<q; j++) { */
	/* 	if( j != i )  */
	/* 		D1_sum += D1[j];		 */
	/* } */

	/* D2[i] = 0.0L; // Will sum to get the weight for the cell index; initializing to 0 */
	/* for(j=0; j<q; j++) { */
	/* 	if( j != i ) { */
	/* 		double ds = 1.0L / (( i - j )*dx); */
	/* 		D2[j] = -2 * D1[j] * ( D1_sum + ds ); */
	/* 		D2[i] -= D2[j]; */
	/* 	} */
	/* } */
	/* free(D1); */
}


/*********************************************************************/
void baryctrwt_uniform_write( const char *fname, const int q, const double *w )
/*********************************************************************/
{
	FILE *weight_file = fopen(fname,"w");

	int i=0;

	/* First line is a comment so starts with # */
	/* fprintf(weight_file,"# "); */
	/* for( i=0; i<q-1; i++ ) fprintf(weight_file, "w[%d], ", i ); */
	/* fprintf(weight_file, "w[%d]\n",q-1); */
	for( i=0; i<q-1; i++ ) fprintf(weight_file, "%.16e ", w[i]);
	fprintf(weight_file, "%.16e", w[q-1]);
	//fprintf(weight_file, "%.16e\r\n", w[q-1]);
	fclose(weight_file);
		
}

/*********************************************************************/
void baryctrwt_diffmat_uniform_write( const char *fname, const int q, const double *D )
/*********************************************************************/
{
	FILE *weight_file = fopen(fname,"w");

	int i=0;

	/* First line is a comment so starts with # */
	/* fprintf(weight_file,"# "); */
	/* for( i=0; i<q-1; i++ ) fprintf(weight_file, "D[%d], ", i ); */
	/* fprintf(weight_file, "D[%d]\n",q-1); */
	for( i=0; i<q-1; i++ ) fprintf(weight_file, "%.16e ", D[i]);
	fprintf(weight_file, "%.16e", D[q-1]);
	fclose(weight_file);
		
}

/*********************************************************************/
void baryctrwt_write_header( FILE *fid, const int q )
/*********************************************************************/
{
	/* First line is a comment so starts with # */
	/* fprintf(fid, "# j, j_s, j_o, "); */
	/* int i=0; */
	/* for( i=0; i<q-1; i++ ) fprintf(fid, "w[%d], ", i ); */
	/* fprintf(fid, "w[%d]\n",q-1); */

}

/*********************************************************************/
void baryctrwt_write_line( FILE *fid, const int i, const int i_o, 
			   const int i_s, const int i_e, const int q, 
			   const double *w, const unsigned char newline )
/*********************************************************************/
{
	fprintf(fid, "%d %d %d %d ", i, i_o, i_s, i_e);
	int j=0;
	for( j=0; j<q-1; j++ ) fprintf(fid, "%.16e ", w[j]);
	fprintf(fid, "%.16e", w[q-1]);
	if( newline ) fprintf(fid, "\r\n");
	
}

/*********************************************************************/
void baryctrwt_diffmat_write_header( FILE *fid, const int q )
/*********************************************************************/
{
	/* int i=0; */

	/* First line is a comment so starts with # */
	/* fprintf(fid, "# j, j_s, j_o, "); */
	/* for( i=0; i<q-1; i++ ) fprintf(fid, "D[%d], ", i ); */
	/* fprintf(fid, "D[%d]\n",q-1); */

}


/*********************************************************************/
const hid_t h5_file_open( const char *fname )
/*********************************************************************/
/*
  This function opens the specified file and creates a new h5file_t
  object
 */
{
	return H5Fopen (fname, H5F_ACC_RDONLY, H5P_DEFAULT);
}

/********************************************************************/
void h5_file_close( const hid_t fid )
/********************************************************************/
{
	H5Fclose( fid );
	return;
}


/*********************************************************************/
size_t h5_dataset_npoints( const hid_t fid, const char *dataset )
/*********************************************************************/
{

  hid_t dd; // Dataset handle
  hid_t ds; // Dataspace handle
  
  hssize_t npoints;

  // Dataset info
  dd = H5Dopen2(fid, dataset, H5P_DEFAULT);
  ds = H5Dget_space(dd);    /* dataspace handle */
  npoints = H5Sget_simple_extent_npoints( ds );

  H5Sclose(ds);
  H5Dclose(dd);

  return (size_t)npoints;

}


/*********************************************************************/
void h5_dataset_read( const hid_t fid, const char *dataset, double *field )
/*********************************************************************/
{

  herr_t status;
  hid_t dd; /* Dataset handle */
  hid_t ds; /* Dataspace handle */
  hid_t dt; /* Datatype handle */
  hid_t dt_native; /* Datatype native type */

    // Dataset info
  dd = H5Dopen2(fid, dataset, H5P_DEFAULT);
  ds = H5Dget_space(dd);    /* dataspace handle */
  dt = H5Dget_type(dd);     /* datatype handle */
  dt_native = H5Tget_native_type(dt, H5T_DIR_ASCEND);
  status = H5Dread(dd, dt_native, H5S_ALL, ds, H5P_DEFAULT, field);

  H5Tclose(dt_native);
  H5Tclose(dt);
  H5Sclose(ds);
  H5Dclose(dd);

  return;

}

