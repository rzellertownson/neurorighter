using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NeuroRighter
{
    ///<author>John Rolston</author>
    sealed internal class GridGraph : GraphicsDeviceControl
    {
        private float minX = 0F;
        private float maxX = 1F;
        private float minY = 0F;
        private float maxY = 1F;
        private float dX = 1F;
        private float dY = 1F;
        private float xScale;
        private float yScale;

        private int numRows;
        private int numCols;
        private int numSamplesPerPlot;
        private bool _isSpikeWaveformPlot;
        internal bool isSpikeWaveformPlot
        {
            get { return _isSpikeWaveformPlot; }
        }
    

        private Color gridColor = Color.White;

        BasicEffect effect;
        VertexDeclaration vDec;
        List<VertexPositionColor[]> lines; //Lines to be plotted
        List<VertexPositionColor[]> gridLines; //Grid lines
        int[] idx; //Index to points in 'lines'
        short[] gridIdx = { 0, 1 }; //Index to points in gridLines

        private const int NUM_WAVEFORMS_PER_PLOT = 10;

        internal void setup(int numRows, int numColumns, int numSamplesPerPlot, bool isSpikeWaveformPlot)
        {
            this.numRows = numRows; this.numCols = numColumns; this._isSpikeWaveformPlot = isSpikeWaveformPlot;
            this.numSamplesPerPlot = numSamplesPerPlot;
            if (!isSpikeWaveformPlot)
            {
                lines = new List<VertexPositionColor[]>(numRows);
                for (int i = 0; i < numRows; ++i) lines.Add(new VertexPositionColor[numSamplesPerPlot * numColumns]);
                idx = new int[numSamplesPerPlot * numCols];
            }
            else 
            {
                lines = new List<VertexPositionColor[]>(numCols * numRows * NUM_WAVEFORMS_PER_PLOT);
                for (int i = 0; i < numCols * numRows * NUM_WAVEFORMS_PER_PLOT; ++i) 
                    lines.Add(new VertexPositionColor[numSamplesPerPlot]);
                idx = new int[numSamplesPerPlot];
            }
            gridLines = new List<VertexPositionColor[]>(numRows + numCols - 2);

            for (int i = 0; i < numRows + numCols - 2; ++i) gridLines.Add(new VertexPositionColor[2]);
            for (int i = 0; i < idx.Length; ++i) idx[i] = i;
        }

        internal void clear()
        {
            if (!_isSpikeWaveformPlot)
            {
                lines.Clear();
                for (int i = 0; i < numRows; ++i) lines.Add(new VertexPositionColor[numSamplesPerPlot * numCols]);
            }
            else
            {
                lines.Clear();
                for (int i = 0; i < numCols * numRows * NUM_WAVEFORMS_PER_PLOT; ++i)
                    lines.Add(new VertexPositionColor[numSamplesPerPlot]);
            }
        }

        protected override void Initialize()
        {
            effect = new BasicEffect(GraphicsDevice, null);
            effect.VertexColorEnabled = true;
            effect.View = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, this.Width, this.Height, 0, 1, 1000);

            GraphicsDevice.RenderState.CullMode = CullMode.None;
            
            vDec = new VertexDeclaration(GraphicsDevice, VertexPositionColor.VertexElements);
        }

        internal void resize(object sender, System.EventArgs e)
        {
            if (!(this.Disposing) && effect != null)
            {
                effect.Projection = Matrix.CreateOrthographicOffCenter(0F, this.Width, this.Height, 0F, 1F, 1000F);
                xScale = (float)this.Width / dX;
                yScale = -(float)this.Height / dY;

                plotGridLines();
            }
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

        internal void plotY(float[] data, float firstX, float incrementX, Color c, int plotNumber)
        {
            for (int i = 0; i < data.GetLength(0); ++i)
                lines[plotNumber][i] = new VertexPositionColor(new Vector3(xScale * (firstX + incrementX * i - minX),
                    yScale * ((float)data[i] - maxY), 0), c);
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.Black);
            GraphicsDevice.VertexDeclaration = vDec;
            effect.Begin();
            effect.CurrentTechnique.Passes[0].Begin();

            for (int i = 0; i < gridLines.Count; ++i)
                GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineStrip,
                    gridLines[i], 0, 2, gridIdx, 0, 1);
            for (int i = 0; i < lines.Count; ++i)
                GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineStrip,
                    lines[i], 0, idx.Length, idx, 0, idx.Length - 1);

            effect.CurrentTechnique.Passes[0].End();
            effect.End();
        }

        private void plotGridLines()
        {
            float boxHeight = (float)this.Height / numRows;
            float boxWidth = (float)this.Width / numCols;

            //Draw horz. lines
            for (short i = 0; i < numRows - 1; ++i)
            {
                gridLines[i][0] = new VertexPositionColor(new Vector3(0F, boxHeight * (i + 1), 0F),
                    gridColor);
                gridLines[i][1] = new VertexPositionColor(new Vector3(this.Width, boxHeight * (i + 1), 0F),
                    gridColor);
            }
            //Draw vert. lines
            for (short i = 0; i < numCols - 1; ++i)
            {
                gridLines[i + numRows - 1][0] = new VertexPositionColor(new Vector3(boxWidth * (i + 1),
                    0F, 0F), gridColor);
                gridLines[i + numCols - 1][1] = new VertexPositionColor(new Vector3(boxWidth * (i + 1),
                    this.Height, 0F), gridColor);
            }
        }
    }
}
