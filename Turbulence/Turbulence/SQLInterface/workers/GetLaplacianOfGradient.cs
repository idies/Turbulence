using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;
namespace Turbulence.SQLInterface.workers
{
    class LaplacianOfGradient : Worker
    {
        private Computations compute;

        float[,] CenteredFiniteDiffCoeff = null;
        //float[] lagDenominator = null;

        public LaplacianOfGradient(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.compute = new Computations(setInfo);

            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                    kernelSize = 4; // kernelSize will be 4 in the case of None_Fd4 and 8 in the case of Fd4Lag4 (4+4)
                    CenteredFiniteDiffCoeff = new float[4, 5];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 4; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0f;

                    CenteredFiniteDiffCoeff[0, 2] = 1.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[1, 0] = 1.0f / 12.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 1] = -2.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 3] = 2.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 4] = -1.0f / 12.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[2, 0] = -1.0f / 12.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 1] = 4.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 2] = -5.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 3] = 4.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 4] = -1.0f / 12.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[3, 0] = -1.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 1] = 1.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 3] = -1.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 4] = 1.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                    kernelSize = 6;
                    CenteredFiniteDiffCoeff = new float[4, 7];
                    for (int j = 0; j < 7; j++)
                        for (int i = 0; i < 4; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0f;

                    CenteredFiniteDiffCoeff[0, 3] = -49.0f / 18.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[1, 0] = -1.0f / 60.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 1] = 3.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 2] = -3.0f / 4.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 4] = 3.0f / 4.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 5] = -3.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 6] = 1.0f / 60.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[2, 0] = 1.0f / 90.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 1] = -3.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 2] = 3.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 3] = -49.0f / 18.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 4] = 3.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 5] = -3.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 6] = 1.0f / 90.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[3, 0] = 1.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 1] = -1.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 2] = 13.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 4] = -13.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 5] = 1.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 6] = -1.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    kernelSize = 8;
                    CenteredFiniteDiffCoeff = new float[4, 9];
                    for (int j = 0; j < 9; j++)
                        for (int i = 0; i < 4; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0f;

                    CenteredFiniteDiffCoeff[0, 4] = 1.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[1, 0] = 1.0f / 280.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 1] = -4.0f / 105.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 2] = 1.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 3] = -4.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 5] = 4.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 6] = -1.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 7] = 4.0f / 105.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 8] = -1.0f / 280.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[2, 0] = -1.0f / 560.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 1] = 8.0f / 315.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 2] = -1.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 3] = 8.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 4] = -205.0f / 72.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 5] = 8.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 6] = -1.0f / 5.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 7] = 8.0f / 315.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 8] = -1.0f / 560.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;

                    CenteredFiniteDiffCoeff[3, 0] = -7.0f / 240.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 1] = 3.0f / 10.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 2] = -169.0f / 120.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 3] = 61.0f / 30.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 5] = -61.0f / 30.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 6] = 169.0f / 120.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 7] = -3.0f / 10.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 8] = 7.0f / 240.0f / setInfo.DxFloat / setInfo.DxFloat / setInfo.DxFloat;
                    break;
                default:
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
            new SqlMetaData("Req", SqlDbType.Int),
            new SqlMetaData("grad2duxdx", SqlDbType.Real),
            new SqlMetaData("grad2duydx", SqlDbType.Real),
            new SqlMetaData("grad2duzdx", SqlDbType.Real),
            new SqlMetaData("grad2duxdy", SqlDbType.Real),
            new SqlMetaData("grad2duydy", SqlDbType.Real),
            new SqlMetaData("grad2duzdy", SqlDbType.Real),
            new SqlMetaData("grad2duxdz", SqlDbType.Real),
            new SqlMetaData("grad2duydz", SqlDbType.Real),
            new SqlMetaData("grad2duzdz", SqlDbType.Real),
            new SqlMetaData("Cubes Read", SqlDbType.Int)};
        }

        /// <summary>
        /// Determines the database atoms that overlap the kernel of computation for the given point
        /// </summary>
        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int X, Y, Z;
            int startz, starty, startx, endz, endy, endx;
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5

            X = LagInterpolation.CalcNodeWithRound(request.x, setInfo.Dx);
            Y = LagInterpolation.CalcNodeWithRound(request.y, setInfo.Dx);
            Z = LagInterpolation.CalcNodeWithRound(request.z, setInfo.Dx);

            // In this case we are not performing Lagrange Polynomial interpolation 
            // and we need data from planer regions centered on the target location
            startz = Z - kernelSize / 2;
            endz = Z + kernelSize / 2;
            starty = Y - kernelSize / 2;
            endy = Y + kernelSize / 2;
            startx = X;
            endx = X;
            GetAtomsForRegion(startz, starty, startx, endz, endy, endx, atoms, mask);

            //x-z plane
            starty = Y;
            endy = Y;
            startx = X - kernelSize / 2;
            endx = X + kernelSize / 2;
            GetAtomsForRegion(startz, starty, startx, endz, endy, endx, atoms, mask);

            //x-y plane
            startz = Z;
            endz = Z;
            starty = Y - kernelSize / 2;
            endy = Y + kernelSize / 2;
            GetAtomsForRegion(startz, starty, startx, endz, endy, endx, atoms, mask);

            foreach (long atom in atoms)
            {
                if (!map.ContainsKey(atom))
                {
                    //map[atom] = new List<int>(pointsPerCubeEstimate);
                    map[atom] = new List<int>();
                }
                //Debug.Assert(!map[zindex].Contains(request.request));
                map[atom].Add(request.request);
                request.numberOfCubes++;
                total_points++;
            }
        }

        private void GetAtomsForRegion(int startz, int starty, int startx, int endz, int endy, int endx, HashSet<long> atoms, long mask)
        {
            long zindex;
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            for (int z = startz; z <= endz; z += setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int yi = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int zi = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            if (!atoms.Contains(zindex))
                                atoms.Add(zindex);
                        }
                    }
                }
            }
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            return (float[])compute.CalcLaplacianOfVelocityGradient(blob,
                new float[] { (float)input.x, (float)input.y, (float)input.z }, spatialInterp).Clone();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float xp = input.x;
            float yp = input.y;
            float zp = input.z;
            return CalcLaplacianOfGradient(blob, xp, yp, zp, input);
        }

        /// <summary>
        /// Function to calculate the Laplacian of the gradient of a field using I/O streaming.
        /// </summary>
        unsafe public float[] CalcLaplacianOfGradient(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            float[] result = new float[GetResultSize()]; // Result value for the user
            for (int i = 0; i < GetResultSize(); i++)
                result[i] = 0.0f;

            float[] data = blob.data;

            int x, y, z;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            int KernelStartX = 0, KernelStartY = 0, KernelStartZ = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    int length = kernelSize / 2;
                    x = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
                    y = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
                    z = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);
                    // Wrap the coordinates into the grid space
                    x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    z = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - length, y - length, x - length, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + length, y + length, x + length, ref endz, ref endy, ref endx);

                    // We also need to determine where we are starting, e.g. f(x_(n-2)), f(x_(n-1)), etc.
                    KernelStartX = blob.GetRealX - x + startx + length;
                    if (KernelStartX >= blob.GetGridResolution)
                        KernelStartX -= blob.GetGridResolution;
                    else if (KernelStartX < 0)
                        KernelStartX += blob.GetGridResolution;

                    KernelStartY = blob.GetRealY - y + starty + length;
                    if (KernelStartY >= blob.GetGridResolution)
                        KernelStartY -= blob.GetGridResolution;
                    else if (KernelStartY < 0)
                        KernelStartY += blob.GetGridResolution;

                    KernelStartZ = blob.GetRealZ - z + startz + length;
                    if (KernelStartZ >= blob.GetGridResolution)
                        KernelStartZ -= blob.GetGridResolution;
                    else if (KernelStartZ < 0)
                        KernelStartZ += blob.GetGridResolution;

                    fixed (float* fdata = data)
                    {
                        int off = 0;

                        // grad2dujdx and grad2dujdy contribution from x-y planer segment (j is any of x,y,z)
                        if (z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                        {
                            for (int iy = starty; iy <= endy; iy++)
                            {
                                int KernelIndexY = KernelStartY + iy - starty;
                                off = startx * blob.GetComponents + iy * blob.GetSide * blob.GetComponents + (z - blob.GetRealZ) * blob.GetSide * blob.GetSide * blob.GetComponents;

                                for (int ix = startx; ix <= endx; ix++)
                                {
                                    // components of the velocity
                                    float ux = fdata[off];
                                    float uy = fdata[off + 1];
                                    float uz = fdata[off + 2];

                                    int KernelIndexX = KernelStartX + ix - startx;
                                    // these are contributtions from the dxd2y to grad2dujdx, 
                                    // hence the coefficients come from the second row for y and first row for x
                                    float coeff_k2y = CenteredFiniteDiffCoeff[2, KernelIndexY];
                                    float coeff_k1x = CenteredFiniteDiffCoeff[1, KernelIndexX];
                                    result[0] += coeff_k1x * coeff_k2y * ux;
                                    result[1] += coeff_k1x * coeff_k2y * uy;
                                    result[2] += coeff_k1x * coeff_k2y * uz;
                                    // these are contributtions from the d2xdy to grad2dujdy, 
                                    // hence the coefficients come from the first row for y and second row for x
                                    float coeff_k1y = CenteredFiniteDiffCoeff[1, KernelIndexY];
                                    float coeff_k2x = CenteredFiniteDiffCoeff[2, KernelIndexX];
                                    result[3] += coeff_k2x * coeff_k1y * ux;
                                    result[4] += coeff_k2x * coeff_k1y * uy;
                                    result[5] += coeff_k2x * coeff_k1y * uz;

                                    // if we are centered on the target location we can add the contribution from the line segment along x
                                    // otherwise we only compute the contributions from the planer segments
                                    // KernelIndexY == length is equivalent to iy == y - blob.GetRealY
                                    if (KernelIndexY == length)
                                    {
                                        // these are contributtions from the d3x to grad2dujdx, 
                                        // hence the coefficients come from the third row for x
                                        float coeff_k3x = CenteredFiniteDiffCoeff[3, KernelIndexX];
                                        result[0] += coeff_k3x * ux;
                                        result[1] += coeff_k3x * uy;
                                        result[2] += coeff_k3x * uz;
                                    }

                                    off += blob.GetComponents;
                                }
                            }
                        }

                        // grad2dujdx and grad2dujdz contribution from x-z planer segment (j is any of x,y,z)
                        if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide)
                        {
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                int KernelIndexZ = KernelStartZ + iz - startz;
                                off = startx * blob.GetComponents + (y - blob.GetRealY) * blob.GetSide * blob.GetComponents + iz * blob.GetSide * blob.GetSide * blob.GetComponents;

                                for (int ix = startx; ix <= endx; ix++)
                                {
                                    // components of the velocity
                                    float ux = fdata[off];
                                    float uy = fdata[off + 1];
                                    float uz = fdata[off + 2];

                                    int KernelIndexX = KernelStartX + ix - startx;
                                    // these are contributtions from the dxd2z to grad2dujdx, 
                                    // hence the coefficients come from the second row for z and first row for x
                                    float coeff_k2z = CenteredFiniteDiffCoeff[2, KernelIndexZ];
                                    float coeff_k1x = CenteredFiniteDiffCoeff[1, KernelIndexX];
                                    result[0] += coeff_k1x * coeff_k2z * ux;
                                    result[1] += coeff_k1x * coeff_k2z * uy;
                                    result[2] += coeff_k1x * coeff_k2z * uz;
                                    // these are contributtions from the d2xdz to grad2dujdz, 
                                    // hence the coefficients come from the first row for z and second row for x
                                    float coeff_k1z = CenteredFiniteDiffCoeff[1, KernelIndexZ];
                                    float coeff_k2x = CenteredFiniteDiffCoeff[2, KernelIndexX];
                                    result[6] += coeff_k2x * coeff_k1z * ux;
                                    result[7] += coeff_k2x * coeff_k1z * uy;
                                    result[8] += coeff_k2x * coeff_k1z * uz;

                                    // if we are centered on the target location we can add the contribution from the line segment along z
                                    // otherwise we only compute the contributions from the planer segments
                                    // KernelIndexX == length is equivalent to ix == x - blob.GetRealX
                                    if (KernelIndexX == length)
                                    {
                                        // these are contributtions from the d3z to grad2dujdz, 
                                        // hence the coefficients come from the third row for z
                                        float coeff_k3z = CenteredFiniteDiffCoeff[3, KernelIndexZ];
                                        result[6] += coeff_k3z * ux;
                                        result[7] += coeff_k3z * uy;
                                        result[8] += coeff_k3z * uz;
                                    }

                                    off += blob.GetComponents;
                                }
                            }
                        }

                        // grad2dujdy and grad2dujdz contribution from y-z planer segment (j is any of x,y,z)
                        if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide)
                        {
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                int KernelIndexZ = KernelStartZ + iz - startz;
                                off = (x - blob.GetRealX) * blob.GetComponents + starty * blob.GetSide * blob.GetComponents + iz * blob.GetSide * blob.GetSide * blob.GetComponents;

                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    // components of the velocity
                                    float ux = fdata[off];
                                    float uy = fdata[off + 1];
                                    float uz = fdata[off + 2];

                                    int KernelIndexY = KernelStartY + iy - starty;
                                    // these are contributtions from the dyd2z to grad2dujdy, 
                                    // hence the coefficients come from the second row for z and first row for y
                                    float coeff_k2z = CenteredFiniteDiffCoeff[2, KernelIndexZ];
                                    float coeff_k1y = CenteredFiniteDiffCoeff[1, KernelIndexY];
                                    result[3] += coeff_k1y * coeff_k2z * ux;
                                    result[4] += coeff_k1y * coeff_k2z * uy;
                                    result[5] += coeff_k1y * coeff_k2z * uz;
                                    // these are contributtions from the d2ydz to grad2dujdz, 
                                    // hence the coefficients come from the first row for z and second row for y
                                    float coeff_k1z = CenteredFiniteDiffCoeff[1, KernelIndexZ];
                                    float coeff_k2y = CenteredFiniteDiffCoeff[2, KernelIndexY];
                                    result[6] += coeff_k2y * coeff_k1z * ux;
                                    result[7] += coeff_k2y * coeff_k1z * uy;
                                    result[8] += coeff_k2y * coeff_k1z * uz;

                                    // if we are centered on the target location we can add the contribution from the line segment along y
                                    // otherwise we only compute the contributions from the planer segments
                                    // KernelIndexZ == length is equivalent to iz == z - blob.GetRealZ
                                    if (KernelIndexZ == length)
                                    {
                                        // these are contributtions from the d3y to grad2dujdy, 
                                        // hence the coefficients come from the third row for z
                                        float coeff_k3y = CenteredFiniteDiffCoeff[3, KernelIndexY];
                                        result[3] += coeff_k3y * ux;
                                        result[4] += coeff_k3y * uy;
                                        result[5] += coeff_k3y * uz;
                                    }

                                    off += blob.GetComponents;
                                }
                            }
                        }
                    }
                    break;
                #endregion
                default:
                    throw new Exception("Invalid Spatial Interpolation Option");
            }

            return result;
        }


        public override int GetResultSize()
        {
            return 9;
        }

    }
}
