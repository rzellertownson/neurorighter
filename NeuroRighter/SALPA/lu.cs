// NeuroRighter
// Copyright (c) 2008 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>. 
//
// This code is a derivation of non-functioning code developed by Sergey Bochkanov (ALGLIB project),
// His code is in turn based on LAPACK: http://www.netlib.org/lapack/.

using System;

namespace NeuroRighter
{
    class lu
    {
        /*************************************************************************
        LU decomposition of a general matrix of size MxN

        The subroutine calculates the LU decomposition of a rectangular general
        matrix with partial pivoting (with row permutations).

        Input parameters:
            A   -   matrix A whose indexes range within [1..M, 1..N].
            M   -   number of rows in matrix A.
            N   -   number of columns in matrix A.

        Output parameters:
            A   -   matrices L and U in compact form (see below).
                    Array whose indexes range within [1..M, 1..N].
            Pivots - permutation matrix in compact form (see below).
                    Array whose index ranges within [1..Min(M,N)].

        Matrix A is represented as A = P * L * U, where P is a permutation matrix,
        matrix L - lower triangular (or lower trapezoid, if M>N) matrix,
        U - upper triangular (or upper trapezoid, if M<N) matrix.

        Let M be equal to 4 and N be equal to 3:

                           (  1          )    ( U11 U12 U13  )
        A = P1 * P2 * P3 * ( L21  1      )  * (     U22 U23  )
                           ( L31 L32  1  )    (         U33  )
                           ( L41 L42 L43 )

        Matrix L has size MxMin(M,N), matrix U has size Min(M,N)xN, matrix P(i) is
        a permutation of the identity matrix of size MxM with numbers I and Pivots[I].

        The algorithm returns array Pivots and the following matrix which replaces
        matrix A and contains matrices L and U in compact form (the example applies
        to M=4, N=3).

         ( U11 U12 U13 )
         ( L21 U22 U23 )
         ( L31 L32 U33 )
         ( L41 L42 L43 )

        As we can see, the unit diagonal isn't stored.

          -- LAPACK routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             June 30, 1992
        *************************************************************************/
        public static void ludecomposition(ref double[,] a,
            int m,
            int n,
            ref int[] pivots)
        {
            int i = 0;
            int j = 0;
            int jp = 0;
            double[] t1 = new double[0];
            double s = 0;
            int i_ = 0;

            pivots = new int[Math.Min(m, n) + 1];
            t1 = new double[Math.Max(m, n) + 1];
            System.Diagnostics.Debug.Assert(m >= 0 & n >= 0, "Error in LUDecomposition: incorrect function arguments");

            //
            // Quick return if possible
            //
            if (m == 0 | n == 0)
            {
                return;
            }
            for (j = 1; j <= Math.Min(m, n); j++)
            {

                //
                // Find pivot and test for singularity.
                //
                jp = j;
                for (i = j + 1; i <= m; i++)
                {
                    if (Math.Abs(a[i, j]) > Math.Abs(a[jp, j]))
                    {
                        jp = i;
                    }
                }
                pivots[j] = jp;
                if (a[jp, j] != 0)
                {

                    //
                    //Apply the interchange to rows
                    //
                    if (jp != j)
                    {
                        for (i_ = 1; i_ <= n; i_++)
                        {
                            t1[i_] = a[j, i_];
                        }
                        for (i_ = 1; i_ <= n; i_++)
                        {
                            a[j, i_] = a[jp, i_];
                        }
                        for (i_ = 1; i_ <= n; i_++)
                        {
                            a[jp, i_] = t1[i_];
                        }
                    }

                    //
                    //Compute elements J+1:M of J-th column.
                    //
                    if (j < m)
                    {

                        //
                        // CALL DSCAL( M-J, ONE / A( J, J ), A( J+1, J ), 1 )
                        //
                        jp = j + 1;
                        s = 1 / a[j, j];
                        for (i_ = jp; i_ <= m; i_++)
                        {
                            a[i_, j] = s * a[i_, j];
                        }
                    }
                }
                if (j < Math.Min(m, n))
                {

                    //
                    //Update trailing submatrix.
                    //CALL DGER( M-J, N-J, -ONE, A( J+1, J ), 1, A( J, J+1 ), LDA,A( J+1, J+1 ), LDA )
                    //
                    jp = j + 1;
                    for (i = j + 1; i <= m; i++)
                    {
                        s = a[i, j];
                        for (i_ = jp; i_ <= n; i_++)
                        {
                            a[i, i_] = a[i, i_] - s * a[j, i_];
                        }
                    }
                }
            }
        }


        /*************************************************************************
        LU decomposition of a general matrix of size MxN

        It uses LUDecomposition. L and U are not output in compact form, but as
        separate general matrices filled up by zero elements in their
        corresponding positions.

        This subroutine described here only serves the purpose to show
        how the result of ComplexLUDecomposition subroutine could be unpacked.

          -- ALGLIB --
             Copyright 2005 by Bochkanov Sergey
        *************************************************************************/
        public static void ludecompositionunpacked(double[,] a,
            int m,
            int n,
            ref double[,] l,
            ref double[,] u,
            ref int[] pivots)
        {
            int i = 0;
            int j = 0;
            int minmn = 0;

            a = (double[,])a.Clone();

            if (m == 0 | n == 0)
            {
                return;
            }
            minmn = Math.Min(m, n);
            l = new double[m + 1, minmn + 1];
            u = new double[minmn + 1, n + 1];
            ludecomposition(ref a, m, n, ref pivots);
            for (i = 1; i <= m; i++)
            {
                for (j = 1; j <= minmn; j++)
                {
                    if (j > i)
                    {
                        l[i, j] = 0;
                    }
                    if (j == i)
                    {
                        l[i, j] = 1;
                    }
                    if (j < i)
                    {
                        l[i, j] = a[i, j];
                    }
                }
            }
            for (i = 1; i <= minmn; i++)
            {
                for (j = 1; j <= n; j++)
                {
                    if (j < i)
                    {
                        u[i, j] = 0;
                    }
                    if (j >= i)
                    {
                        u[i, j] = a[i, j];
                    }
                }
            }
        }
    }
}