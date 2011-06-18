using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter
{
    sealed public partial class SpikeWaveformGraph : UserControl
    {
        private float minX = 0F;
        private float minY = 0F;
        private float maxX = 1F;
        private float maxY = 1F;
        private float dX = 1F;
        private float dY = 1F;
        private Color lineColor = Color.Lime;
        private float penWidth = 1;
        private int numRows = 4;
        private int numCols = 4;
        private float xScale;
        private float yScale;

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

        internal void setMinMax(float minX, float maxX, float minY, float maxY)
        {
            lock (this)
            {
                this.minX = minX;
                this.minY = minY;
                this.maxX = maxX;
                this.maxY = maxY;
                dX = maxX - minX;
                dY = maxY - minY;
                xScale = (float)this.Width / dX;
                yScale = -(float)this.Height / dY;
            }
        }
        internal void setNumRowCols(int numRows, int numCols) { this.numRows = numRows; this.numCols = numCols; }

        internal void plotY(float[] data, float firstX, float incrementX)
        {
            plotY(data, firstX, incrementX, tracePen);
        }
        internal void plotY(float[] data, float firstX, float incrementX, Pen p)
        {
            Point[] pts = new Point[data.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i)
            {
                pts[i].X = Convert.ToInt32(xScale * (firstX + incrementX * i - minX));
                pts[i].Y = Convert.ToInt32(yScale * (data[i] - maxY));
            }
            lock (this)
            {
                offScreenG.DrawLines(p, pts);
            }
        }

        internal void plotX(float[] data, float firstY, float incrementY)
        {
            plotX(data, firstY, incrementY, tracePen);
        }
        internal void plotX(float[] data, float firstY, float incrementY, Pen p)
        {
            Point[] pts = new Point[data.GetLength(0)];
            for (int i = 0; i < pts.GetLength(0); ++i)
            {
                pts[i].X = Convert.ToInt32(xScale * (data[i] - minX));
                pts[i].Y = Convert.ToInt32(yScale * (firstY + incrementY * i - maxY));
            }
            lock (this)
            {
                offScreenG.DrawLines(p, pts);
            }
        }

        internal void clear()
        {
            lock (this)
            {
                offScreenG.Clear(Color.Black);
            }
            plotGridLines();
        }

        private void Graph_Resize(object sender, EventArgs e)
        {
            if (this.Width != 0 && this.Height != 0)
            {
                lock (this)
                {
                    offScreenBMP.Dispose();
                    offScreenG.Dispose();

                    offScreenBMP = new Bitmap(this.Width, this.Height);
                    offScreenG = Graphics.FromImage(offScreenBMP);

                    xScale = (float)this.Width / dX;
                    yScale = -(float)this.Height / dY;
                }

                plotGridLines();
            }
        }

        private void plotGridLines()
        {
            float boxHeight = dY / numRows;
            float boxWidth = dX / numCols;

            //Plot gridlines
            for (int r = 1; r <= numRows - 1; ++r)
                plotY(new float[] { boxHeight * r + minY, boxHeight * r + minY },
                    minX, dX, gridPen);
            for (int c = 1; c <= numCols - 1; ++c)
                plotX(new float[] { boxWidth * c + minX, boxWidth * c + minX },
                    minY, dY, gridPen);
        }

        private void SpikeWaveformGraph_VisibleChanged(object sender, EventArgs e)
        {
            plotGridLines();
        }

        private void SpikeWaveformGraph_Paint(object sender, PaintEventArgs e)
        {
            lock (this)
            {
                e.Graphics.DrawImageUnscaled(offScreenBMP, 0, 0);
            }
        }

    }
}
