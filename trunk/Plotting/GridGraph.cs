using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter.Plotting
{
    internal partial class GridGraph : UserControl
    {
        private double minX = 0.0;
        private double minY = 0.0;
        private double maxX = 1.0;
        private double maxY = 1.0;
        private double dX = 1.0;
        private double dY = 1.0;
        private Color lineColor = Color.Lime;
        private Color gridColor = Color.White;
        private float penWidth = 1;
        private int numRows = 4;
        private int numCols = 4;

        //private Graphics onScreenG;
        private Pen tracePen;
        private Pen gridPen;

        struct LineSkeleton
        {
            public Point[] lines;
            public Boolean isGrid;
        }
        List<LineSkeleton> linesToPlot;

        internal GridGraph()
        {
            InitializeComponent();

            //onScreenG = panel.CreateGraphics();
            tracePen = new Pen(lineColor);
            tracePen.Width = penWidth;
            gridPen = new Pen(gridColor);
            gridPen.Width = penWidth;

            linesToPlot = new List<LineSkeleton>(4);

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        internal void setMinMax(double minX, double maxX, double minY, double maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            dX = maxX - minX;
            dY = maxY - minY;
        }
        internal void setNumRowCols(int numRows, int numCols) { this.numRows = numRows; this.numCols = numCols; }

        internal void plotY(double[] data, double firstX, double incrementX)
        {
            plotY(data, firstX, incrementX, false);
        }
        internal void plotY(double[] data, double firstX, double incrementX, bool isGrid)
        {
            double xScale = this.Width / dX;
            double yScale = this.Height / dY;

            LineSkeleton ls = new LineSkeleton();
            Point[] pts = new Point[data.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i)
            {
                pts[i].X = Convert.ToInt32(xScale * (firstX + incrementX * i - minX));
                pts[i].Y = Convert.ToInt32(yScale * (data[i] - minY));
            }
            ls.lines = pts;
            ls.isGrid = isGrid;

            linesToPlot.Add(ls);
        }

        internal void plotX(double[] data, double firstY, double incrementY)
        {
            plotX(data, firstY, incrementY, false);
        }
        internal void plotX(double[] data, double firstY, double incrementY, bool isGrid)
        {
            double xScale = this.Width / dX;
            double yScale = this.Height / dY;

            LineSkeleton ls = new LineSkeleton();
            Point[] pts = new Point[data.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i)
            {
                pts[i].X = Convert.ToInt32(xScale * (data[i] - minX));
                pts[i].Y = Convert.ToInt32(yScale * (firstY + incrementY * i - minY));
            }
            ls.lines = pts;
            ls.isGrid = isGrid;

            linesToPlot.Add(ls);
        }

        internal void clear()
        {
            //plotGridLines();
        }

        private void plotGridLines()
        {
            double boxHeight = dY / numRows;
            double boxWidth = dX / numCols;

            //Plot gridlines
            for (int r = 1; r <= numRows - 1; ++r)
                plotY(new double[] { boxHeight * r + minY, boxHeight * r + minY },
                    minX, dX, true);
            for (int c = 1; c <= numCols - 1; ++c)
                plotX(new double[] { boxWidth * c + minX, boxWidth * c + minX },
                    minY, dY, true);
        }

        private void SpikeWaveformGraph_VisibleChanged(object sender, EventArgs e)
        {
            //plotGridLines();
        }

        private void GridGraph_Paint(object sender, PaintEventArgs e)
        {
            plotGridLines();
            e.Graphics.Clear(Color.Black);
            while (linesToPlot.Count > 0)
            {
                if (linesToPlot[0].isGrid)
                    e.Graphics.DrawLines(gridPen, linesToPlot[0].lines);
                else
                    e.Graphics.DrawLines(tracePen, linesToPlot[0].lines);
                linesToPlot.RemoveAt(0);
            }
        }

        private void GridGraph_Resize(object sender, EventArgs e)
        {
            //plotGridLines();
        }
    }
}

