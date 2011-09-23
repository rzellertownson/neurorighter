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

namespace Accord.MachineLearning
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Accord.Math;

    /// <summary>
    ///   Delegate for Grid search fitting function.
    /// </summary>
    /// 
    /// <typeparam name="TModel">The type of the model to fit.</typeparam>
    /// 
    /// <param name="parameters">The collection of parameters to be used in the fitting process.</param>
    /// <param name="error">The error (or any other performance measure) returned by the model.</param>
    /// <returns>The model fitted to the data using the given parameters.</returns>
    /// 
    public delegate TModel GridSearchFittingFunction<TModel>(GridSearchParameterCollection parameters, out double error);

    /// <summary>
    ///   Grid Search for automatic parameter tuning.
    /// </summary>
    /// <remarks>
    ///   Grid Search tries to find the best combination of parameters across
    ///   a range of possible values that produces the best fit model. If there
    ///   are two parameters, each with 10 possible values, Grid Search will try
    ///   an exhaustive evaluation of the model using every combination of points,
    ///   resulting in 100 model fits.
    /// </remarks>
    /// 
    /// <typeparam name="TModel">The type of the model to be tuned.</typeparam>
    /// 
    /// <example>
    ///   How to fit a Kernel Support Vector Machine using Grid Search.
    ///   <code>
    ///   // Example binary data
    ///   double[][] inputs =
    ///   {
    ///       new double[] { -1, -1 },
    ///       new double[] { -1,  1 },
    ///       new double[] {  1, -1 },
    ///       new double[] {  1,  1 }
    ///   };
    ///
    ///   int[] xor = // xor labels
    ///   {
    ///       -1, 1, 1, -1
    ///   };
    ///
    ///   // Declare the parameters and ranges to be searched
    ///   GridSearchRange[] ranges = 
    ///   {
    ///       new GridSearchRange("complexity", new double[] { 0.00000001, 5.20, 0.30, 0.50 } ),
    ///       new GridSearchRange("degree",     new double[] { 1, 10, 2, 3, 4, 5 } ),
    ///       new GridSearchRange("constant",   new double[] { 0, 1, 2 } )
    ///   };
    ///
    ///
    ///   // Instantiate a new Grid Search algorithm for Kernel Support Vector Machines
    ///   var gridsearch = new GridSearch&lt;KernelSupportVectorMachine>(ranges);
    ///
    ///   // Set the fitting function for the algorithm
    ///   gridsearch.Fitting = delegate(GridSearchParameterCollection parameters, out double error)
    ///   {
    ///       // The parameters to be tried will be passed as a function parameter.
    ///       int degree = (int)parameters["degree"].Value;
    ///       double constant = parameters["constant"].Value;
    ///       double complexity = parameters["complexity"].Value;
    ///
    ///       // Use the parameters to build the SVM model
    ///       Polynomial kernel = new Polynomial(degree, constant);
    ///       KernelSupportVectorMachine ksvm = new KernelSupportVectorMachine(kernel, 2);
    ///
    ///       // Create a new learning algorithm for SVMs
    ///       SequentialMinimalOptimization smo = new SequentialMinimalOptimization(ksvm, inputs, xor);
    ///       smo.Complexity = complexity;
    ///
    ///       // Measure the model performance to return as an out parameter
    ///       error = smo.Run();
    ///
    ///       return ksvm; // Return the current model
    ///   };
    ///
    ///
    ///   // Declare some out variables to pass to the grid search algorithm
    ///   GridSearchParameterCollection bestParameters; double minError;
    ///
    ///   // Compute the grid search to find the best Support Vector Machine
    ///   KernelSupportVectorMachine bestModel = gridsearch.Compute(out bestParameters, out minError);
    ///   </code>
    /// </example>
    /// 
    [Serializable]
    public class GridSearch<TModel> where TModel : class
    {
        private GridSearchRangeCollection ranges;
        private GridSearchFittingFunction<TModel> fitting;

        /// <summary>
        ///   Constructs a new Grid search algorithm.
        /// </summary>
        /// 
        /// <param name="parameterRanges">The range of parameters to search.</param>
        /// 
        public GridSearch(params GridSearchRange[] parameterRanges)
        {
            this.ranges = new GridSearchRangeCollection(parameterRanges);
        }

        /// <summary>
        ///   A function that fits a model using the given parameters.
        /// </summary>
        /// 
        public GridSearchFittingFunction<TModel> Fitting
        {
            get { return fitting; }
            set { fitting = value; }
        }

        /// <summary>
        ///   The range of parameters to consider during search.
        /// </summary>
        /// 
        public GridSearchRangeCollection ParameterRanges
        {
            get { return ranges; }
        }


        /// <summary>
        ///   Searches for the best combination of parameters that results in the most accurate model.
        /// </summary>
        /// 
        /// <param name="bestParameters">The best combination of parameters found by the grid search.</param>
        /// <param name="error">The minimum error of the best model found by the grid search.</param>
        /// <returns>The best model found during the grid search.</returns>
        /// 
        public TModel Compute(out GridSearchParameterCollection bestParameters, out double error)
        {

            // Get the total number of different parameters
            var values = new GridSearchParameter[ranges.Count][];
            for (int i = 0; i < values.Length; i++)
                values[i] = ranges[i].GetParameters();


            // Generate the cartesian product between all parameters
            GridSearchParameter[][] grid = Matrix.CartesianProduct(values);


            // Initialize the search
            var parameters = new GridSearchParameterCollection[grid.Length];
            var models = new TModel[grid.Length];
            var errors = new double[grid.Length];
            int best;

            // Search the grid for the optimal parameters
            AForge.Parallel.For(0, grid.Length, i =>
            {
                // Get the current parameters for the current point
                parameters[i] = new GridSearchParameterCollection(grid[i]);

                // Try to fit a model using the parameters
                models[i] = Fitting(parameters[i], out errors[i]);
            });

            
            // Select the minimum error
            error = errors.Min(out best);
            bestParameters = parameters[best];

            // Return the best model found.
            return models[best];
        }

    }

    /// <summary>
    ///   Contains the name and value of a parameter that should be used during fitting.
    /// </summary>
    /// 
    [Serializable]
    public struct GridSearchParameter
    {
        private string name;
        private double value;

        /// <summary>
        ///   Gets the name of the parameter
        /// </summary>
        /// 
        public string Name { get { return name; } }

        /// <summary>
        ///   Gets the value of the parameter.
        /// </summary>
        /// 
        public double Value { get { return value; } }

        /// <summary>
        ///   Constructs a new parameter.
        /// </summary>
        /// 
        /// <param name="name">The name for the parameter.</param>
        /// <param name="value">The value for the parameter.</param>
        /// 
        public GridSearchParameter(string name, double value)
        {
            this.name = name;
            this.value = value;
        }

        /// <summary>
        ///   Determines whether the specified object is equal
        ///   to the current GridSearchParameter object.
        /// </summary>
        /// 
        public override bool Equals(object obj)
        {
            if (obj is GridSearchParameter)
            {
                GridSearchParameter g = (GridSearchParameter)obj;
                if (g.name != name || g.value != value)
                    return false;
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Returns the hash code for this GridSearchParameter
        /// </summary>
        /// 
        public override int GetHashCode()
        {
            return name.GetHashCode() ^ value.GetHashCode();
        }

        /// <summary>
        ///   Compares two GridSearchParameters for equality.
        /// </summary>
        /// 
        public static bool operator ==(GridSearchParameter parameter1, GridSearchParameter parameter2)
        {
            return (parameter1.name == parameter2.name && parameter1.value == parameter2.value);
        }

        /// <summary>
        ///   Compares two GridSearchParameters for inequality.
        /// </summary>
        /// 
        public static bool operator !=(GridSearchParameter parameter1, GridSearchParameter parameter2)
        {
            return (parameter1.name != parameter2.name || parameter1.value != parameter2.value);
        }
    }

    /// <summary>
    ///   Represents a range of parameters to be tried during a grid search.
    /// </summary>
    /// 
    [Serializable]
    public class GridSearchRange
    {

        /// <summary>
        ///   Gets or sets the name of the parameter from which the range belongs to.
        /// </summary>
        /// 
        public string Name { get; set; }

        /// <summary>
        ///   Gets or sets the range of values that should be tested for this parameter.
        /// </summary>
        /// 
        public double[] Values { get; set; }

        /// <summary>
        ///   Constructs a new GridsearchRange object.
        /// </summary>
        /// 
        /// <param name="name">The name for this parameter.</param>
        /// <param name="start">The starting value for this range.</param>
        /// <param name="end">The end value for this range.</param>
        /// <param name="step">The step size for this range.</param>
        /// 
        public GridSearchRange(string name, double start, double end, double step)
        {
            this.Name = name;
            this.Values = Matrix.Interval(start, end, step);
        }

        /// <summary>
        ///   Constructs a new GridSearchRange object.
        /// </summary>
        /// 
        /// <param name="name">The name for this parameter.</param>
        /// <param name="values">The array of values to try.</param>
        /// 
        public GridSearchRange(string name, double[] values)
        {
            this.Name = name;
            this.Values = values;
        }

        /// <summary>
        ///   Gets the array of GridSearchParameters to try.
        /// </summary>
        /// 
        public GridSearchParameter[] GetParameters()
        {
            GridSearchParameter[] parameters = new GridSearchParameter[Values.Length];
            for (int i = 0; i < Values.Length; i++)
                parameters[i] = new GridSearchParameter(Name, Values[i]);
            return parameters;
        }

    }



    /// <summary>
    ///   GridSearchRange collection.
    /// </summary>
    /// 
    [Serializable]
    public class GridSearchRangeCollection : KeyedCollection<string, GridSearchRange>
    {
        /// <summary>
        ///   Constructs a new collection of GridsearchRange objects.
        /// </summary>
        /// 
        public GridSearchRangeCollection(params GridSearchRange[] ranges)
        {
            foreach (var range in ranges)
                this.Add(range);
        }

        /// <summary>
        ///   Returns the identifying value for an item on this collection.
        /// </summary>
        /// 
        protected override string GetKeyForItem(GridSearchRange item)
        {
            return item.Name;
        }

        /// <summary>
        ///   Adds a parameter range to the end of the GridsearchRangeCollection.
        /// </summary>
        /// 
        public void Add(string name, params double[] values)
        {
            this.Add(new GridSearchRange(name, values));
        }
    }

    /// <summary>
    ///   GridsearchParameter collection.
    /// </summary>
    /// 
    [Serializable]
    public class GridSearchParameterCollection : KeyedCollection<string, GridSearchParameter>
    {
        /// <summary>
        ///   Constructs a new collection of GridsearchParameter objects.
        /// </summary>
        /// 
        public GridSearchParameterCollection(params GridSearchParameter[] parameters)
        {
            foreach (GridSearchParameter param in parameters)
                this.Add(param);
        }

        /// <summary>
        ///   Constructs a new collection of GridsearchParameter objects.
        /// </summary>
        /// 
        public GridSearchParameterCollection(IEnumerable<GridSearchParameter> parameters)
        {
            foreach (GridSearchParameter param in parameters)
                this.Add(param);
        }

        /// <summary>
        ///   Returns the identifying value for an item on this collection.
        /// </summary>
        /// 
        protected override string GetKeyForItem(GridSearchParameter item)
        {
            return item.Name;
        }
    }

}
