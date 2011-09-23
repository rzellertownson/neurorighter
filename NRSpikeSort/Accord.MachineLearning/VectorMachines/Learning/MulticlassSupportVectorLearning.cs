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

namespace Accord.MachineLearning.VectorMachines.Learning
{
    using System;
    using Accord.Math;

    /// <summary>
    ///   Configuration function to configure the learning algorithms
    ///   for each of the Kernel Support Vector Machines used in this
    ///   Multi-class Support Vector Machine.
    /// </summary>
    /// 
    /// <param name="inputs">The input data for the learning algorithm.</param>
    /// <param name="outputs">The output data for the learning algorithm.</param>
    /// <param name="machine">The machine for the learning algorithm.</param>
    /// <param name="class1">The class index corresponding to the negative values
    ///     in the output values contained in <paramref name="outputs"/>.</param>
    /// <param name="class2">The class index corresponding to the positive values
    ///     in the output values contained in <paramref name="outputs"/>.</param>
    ///     
    /// <returns>
    ///   The configured <see cref="ISupportVectorMachineLearning"/> algorithm
    ///   to be used to train the given <see cref="KernelSupportVectorMachine"/>.
    /// </returns>
    /// 
    public delegate ISupportVectorMachineLearning SupportVectorMachineLearningConfigurationFunction(
      KernelSupportVectorMachine machine, double[][] inputs, int[] outputs, int class1, int class2);


    /// <summary>
    ///   One-against-one Multi-class Support Vector Machine Learning Algorithm
    /// </summary>
    /// 
    /// <remarks>
    ///   This class can be used to train Kernel Support Vector Machines with
    ///   any algorithm using a one-against-one strategy. The underlying training
    ///   algorithm can be configured by defining the Configure delegate.
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
    public class MulticlassSupportVectorLearning : ISupportVectorMachineLearning
    {
        // Training data
        private double[][] inputs;
        private int[] outputs;

        // Machine
        private MulticlassSupportVectorMachine msvm;

        // Training configuration function
        private SupportVectorMachineLearningConfigurationFunction configure;


        /// <summary>
        ///   Constructs a new Multi-class Support Vector Learning algorithm.
        /// </summary>
        /// 
        public MulticlassSupportVectorLearning(MulticlassSupportVectorMachine machine,
            double[][] inputs, int[] outputs)
        {

            // Initial argument checking
            if (machine == null)
                throw new ArgumentNullException("machine");

            if (inputs == null)
                throw new ArgumentNullException("inputs");

            if (outputs == null)
                throw new ArgumentNullException("outputs");

            if (inputs.Length != outputs.Length)
                throw new ArgumentException("The number of inputs and outputs does not match.", "outputs");

            if (machine.Inputs > 0)
            {
                // This machine has a fixed input vector size
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i].Length != machine.Inputs)
                        throw new ArgumentException("The size of the input vectors does not match the expected number of inputs of the machine");
            }

            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] < 0 || outputs[i] >= machine.Classes)
                    throw new ArgumentException("Some output values are outside of the expected class label ranges.", "outputs");
            }


            // Machine
            this.msvm = machine;

            // Learning data
            this.inputs = inputs;
            this.outputs = outputs;

        }

        /// <summary>
        ///   Gets or sets the configuration function for the learning algorithm.
        /// </summary>
        /// 
        /// <remarks>
        ///   The configuration function should return a properly configured ISupportVectorMachineLearning
        ///   algorithm using the given support vector machine and the input and output data.
        /// </remarks>
        /// 
        public SupportVectorMachineLearningConfigurationFunction Algorithm
        {
            get { return configure; }
            set { configure = value; }
        }

        /// <summary>
        ///   Runs the one-against-one learning algorithm.
        /// </summary>
        /// 
        public double Run()
        {
            return Run(true);
        }

        /// <summary>
        ///   Runs the one-against-one learning algorithm.
        /// </summary>
        /// 
        /// <param name="computeError">
        ///   True to compute error after the training
        ///   process completes, false otherwise. Default is true.
        /// </param>
        /// 
        /// <returns>
        ///   The sum of squares error rate for
        ///   the resulting support vector machine.
        /// </returns>
        /// 
        public double Run(bool computeError)
        {
            // For each class i
            AForge.Parallel.For(0, msvm.Classes, i =>
            {
                // For each class j
                for (int j = 0; j < i; j++)
                {
                    // Retrieve the associated machine
                    var machine = msvm[i, j];

                    // Retrieve the associated classes
                    int[] idx = outputs.Find(x => x == i || x == j);

                    double[][] subInputs = inputs.Submatrix(idx);
                    int[] subOutputs = outputs.Submatrix(idx);


                    // Transform in a two-class problem
                    subOutputs.ApplyInPlace(x => x = (x == i) ? -1 : 1);

                    // Train the machine on the two-class problem.
                    configure(machine, subInputs, subOutputs, i, j).Run(false);
                }
            });


            // Compute error if required.
            return (computeError) ? ComputeError(inputs, outputs) : 0.0;
        }

        /// <summary>
        ///   Compute the error ratio.
        /// </summary>
        /// 
        public double ComputeError(double[][] inputs, int[] expectedOutputs)
        {
            // Compute errors
            int count = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                double y = msvm.Compute(inputs[i]);

                if (y != expectedOutputs[i])
                    count++;
            }

            // Return misclassification error ratio
            return (double)count / inputs.Length;
        }

    }

}
