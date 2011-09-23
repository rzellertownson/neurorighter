// Accord Math Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Math.Differentiation
{
    using System;
    using Accord.Math;

    /// <summary>
    ///   Derivative approximation by finite differences.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   Numerical differentiation is a technique of numerical analysis to
    ///   produce an estimate of the derivative of a mathematical function or
    ///   function subroutine using values from the function and perhaps other
    ///   knowledge about the function.</para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///     Trent F. Guidry, Calculating derivatives of a function numerically. Available on:
    ///     http://www.trentfguidry.net/post/2009/07/12/Calculate-derivatives-function-numerically.aspx
    ///     </description></item>
    ///     </list>
    ///  </para>
    /// </remarks>
    /// 
    public class FiniteDifferences
    {
        private const double step = 1e-2;

        private int n;

        private double[][,] coef; // differential coefficients
        private double[] stepSize; // Relative step sizes




        /// <summary>
        ///   Gets or sets the function to be differentiated.
        /// </summary>
        /// 
        public Func<double[], double> Function { get; set; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="FiniteDifferences"/> class.
        /// </summary>
        /// <param name="variables">The number of free parameters in the function.</param>
        /// 
        public FiniteDifferences(int variables)
        {
            this.n = variables;
            this.stepSize = new double[variables];

            // Create interpolation coefficient table
            // for interpolated numerical differentation
            this.coef = createInterpolationCoefficients(3);
        }

        /// <summary>
        ///   Resets the relative step sizes of the approximation.
        /// </summary>
        /// 
        public void Reset()
        {
            for (int i = 0; i < stepSize.Length; i++)
                this.stepSize[i] = step;
        }

        /// <summary>
        ///   Computes the gradient at the given point <c>x</c>.
        /// </summary>
        /// <param name="x">The point where to compute the gradient.</param>
        /// <returns>The gradient of the function evaluated at point <c>x</c>.</returns>
        /// 
        public double[] Compute(double[] x)
        {
            if (x == null)
                throw new ArgumentNullException("x");

            if (x.Length != n)
                throw new ArgumentException("The number of dimensions does not match.", "x");


            double output = Function(x);

            double[] gradient = new double[x.Length];
            for (int i = 0; i < gradient.Length; i++)
                gradient[i] = derivative(x, i, output);

            return gradient;
        }

        /// <summary>
        ///   Computes the derivative at point <c>x_i</c>.
        /// </summary>
        /// 
        private double derivative(double[] x, int i, double output)
        {
            // Saves the original parameter value
            double original = x[i];

            stepSize[i] = (original != 0.0) ? step * System.Math.Abs(original) : step;

            // Create the interpolation points
            double[] points = new double[coef.Length];
            int center = (coef.Length - 1) / 2;

            for (int k = 0; k < coef.Length; k++)
            {
                if (k != center)
                {
                    // Make a small perturbation in the original parameter
                    x[i] = original + (k - center) * stepSize[i];

                    // Recompute the function to measure its importance
                    points[k] = Function(x);
                }
                else
                {
                    // The center point is the original function
                    points[k] = output;
                }
            }

            // Interpolate the points to obtain an
            //   estimation of the derivative at x.
            double derivative = 0.0;
            for (int j = 0; j < coef.Length; j++)
                derivative += coef[center][1, j] * points[j];
            derivative /= Math.Pow(stepSize[i], 1);

            // Reverts the modified value
            x[i] = original;

            return derivative;
        }


        /// <summary>
        ///   Creates the interpolation coefficients.
        /// </summary>
        /// <param name="points">The number of points in the tableau.</param>
        /// 
        private static double[][,] createInterpolationCoefficients(int points)
        {
            // Compute difference coefficient table
            double[][,] c = new double[points][,];

            double fac = Special.Factorial(points);

            for (int i = 0; i < points; i++)
            {
                double[,] deltas = new double[points, points];

                for (int j = 0; j < points; j++)
                {
                    double h = 1.0;
                    double delta = j - i;

                    for (int k = 0; k < points; k++)
                    {
                        deltas[j, k] = h / Special.Factorial(k);
                        h *= delta;
                    }
                }

                c[i] = Matrix.Inverse(deltas);
                for (int j = 0; j < points; j++)
                    for (int k = 0; k < points; k++)
                        c[i][j, k] = (Math.Round(c[i][j, k] * fac, MidpointRounding.AwayFromZero)) / fac;
            }

            return c;
        }


    }
}
