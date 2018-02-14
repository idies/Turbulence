USE [turblib_test]
GO

INSERT INTO turblib_test..barycentric_weights_x_4
SELECT * FROM turblib..barycentric_weights_x_4

INSERT INTO turblib_test..barycentric_weights_x_6
SELECT * FROM turblib..barycentric_weights_x_6

INSERT INTO turblib_test..barycentric_weights_x_8
SELECT * FROM turblib..barycentric_weights_x_8

---------------------------
INSERT INTO turblib_test..barycentric_weights_y_4
SELECT * FROM turblib..barycentric_weights_y_4

INSERT INTO turblib_test..barycentric_weights_y_6
SELECT * FROM turblib..barycentric_weights_y_6

INSERT INTO turblib_test..barycentric_weights_y_8
SELECT * FROM turblib..barycentric_weights_y_8

---------------------------
INSERT INTO turblib_test..barycentric_weights_z_4
SELECT * FROM turblib..barycentric_weights_z_4

INSERT INTO turblib_test..barycentric_weights_z_6
SELECT * FROM turblib..barycentric_weights_z_6

INSERT INTO turblib_test..barycentric_weights_z_8
SELECT * FROM turblib..barycentric_weights_z_8

---------------------------
INSERT INTO turblib_test..diff_matrix_x_r1_fd4
SELECT * FROM turblib..diff_matrix_x_r1_fd4

INSERT INTO turblib_test..diff_matrix_x_r1_fd6
SELECT * FROM turblib..diff_matrix_x_r1_fd6

INSERT INTO turblib_test..diff_matrix_x_r1_fd8
SELECT * FROM turblib..diff_matrix_x_r1_fd8

---------------------------
INSERT INTO turblib_test..diff_matrix_y_r1_fd4
SELECT * FROM turblib..diff_matrix_y_r1_fd4

INSERT INTO turblib_test..diff_matrix_y_r1_fd6
SELECT * FROM turblib..diff_matrix_y_r1_fd6

INSERT INTO turblib_test..diff_matrix_y_r1_fd8
SELECT * FROM turblib..diff_matrix_y_r1_fd8

---------------------------
INSERT INTO turblib_test..diff_matrix_z_r1_fd4
SELECT * FROM turblib..diff_matrix_z_r1_fd4

INSERT INTO turblib_test..diff_matrix_z_r1_fd6
SELECT * FROM turblib..diff_matrix_z_r1_fd6

INSERT INTO turblib_test..diff_matrix_z_r1_fd8
SELECT * FROM turblib..diff_matrix_z_r1_fd8

---------------------------
INSERT INTO turblib_test..grid_points_y
SELECT * FROM turblib..grid_points_y

INSERT INTO turblib_test..PartLimits08
SELECT * FROM turblib..PartLimits08

---------------------------
INSERT INTO turblib_test..spline_coeff_y_m1q4_d0
SELECT * FROM turblib..spline_coeff_y_m1q4_d0

INSERT INTO turblib_test..spline_coeff_y_m1q4_d1
SELECT * FROM turblib..spline_coeff_y_m1q4_d1

---------------------------
INSERT INTO turblib_test..spline_coeff_y_m2q14_d0
SELECT * FROM turblib..spline_coeff_y_m2q14_d0

INSERT INTO turblib_test..spline_coeff_y_m2q14_d1
SELECT * FROM turblib..spline_coeff_y_m2q14_d1

INSERT INTO turblib_test..spline_coeff_y_m2q14_d2
SELECT * FROM turblib..spline_coeff_y_m2q14_d2

---------------------------
INSERT INTO turblib_test..spline_coeff_y_m2q8_d0
SELECT * FROM turblib..spline_coeff_y_m2q8_d0

INSERT INTO turblib_test..spline_coeff_y_m2q8_d1
SELECT * FROM turblib..spline_coeff_y_m2q8_d1

INSERT INTO turblib_test..spline_coeff_y_m2q8_d2
SELECT * FROM turblib..spline_coeff_y_m2q8_d2

INSERT INTO turblib_test..diff_matrix_x_r2_fd4
SELECT * FROM turblib..diff_matrix_x_r2_fd4

INSERT INTO turblib_test..diff_matrix_y_r2_fd4
SELECT * FROM turblib..diff_matrix_y_r2_fd4

INSERT INTO turblib_test..diff_matrix_z_r2_fd4
SELECT * FROM turblib..diff_matrix_z_r2_fd4

INSERT INTO turblib_test..diff_matrix_x_r2_fd6
SELECT * FROM turblib..diff_matrix_x_r2_fd6

INSERT INTO turblib_test..diff_matrix_x_r2_fd8
SELECT * FROM turblib..diff_matrix_x_r2_fd8

INSERT INTO turblib_test..diff_matrix_y_r2_fd6
SELECT * FROM turblib..diff_matrix_y_r2_fd6

INSERT INTO turblib_test..diff_matrix_y_r2_fd8
SELECT * FROM turblib..diff_matrix_y_r2_fd8

INSERT INTO turblib_test..diff_matrix_z_r2_fd6
SELECT * FROM turblib..diff_matrix_z_r2_fd6

INSERT INTO turblib_test..diff_matrix_z_r2_fd8
SELECT * FROM turblib..diff_matrix_z_r2_fd8