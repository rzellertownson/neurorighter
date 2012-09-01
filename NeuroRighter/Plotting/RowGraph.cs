using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace NeuroRighter
{
    sealed internal class RowGraph : GraphicsDeviceControl
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
        private int numSamplesPerPlot;
        private Color gridColor = Color.White;

        BasicEffect effect;
        VertexDeclaration vDec;
        List<VertexPositionColor[]> lines; //Lines to be plotted
        short[] idx; //Index to points in 'lines'

        //Constants for text rendering
        private ContentManager content;
        private SpriteBatch spriteBatch;
        private SpriteFont font;
        private Dictionary<int, Vector2> channelNumberLocations;
        private Dictionary<int, String> channelNumberText;
        private String voltageTimeLabel;
        private double displayGain = 1;
        private double voltageRange; //in volts
        private double timeRange; //in seconds
        private Vector2 voltageTimeLabelCoords;

        internal void setup(int numRows, int numSamplesPerPlot, double timeRange, double voltageRange)
        {
            this.numRows = numRows;
            this.numSamplesPerPlot = numSamplesPerPlot;
            lines = new List<VertexPositionColor[]>(numRows);
            for (int i = 0; i < numRows; ++i) lines.Add(new VertexPositionColor[numSamplesPerPlot]);
            idx = new short[numSamplesPerPlot];

            for (short i = 0; i < idx.Length; ++i) 
                idx[i] = i;

            this.timeRange = timeRange;
            this.voltageRange = voltageRange;
        }

        internal void clear()
        {
            lines.Clear();
            for (int i = 0; i < numRows; ++i) lines.Add(new VertexPositionColor[numSamplesPerPlot]);
        }

        protected override void Initialize()
        {
            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.View = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, this.Width, this.Height, 0, 1, 1000);

            // Graphics device options
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            content = new ContentManager(Services, "Content");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = content.Load<SpriteFont>("NRArial");

            updateVoltageTime();

            this.Resize += new EventHandler(resize);
            this.SizeChanged += new EventHandler(resize);
            this.VisibleChanged += new EventHandler(resize);
        }

        internal void resize(object sender, System.EventArgs e)
        {
            if (!(this.Disposing) && effect != null)
            {
                effect.Projection = Matrix.CreateOrthographicOffCenter(0F, this.Width, this.Height, 0F, 1F, 1000F);
                xScale = (float)this.Width / dX;
                yScale = -(float)this.Height / dY;

                updateChannelNumbers();
                updateVoltageTime();
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

        internal void plotY(float[] data, float firstX, float incrementX, List<Color> colorWave, int plotNumber)
        {
            for (int i = 0; i < data.GetLength(0); ++i)
                lines[plotNumber][i] = new VertexPositionColor(new Vector3(xScale * (firstX + incrementX * i - minX),
                    yScale * ((float)data[i] - maxY), 0), colorWave[plotNumber]);
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.Black);
            //GraphicsDevice.VertexDeclaration = vDec;

            plotChannelNumbers();
            plotVoltageTime();

            //effect.EffectPass
            effect.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < lines.Count; ++i)
                GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineStrip,
                    lines[i], 0, idx.Length, idx, 0, idx.Length - 1);

            //effect.CurrentTechnique.Passes[0].End();
            //effect.End();
        }

        private void plotChannelNumbers()
        {
            spriteBatch.Begin();
            for (int i = 1; i <= channelNumberText.Count; ++i)
            {
                spriteBatch.DrawString(font, channelNumberText[i], channelNumberLocations[i], Color.White);
            }
            spriteBatch.End();
        }

        private void updateChannelNumbers()
        {
            float boxHeight = (float)this.Height / numRows;

            const int MARGIN = 5; //Pixels from vert/horz grid for each label
            //labels will be in upper left of each box

            if (channelNumberLocations == null)
                channelNumberLocations = new Dictionary<int, Vector2>(numRows);
            else channelNumberLocations.Clear();

            if (channelNumberText == null)
                channelNumberText = new Dictionary<int, string>(numRows);
            else channelNumberText.Clear();

            int i = 1;
            for (int r = 0; r < numRows; ++r)
            {
                channelNumberLocations.Add(i, new Vector2(MARGIN, boxHeight * r + MARGIN));
                channelNumberText.Add(i, i.ToString());
                ++i;
            }
        }

        private void plotVoltageTime()
        {
            spriteBatch.Begin();
            lock (voltageTimeLabelLock)
            {
                spriteBatch.DrawString(font, voltageTimeLabel, voltageTimeLabelCoords, Color.White);
            }
            spriteBatch.End();
        }

        private object voltageTimeLabelLock = new object();
        private void updateVoltageTime()
        {
            if (font != null) //Prevents this from being called if object isn't initialized
            {
                const int VERTICAL_MARGIN = 5;
                const int HORIZONTAL_MARGIN = 5;

                double displayVoltage = voltageRange / displayGain;

                lock (voltageTimeLabelLock)
                {
                    if (displayVoltage >= 1)
                        voltageTimeLabel = @"+-" + Math.Ceiling(displayVoltage) + " V, ";
                    else if (displayVoltage * 1000 >= 1)
                        voltageTimeLabel = @"+-" + Math.Ceiling(displayVoltage * 1000) + " mV, ";
                    else if (displayVoltage * 1E6 >= 1)
                        voltageTimeLabel = @"+-" + Math.Ceiling(displayVoltage * 1E6) + " uV, ";

                    voltageTimeLabel += timeRange + " s";

                    Vector2 stringExtent = font.MeasureString(voltageTimeLabel);
                    voltageTimeLabelCoords = new Vector2(this.Width - stringExtent.X - HORIZONTAL_MARGIN, this.Height - stringExtent.Y - VERTICAL_MARGIN);
                }
            }
        }

        internal void setDisplayGain(double gain)
        {
            displayGain = gain;
            updateVoltageTime();
        }


        protected override void Dispose(bool disposing)
        {
            if (content != null)
                content.Unload();
            base.Dispose(disposing);
        }
    }
}
