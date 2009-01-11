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
    public partial class SpikeWaveformGraph : UserControl
    {
        private double minX = 0.0;
        private double minY = 0.0;
        private double maxX = 1.0;
        private double maxY = 1.0;
        private double dX = 1.0;
        private double dY = 1.0;
        private Color lineColor = Color.Lime;
        private float penWidth = 1;
        private int numRows = 4;
        private int numCols = 4;

        private Graphics offScreenG;
        private Bitmap offScreenBMP;
        private Pen tracePen;
        private Pen gridPen;

        internal SpikeWaveformGraph()
        {
            InitializeComponent();

            offScreenBMP = new Bitmap(this.Width, this.Height);
            offScreenG = Graphics.FromImage(offScreenBMP);
            tracePen = new Pen(lineColor);
            tracePen.Width = penWidth;
            gridPen = new Pen(Color.White);
            gridPen.Width = 1;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
            SetStyle(ControlStyles.UserPaint, true);
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
            plotY(data, firstX, incrementX, tracePen);
        }
        internal void plotY(double[] data, double firstX, double incrementX, Pen p)
        {
            double xScale = this.Width / dX;
            double yScale = this.Height / dY;

            Point[] pts = new Point[data.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i)
            {
                pts[i].X = Convert.ToInt32(xScale * (firstX + incrementX * i - minX));
                pts[i].Y = Convert.ToInt32(yScale * (data[i] - minY));
            }

            offScreenG.DrawLines(p, pts);
        }

        internal void plotX(double[] data, double firstY, double incrementY)
        {
            plotX(data, firstY, incrementY, tracePen);
        }
        internal void plotX(double[] data, double firstY, double incrementY, Pen p)
        {
            double xScale = this.Width / dX;
            double yScale = this.Height / dY;

            Point[] pts = new Point[data.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i)
            {
                pts[i].X = Convert.ToInt32(xScale * (data[i] - minX));
                pts[i].Y = Convert.ToInt32(yScale * (firstY + incrementY * i - minY));
            }

            offScreenG.DrawLines(p, pts);
        }

        internal void clear()
        {
            offScreenG.Clear(Color.Black);
            plotGridLines();
        }

        private void Graph_Resize(object sender, EventArgs e)
        {
            offScreenBMP.Dispose();
            offScreenG.Dispose();

            offScreenBMP = new Bitmap(this.Width, this.Height);
            offScreenG = Graphics.FromImage(offScreenBMP);
            
            plotGridLines();
        }

        private void plotGridLines()
        {
            double boxHeight = dY / numRows;
            double boxWidth = dX / numCols;

            //Plot gridlines
            for (int r = 1; r <= numRows - 1; ++r)
                plotY(new double[] { boxHeight * r + minY, boxHeight * r + minY },
                    minX, dX, gridPen);
            for (int c = 1; c <= numCols - 1; ++c)
                plotX(new double[] { boxWidth * c + minX, boxWidth * c + minX },
                    minY, dY, gridPen);
        }

        private void SpikeWaveformGraph_VisibleChanged(object sender, EventArgs e)
        {
            plotGridLines();
        }

        private void SpikeWaveformGraph_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(offScreenBMP, 0, 0);
        }

    }
}
