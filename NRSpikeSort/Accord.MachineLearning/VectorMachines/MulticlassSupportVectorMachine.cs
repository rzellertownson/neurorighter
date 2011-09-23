// Accord Machine Learning Library
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

namespace Accord.MachineLearning.VectorMachines
{
    using System;
    using Accord.Math;
    using Accord.Statistics.Kernels;

    /// <summary>
    ///   One-against-one Multi-class Kernel Support Vector Machine Classifier.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The Support Vector Machine is by nature a binary classifier. One of the ways
    ///   to extend the original SVM algorithm to multiple classes is to build a one-
    ///   against-one scheme where multiple SVMs specialize to recognize each of the
    ///   available classes. By using a competition scheme, the original multi-class
    ///   classification problem is then reduced to <c>n*(n/2)</c> smaller binary problems.</para>
    /// <para>
    ///   Currently this class supports only Kernel machines as the underlying classifiers.
    ///   If a Linear Support Vector Machine is needed, specify a Linear kernel in the
    ///   constructor at the moment of creation. </para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       <a href="http://courses.media.mit.edu/2006fall/mas622j/Projects/aisen-project/index.html">
    ///        http://courses.media.mit.edu/2006fall/mas622j/Projects/aisen-project/index.html</a></description></item>
    ///     <item><description>
    ///       <a href="http://nlp.stanford.edu/IR-book/html/htmledition/multiclass-svms-1.html">
    ///        http://nlp.stanford.edu/IR-book/html/htmledition/multiclass-svms-1.html</a></description></item>
    ///     </list></para>
    ///     
    /// </remarks>
    ///
    /// <example>
    ///   <code>
    ///   // Sample data
    ///   //   The following is simple auto association function
    ///   //   where each input correspond to its own class. This
    ///   //   problem should be easily solved by a Linear kernel.
    ///
    ///   // Sample input data
    ///   double[][] inputs =
    ///   {
    ///       new double[] { 0 },
    ///       new double[] { 3 },
    ///       new double[] { 1 },
    ///       new double[] { 2 },
    ///   };
    ///   
    ///   // Output for each of the inputs
    ///   int[] outputs = { 0, 3, 1, 2 };
    ///   
    ///   
    ///   // Create a new Linear kernel
    ///   IKernel kernel = new Linear();
    ///   
    ///   // Create a new Multi-class Support Vector Machine with one input,
    ///   //  using the linear kernel and for four disjoint classes.
    ///   var machine = new MulticlassSupportVectorMachine(1, kernel, 4);
    ///   
    ///   // Create the Multi-class learning algorithm for the machine
    ///   var teacher = new MulticlassSupportVectorLearning(machine, inputs, outputs);
    ///   
    ///   // Configure the learning algorithm to use SMO to train the
    ///   //  underlying SVMs in each of the binary class subproblems.
    ///   teacher.Algorithm = (svm, classInputs, classOutputs, i, j) =>
    ///       new SequentialMinimalOptimization(svm, classInputs, classOutputs);
    ///   
    ///   // Run the learning algorithm
    ///   double error = teacher.Run();
    ///   </code>
    /// </example>
    ///
    /// <seealso cref="Learning.MulticlassSupportVectorLearning"/>
    ///
    [Serializable]
    public class MulticlassSupportVectorMachine : ISupportVectorMachine
    {

        // Underlying classifiers
        KernelSupportVectorMachine[][] machines;


        /// <summary>
        ///   Constructs a new Multi-class Kernel Support Vector Machine
        /// </summary>
        /// <param name="kernel">The chosen kernel for the machine.</param>
        /// <param name="inputs">The number of inputs for the machine.</param>
        /// <param name="classes">The number of classes in the classification problem.</param>
        /// <remarks>
        ///   If the number of inputs is zero, this means the machine
        ///   accepts a indefinite number of inputs. This is often the
        ///   case for kernel vector machines using a sequence kernel.
        /// </remarks>
        public MulticlassSupportVectorMachine(int inputs, IKernel kernel, int classes)
        {
            if (classes <= 1)
            {
                throw new ArgumentException("The machine must have at least two classes.", "classes");
            }

            // Create the kernel machines
            machines = new KernelSupportVectorMachine[classes - 1][];
            for (int i = 0; i < classes - 1; i++)
            {
                machines[i] = new KernelSupportVectorMachine[i + 1];
                for (int j = 0; j <= i; j++)
                {
                    machines[i][j] = new KernelSupportVectorMachine(kernel, inputs);
                }
            }
        }

        /// <summary>
        ///   Constructs a new Multi-class Kernel Support Vector Machine
        /// </summary>
        /// <param name="machines">
        ///   The machines to be used in each of the pairwise class subproblems.
        /// </param>
        public MulticlassSupportVectorMachine(KernelSupportVectorMachine[][] machines)
        {
            if (machines == null) throw new ArgumentNullException("machines");

            this.machines = machines;
        }

        /// <summary>
        ///   Gets the classifier for <paramref name="class1"/> against <paramref name="class2"/>.
        /// </summary>
        /// <remarks>
        ///   If the index of <paramref name="class1"/> is greater than <paramref name="class2"/>,
        ///   the classifier for the <paramref name="class2"/> against <paramref name="class1"/>
        ///   will be returned instead. If both indices are equal, null will be
        ///   returned instead.
        /// </remarks>
        public KernelSupportVectorMachine this[int class1, int class2]
        {
            get
            {
                if (class1 == class2)
                    return null;
                if (class1 > class2)
                    return machines[class1 - 1][class2];
                else
                    return machines[class2 - 1][class1];
            }
        }

        /// <summary>
        ///   Gets the number of classes.
        /// </summary>
        public int Classes
        {
            get { return machines.Length + 1; }
        }

        /// <summary>
        ///   Gets the number of inputs of the machines.
        /// </summary>
        public int Inputs
        {
            get { return machines[0][0].Inputs; }
        }

        /// <summary>
        ///   Gets the subproblems classifiers.
        /// </summary>
        public KernelSupportVectorMachine[][] Machines
        {
            get { return machines; }
        }

        /// <summary>
        ///   Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <returns>The output for the given input.</returns>
        double ISupportVectorMachine.Compute(double[] inputs)
        {
            return Compute(inputs);
        }

        /// <summary>
        ///   Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <returns>The output for the given input.</returns>
        public int Compute(double[] inputs)
        {
            int[] votes;
            return Compute(inputs, out votes);
        }

        /// <summary>
        ///   Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="votes">A vector containing the number of votes for each class.</param>
        /// <returns>The output for the given input.</returns>
        public int Compute(double[] inputs, out int[] votes)
        {
            // out variables cannot be passed into delegates,
            // so will be creating a copy for the vote array.
            int[] voting = new int[this.Classes];


            // For each class
            AForge.Parallel.For(0, Classes, i =>
            {
                // For each other class
                for (int j = 0; j < i; j++)
                {
                    KernelSupportVectorMachine machine = this[i, j];

                    double answer = machine.Compute(inputs);

                    // Compute the two-class problem
                    if (answer < 0)
                    {
                        voting[i] += 1; // Class i has won
                    }
                    else
                    {
                        voting[j] += 1; // Class j has won
                    }
                }
            });

            // Voting finished.
            votes = voting;

            // Select class which maximum number of votes
            int output; Matrix.Max(votes, out output);

            return output; // Return as the output.
        }

    }
}
