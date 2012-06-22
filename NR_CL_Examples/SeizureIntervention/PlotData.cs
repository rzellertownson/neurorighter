using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR_CL_Examples
{
    class PlotData
    {
        double[][] plotPoints;
        double plotThreshold;
        bool stimulationNow;

        /// <summary>
        /// Wrapper for plot data to be passed to BW.
        /// </summary>
        /// <param name="plotData"></param>
        /// <param name="plotThreshold"></param>
        public PlotData(double[][] plotPoints, double plotThreshold, bool stimulationNow)
        {
            this.plotPoints = plotPoints;
            this.plotThreshold = plotThreshold;
            this.stimulationNow = stimulationNow;
        }

        /// <summary>
        /// The current data to plot
        /// </summary>
        public double[][] PlotPoints
        {
            get
            {
                return plotPoints;
            }
        }

        /// <summary>
        /// The current threshold
        /// </summary>
        public double PlotThreshold
        {
            get
            {
                return plotThreshold;
            }
        }

        /// <summary>
        /// Are we in the middle of stimuation?
        /// </summary>
        public bool StimulationNow
        {
            get
            {
                return stimulationNow;
            }
        }
    }
}
