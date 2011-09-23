// Accord Statistics Library
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

namespace Accord.Statistics.Testing
{
    using System;
    using System.Collections.Generic;
    using Accord.Math;

    /// <summary>
    ///   One-way Analysis of Variance (ANOVA).
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The one-way ANOVA is a way to test for the equality of three or more means at the same
    ///   time by using variances. In its simplest form ANOVA provides a statistical test of whether
    ///   or not the means of several groups are all equal, and therefore generalizes t-test to more 
    ///   than two groups.</para>
    /// 
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
    ///       Wikipedia, The Free Encyclopedia. Analysis of variance. </a></description></item>
    ///     <item><description><a href="http://en.wikipedia.org/wiki/F_test">
    ///       Wikipedia, The Free Encyclopedia. F-Test. </a></description></item>
    ///     <item><description><a href="http://en.wikipedia.org/wiki/One-way_ANOVA">
    ///       Wikipedia, The Free Encyclopedia. One-way ANOVA. </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class OneWayAnova : IAnova
    {

        private int groupCount;

        private int[] sizes;
        private int totalSize;

        private double[] means;
        private double totalMean;

        private double Sb; // Between-group sum of squares
        private double Sw; // Within-group sum of squares
        private double St; // Total sum of squares

        private double MSb; // Between-group mean square
        private double MSw; // Within-group mean square

        private int Db; // Between-group degrees-of-freedom
        private int Dw; // Within-group degrees-of-freedom
        private int Dt; // Total degrees-of-freedom

        /// <summary>
        ///   Gets the F-Test produced by this one-way ANOVA.
        /// </summary>
        /// 
        public FTest FTest { get; private set; }

        /// <summary>
        ///   Gets the ANOVA results in the form of a table.
        /// </summary>
        /// 
        public AnovaSourceCollection Table { get; private set; }


        /// <summary>
        ///   Creates a new one-way ANOVA test.
        /// </summary>
        /// 
        /// <param name="samples">The sampled values.</param>
        /// <param name="labels">The independent, nominal variables.</param>
        /// 
        public OneWayAnova(double[] samples, int[] labels)
        {
            totalSize = samples.Length;
            groupCount = labels.Max();

            sizes = new int[groupCount];

            double[][] groups = new double[groupCount][];
            for (int i = 0; i < groups.Length; i++)
            {
                int[] idx = labels.Find(label => label == i);
                double[] group = samples.Submatrix(idx);

                groups[i] = group;
                sizes[i] = group.Length;
            }

            initialize(groups);
        }

        /// <summary>
        ///   Creates a new one-way ANOVA test.
        /// </summary>
        /// 
        /// <param name="samples">The grouped sampled values.</param>
        ///
        public OneWayAnova(params double[][] samples)
        {
            sizes = new int[samples.Length];

            groupCount = samples.Length;
            for (int i = 0; i < samples.Length; i++)
                totalSize += sizes[i] = samples[i].Length;

            initialize(samples);
        }

        private void initialize(double[][] samples)
        {
            Db = groupCount - 1;
            Dw = totalSize - groupCount;
            Dt = groupCount * totalSize - 1;

            // Step 1. Calculate the mean within each group
            means = Statistics.Tools.Mean(samples, 1);

            // Step 2. Calculate the overall mean
            totalMean = Statistics.Tools.Mean(means);


            // Step 3. Calculate the "between-group" sum of squares
            for (int i = 0; i < samples.Length; i++)
            {
                //  between-group sum of squares
                double u = (means[i] - totalMean);
                Sb += sizes[i] * u * u;
            }


            // Step 4. Calculate the "within-group" sum of squares
            for (int i = 0; i < samples.Length; i++)
            {
                for (int j = 0; j < samples[i].Length; j++)
                {
                    double u = samples[i][j] - means[i];
                    Sw += u * u;
                }
            }
            
            St = Sb + Sw; // total sum of squares


            // Step 5. Calculate the F statistic
            MSb = Sb / Db; // between-group mean square
            MSw = Sw / Dw; // within-group mean square
            FTest = new FTest(MSb / MSw, Db, Dw);


            // Step 6. Create the ANOVA table
            List<AnovaVariationSource> table = new List<AnovaVariationSource>();
            table.Add(new AnovaVariationSource(this, "Between-Groups", Sb, Db, FTest));
            table.Add(new AnovaVariationSource(this, "Within-Groups", Sw, Dw, null));
            table.Add(new AnovaVariationSource(this, "Total", St, Dt, null));
            this.Table = new AnovaSourceCollection(table);
        }

    }
}
