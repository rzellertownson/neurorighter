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
    using System.Collections.Generic;
    using Accord.Statistics.Kernels;

    /// <summary>
    ///   Sequential Minimal Optimization (SMO) Algorithm
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The SMO algorithm is an algorithm for solving large quadratic programming (QP)
    ///   optimization problems, widely used for the training of support vector machines.
    ///   First developed by John C. Platt in 1998, SMO breaks up large QP problems into
    ///   a series of smallest possible QP problems, which are then solved analytically.</para>
    /// <para>
    ///   This class follows the original algorithm by Platt as strictly as possible.</para>
    ///  
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       <a href="http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization">
    ///       Wikipedia, The Free Encyclopedia. Sequential Minimal Optimization. Available on:
    ///       http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization </a></description></item>
    ///     <item><description>
    ///       <a href="http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf">
    ///       John C. Platt, Sequential Minimal Optimization: A Fast Algorithm for Training Support
    ///       Vector Machines. 1998. Available on: http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf </a></description></item>
    ///     <item><description>
    ///       <a href="http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf">
    ///       J. P. Lewis. A Short SVM (Support Vector Machine) Tutorial. Available on:
    ///       http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf </a></description></item>
    ///     </list></para>  
    /// </remarks>
    /// 
    /// <example>
    ///   <code>
    ///   // Example XOR problem
    ///   double[][] inputs =
    ///   {
    ///       new double[] { 0, 0 }, // 0 xor 0: 1 (label +1)
    ///       new double[] { 0, 1 }, // 0 xor 1: 0 (label -1)
    ///       new double[] { 1, 0 }, // 1 xor 0: 0 (label -1)
    ///       new double[] { 1, 1 }  // 1 xor 1: 1 (label +1)
    ///   };
    ///    
    ///   // Dichotomy SVM outputs should be given as [-1;+1]
    ///   int[] labels =
    ///   {
    ///          1, -1, -1, 1
    ///   };
    ///  
    ///   // Create a Kernel Support Vector Machine for the given inputs
    ///   KernelSupportVectorMachine machine = new KernelSupportVectorMachine(new Gaussian(0.1), inputs[0].Length);
    /// 
    ///   // Instantiate a new learning algorithm for SVMs
    ///   SequentialMinimalOptimization smo = new SequentialMinimalOptimization(machine, inputs, labels);
    /// 
    ///   // Set up the learning algorithm
    ///   smo.Complexity = 1.0;
    /// 
    ///   // Run the learning algorithm
    ///   double error = smo.Run();
    ///   
    ///   // Compute the decision output for one of the input vectors
    ///   int decision = System.Math.Sign(svm.Compute(inputs[0]));
    ///  </code>
    /// </example>
    /// 
    public class SequentialMinimalOptimization : ISupportVectorMachineLearning
    {
        private static Random random = new Random();

        // Training data
        private double[][] inputs;
        private int[] outputs;

        // Learning algorithm parameters
        private double c = 1.0;
        private double tolerance = 1e-3;
        private double epsilon = 1e-3;
        private bool useComplexityHeuristic;

        // Support Vector Machine parameters
        private SupportVectorMachine machine;
        private IKernel kernel;
        private double[] alpha;
        private double bias;


        // Error cache to speed up computations
        private double[] errors;


        /// <summary>
        ///   Initializes a new instance of a Sequential Minimal Optimization (SMO) algorithm.
        /// </summary>
        /// 
        /// <param name="machine">A Support Vector Machine.</param>
        /// <param name="inputs">The input data points as row vectors.</param>
        /// <param name="outputs">The classification label for each data point in the range [-1;+1].</param>
        /// 
        public SequentialMinimalOptimization(SupportVectorMachine machine,
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

            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] != 1 && outputs[i] != -1)
                    throw new ArgumentOutOfRangeException("outputs", "One of the labels in the output vector is neither +1 or -1.");
            }

            if (machine.Inputs > 0)
            {
                // This machine has a fixed input vector size
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i].Length != machine.Inputs)
                        throw new ArgumentException("The size of the input vectors does not match the expected number of inputs of the machine");
            }


            // Machine
            this.machine = machine;

            // Kernel (if applicable)
            KernelSupportVectorMachine ksvm = machine as KernelSupportVectorMachine;
            this.kernel = (ksvm != null) ? ksvm.Kernel : new Linear();


            // Learning data
            this.inputs = inputs;
            this.outputs = outputs;

        }


        //---------------------------------------------


        #region Properties
        /// <summary>
        ///   Complexity (cost) parameter C. Increasing the value of C forces the creation
        ///   of a more accurate model that may not generalize well. Default value is the
        ///   number of examples divided by the trace of the kernel matrix.
        /// </summary>
        /// <remarks>
        ///   The cost parameter C controls the trade off between allowing training
        ///   errors and forcing rigid margins. It creates a soft margin that permits
        ///   some misclassifications. Increasing the value of C increases the cost of
        ///   misclassifying points and forces the creation of a more accurate model
        ///   that may not generalize well.
        /// </remarks>
        public double Complexity
        {
            get { return this.c; }
            set { this.c = value; }
        }


        /// <summary>
        ///   Gets or sets a value indicating whether the Complexity parameter C
        ///   should be computed automatically by employing an heuristic rule.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if complexity should be computed automatically; otherwise, <c>false</c>.
        /// </value>
        public bool UseComplexityHeuristic
        {
            get { return useComplexityHeuristic; }
            set { useComplexityHeuristic = value; }
        }

        /// <summary>
        ///   Insensitivity zone ε. Increasing the value of ε can result in fewer support
        ///   vectors in the created model. Default value is 1e-3.
        /// </summary>
        /// <remarks>
        ///   Parameter ε controls the width of the ε-insensitive zone, used to fit the training
        ///   data. The value of ε can affect the number of support vectors used to construct the
        ///   regression function. The bigger ε, the fewer support vectors are selected. On the
        ///   other hand, bigger ε-values results in more flat estimates.
        /// </remarks>
        public double Epsilon
        {
            get { return epsilon; }
            set { epsilon = value; }
        }

        /// <summary>
        ///   Convergence tolerance. Default value is 1e-3.
        /// </summary>
        /// <remarks>
        ///   The criterion for completing the model training process. The default is 0.001.
        /// </remarks>
        public double Tolerance
        {
            get { return this.tolerance; }
            set { this.tolerance = value; }
        }
        #endregion


        //---------------------------------------------


        /// <summary>
        ///   Runs the SMO algorithm.
        /// </summary>
        /// 
        /// <param name="computeError">
        ///   True to compute error after the training
        ///   process completes, false otherwise. Default is true.
        /// </param>
        /// 
        /// <returns>
        ///   The misclassification error rate of
        ///   the resulting support vector machine.
        /// </returns>
        /// 
        public double Run(bool computeError)
        {

            // The SMO algorithm chooses to solve the smallest possible optimization problem
            // at every step. At every step, SMO chooses two Lagrange multipliers to jointly
            // optimize, finds the optimal values for these multipliers, and updates the SVM
            // to reflect the new optimal values
            //
            // Reference: http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf


            // Initialize variables
            int N = inputs.Length;

            if (useComplexityHeuristic)
                c = computeComplexity();

            // Lagrange multipliers
            this.alpha = new double[N];

            // Error cache
            this.errors = new double[N];

            // Algorithm:
            int numChanged = 0;
            int examineAll = 1;

            while (numChanged > 0 || examineAll > 0)
            {
                numChanged = 0;
                if (examineAll > 0)
                {
                    // loop I over all training examples
                    for (int i = 0; i < N; i++)
                        numChanged += examineExample(i);
                }
                else
                {
                    // loop I over examples where alpha is not 0 and not C
                    for (int i = 0; i < N; i++)
                        if (alpha[i] != 0 && alpha[i] != c)
                            numChanged += examineExample(i);
                }

                if (examineAll == 1)
                    examineAll = 0;
                else if (numChanged == 0)
                    examineAll = 1;
            }


            // Store Support Vectors in the SV Machine. Only vectors which have lagrange multipliers
            // greater than zero will be stored as only those are actually required during evaluation.
            List<int> indices = new List<int>();
            for (int i = 0; i < N; i++)
            {
                // Only store vectors with multipliers > 0
                if (alpha[i] > 0) indices.Add(i);
            }

            int vectors = indices.Count;
            machine.SupportVectors = new double[vectors][];
            machine.Weights = new double[vectors];
            for (int i = 0; i < vectors; i++)
            {
                int j = indices[i];
                machine.SupportVectors[i] = inputs[j];
                machine.Weights[i] = alpha[j] * outputs[j];
            }
            machine.Threshold = -bias;


            // Compute error if required.
            return (computeError) ? ComputeError(inputs, outputs) : 0.0;
        }

        /// <summary>
        ///   Runs the SMO algorithm.
        /// </summary>
        /// 
        /// <returns>
        ///   The misclassification error rate of
        ///   the resulting support vector machine.
        /// </returns>
        /// 
        public double Run()
        {
            return Run(true);
        }

        /// <summary>
        ///   Computes the error rate for a given set of input and outputs.
        /// </summary>
        /// 
        public double ComputeError(double[][] inputs, int[] expectedOutputs)
        {
            // Compute errors
            int count = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (Math.Sign(compute(inputs[i])) != Math.Sign(expectedOutputs[i]))
                    count++;
            }

            // Return misclassification error ratio
            return (double)count / inputs.Length;
        }

        //---------------------------------------------


        /// <summary>
        ///  Chooses which multipliers to optimize using heuristics.
        /// </summary>
        /// 
        private int examineExample(int i2)
        {
            double[] p2 = inputs[i2]; // Input point at index i2
            double y2 = outputs[i2];  // Classification label for p2
            double alph2 = alpha[i2];    // Lagrange multiplier for p2

            // SVM output on p2 - y2. Check if it has already been computed
            double e2 = (alph2 > 0 && alph2 < c) ? errors[i2] : compute(p2) - y2;

            double r2 = y2 * e2;


            // Heuristic 01 (for the first multiplier choice):
            //  - Testing for KKT conditions within the tolerance margin
            if (!(r2 < -tolerance && alph2 < c) && !(r2 > tolerance && alph2 > 0))
                return 0;


            // Heuristic 02 (for the second multiplier choice):
            //  - Once a first Lagrange multiplier is chosen, SMO chooses the second Lagrange multiplier to
            //    maximize the size of the step taken during joint optimization. Now, evaluating the kernel
            //    function is time consuming, so SMO approximates the step size by the absolute value of the
            //    absolute error difference.
            int i1 = -1; double max = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (alpha[i] > 0 && alpha[i] < c)
                {
                    double error1 = errors[i];
                    double aux = System.Math.Abs(e2 - error1);

                    if (aux > max)
                    {
                        max = aux;
                        i1 = i;
                    }
                }
            }

            if (i1 >= 0 && takeStep(i1, i2)) return 1;


            // Heuristic 03:
            //  - Under unusual circumstances, SMO cannot make positive progress using the second
            //    choice heuristic above. If it is the case, then SMO starts iterating through the
            //    non-bound examples, searching for an second example that can make positive progress.

            int start = random.Next(inputs.Length);
            for (i1 = start; i1 < inputs.Length; i1++)
            {
                if (alpha[i1] > 0 && alpha[i1] < c)
                    if (takeStep(i1, i2)) return 1;
            }
            for (i1 = 0; i1 < start; i1++)
            {
                if (alpha[i1] > 0 && alpha[i1] < c)
                    if (takeStep(i1, i2)) return 1;
            }


            // Heuristic 04:
            //  - If none of the non-bound examples make positive progress, then SMO starts iterating
            //    through the entire training set until an example is found that makes positive progress.
            //    Both the iteration through the non-bound examples and the iteration through the entire
            //    training set are started at random locations, in order not to bias SMO towards the
            //    examples at the beginning of the training set. 

            start = random.Next(inputs.Length);
            for (i1 = start; i1 < inputs.Length; i1++)
            {
                if (takeStep(i1, i2)) return 1;
            }
            for (i1 = 0; i1 < start; i1++)
            {
                if (takeStep(i1, i2)) return 1;
            }


            // In extremely degenerate circumstances, none of the examples will make an adequate second
            // example. When this happens, the first example is skipped and SMO continues with another
            // chosen first example.
            return 0;
        }

        /// <summary>
        ///   Analytically solves the optimization problem for two Lagrange multipliers.
        /// </summary>
        /// 
        private bool takeStep(int i1, int i2)
        {
            if (i1 == i2) return false;

            double[] p1 = inputs[i1]; // Input point at index i1
            double alph1 = alpha[i1];    // Lagrange multiplier for p1
            double y1 = outputs[i1];  // Classification label for p1

            // SVM output on p1 - y1. Check if it has already been computed
            double e1 = (alph1 > 0 && alph1 < c) ? errors[i1] : compute(p1) - y1;

            double[] p2 = inputs[i2]; // Input point at index i2
            double alph2 = alpha[i2];    // Lagrange multiplier for p2
            double y2 = outputs[i2];  // Classification label for p2

            // SVM output on p2 - y2. Check if it has already been computed
            double e2 = (alph2 > 0 && alph2 < c) ? errors[i2] : compute(p2) - y2;


            double s = y1 * y2;


            // Compute L and H according to equations (13) and (14) (Platt, 1998)
            double L, H;
            if (y1 != y2)
            {
                // If the target y1 does not equal the target           (13)
                // y2, then the following bounds apply to a2:
                L = Math.Max(0, alph2 - alph1);
                H = Math.Min(c, c + alph2 - alph1);
            }
            else
            {
                // If the target y1 does equal the target               (14)
                // y2, then the following bounds apply to a2:
                L = Math.Max(0,  alph2 + alph1 - c);
                H = Math.Min(c,  alph2 + alph1);
            }

            if (L == H) return false;

            double k11, k22, k12, eta;
            k11 = kernel.Function(p1, p1);
            k12 = kernel.Function(p1, p2);
            k22 = kernel.Function(p2, p2);
            eta = k11 + k22 - 2.0 * k12;

            double a1, a2;

            if (eta > 0)
            {
                a2 = alph2 - y2 * (e2 - e1) / eta;

                if (a2 < L) a2 = L;
                else if (a2 > H) a2 = H;
            }
            else
            {
                // Compute objective function Lobj and Hobj at
                //  a2=L and a2=H respectively, using (eq. 19)

                double L1 = alph1 + s * (alph2 - L);
                double H1 = alph1 + s * (alph2 - H);
                double f1 = y1 * (e1 + bias) - alph1 * k11 - s * alph2 * k12;
                double f2 = y2 * (e2 + bias) - alph2 * k22 - s * alph1 * k12;
                double Lobj = -0.5 * L1 * L1 * k11 - 0.5 * L * L * k22 - s * L * L1 * k12 - L1 * f1 - L * f2;
                double Hobj = -0.5 * H1 * H1 * k11 - 0.5 * H * H * k22 - s * H * H1 * k12 - H1 * f1 - H * f2;

                if (Lobj > Hobj + epsilon) a2 = L;
                else if (Lobj < Hobj - epsilon) a2 = H;
                else a2 = alph2;
            }

            if (Math.Abs(a2 - alph2) < epsilon * (a2 + alph2 + epsilon))
                return false;

            a1 = alph1 + s * (alph2 - a2);

            if (a1 < 0)
            {
                a2 += s * a1;
                a1 = 0;
            }
            else if (a1 > c)
            {
                double d = a1 - c;
                a2 += s * d;
                a1 = c;
            }


            // Update threshold (bias) to reflect change in Lagrange multipliers
            double b1 = 0, b2 = 0;
            double new_b = 0, delta_b;

            if (a1 > 0 && a1 < c)
            {
                // a1 is not at bounds
                new_b = e1 + y1 * (a1 - alph1) * k11 + y2 * (a2 - alph2) * k12 + bias;
            }
            else
            {
                if (a2 > 0 && a2 < c)
                {
                    // a1 is at bounds but a2 is not.
                    new_b = e2 + y1 * (a1 - alph1) * k12 + y2 * (a2 - alph2) * k22 + bias;
                }
                else
                {
                    // Both new Lagrange multipliers are at bound. SMO algorithm
                    // chooses the threshold to be halfway in between b1 and b2.
                    b1 = e1 + y1 * (a1 - alph1) * k11 + y2 * (a2 - alph2) * k12 + bias;
                    b2 = e2 + y1 * (a1 - alph1) * k12 + y2 * (a2 - alph2) * k22 + bias;
                    new_b = (b1 + b2) / 2;
                }
            }

            delta_b = new_b - bias;
            bias = new_b;


            // Update error cache using new Lagrange multipliers
            double t1 = y1 * (a1 - alph1);
            double t2 = y2 * (a2 - alph2);

            for (int i = 0; i < inputs.Length; i++)
            {
                if (0 < alpha[i] && alpha[i] < c)
                {
                    double[] point = inputs[i];
                    errors[i] +=
                          t1 * kernel.Function(p1, point) +
                          t2 * kernel.Function(p2, point) -
                          delta_b;
                }
            }

            errors[i1] = 0f;
            errors[i2] = 0f;


            // Update lagrange multipliers
            alpha[i1] = a1;
            alpha[i2] = a2;


            return true;
        }

        /// <summary>
        ///   Computes the SVM output for a given point.
        /// </summary>
        /// 
        private double compute(double[] point)
        {
            double sum = -bias;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (alpha[i] > 0)
                    sum += alpha[i] * outputs[i] * kernel.Function(inputs[i], point);
            }

            return sum;
        }


        private double computeComplexity()
        {
            // Compute initial value for C as the number of examples
            // divided by the trace of the input sample kernel matrix.
            double sum = 0.0;
            for (int i = 0; i < inputs.Length; i++)
                sum += kernel.Function(inputs[i], inputs[i]);
            return inputs.Length / sum;
        }
    }
}
