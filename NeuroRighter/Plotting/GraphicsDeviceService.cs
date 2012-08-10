#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceService.cs
//
// Microsoft XNA Community Game Platform
// 
// Distributed under the Microsoft Permissive License (Ms-PL).
//
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
#endregion

// The IGraphicsDeviceService interface requires a DeviceCreated event, but we
// always just create the device inside our constructor, so we have no place to
// raise that event. The C# compiler warns us that the event is never used, but
// we don't care so we just disable this warning.
#pragma warning disable 67

namespace NeuroRighter
{
    /// <summary>
    /// Helper class responsible for creating and managing the GraphicsDevice.
    /// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
    /// so even though there can be many controls, there will only ever be a single
    /// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
    /// interface, which provides notification events for when the device is reset
    /// or disposed.
    /// </summary>
    class GraphicsDeviceService : IGraphicsDeviceService
    {
        #region Fields

        // Singleton device service instance.
        static GraphicsDeviceService singletonInstance;

        // Presentaiton parameters
        public PresentationParameters Parameters { get; private set; }

        // IGraphicsDeviceService events.
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        // The actual graphics device
        GraphicsDevice graphicsDevice;

        // Keep track of how many controls are sharing the singletonInstance.
        static int referenceCount;

        #endregion


        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        GraphicsDeviceService(IntPtr windowHandle, int width, int height)
        {
            //parameters = new PresentationParameters();

            //parameters.BackBufferWidth = Math.Max(width, 1);
            //parameters.BackBufferHeight = Math.Max(height, 1);
            //parameters.BackBufferFormat = SurfaceFormat.Color;

            //parameters.EnableAutoDepthStencil = true;
            //parameters.AutoDepthStencilFormat = DepthFormat.Depth24;

            this.Parameters = new PresentationParameters()
            {
                BackBufferWidth = Math.Max(width, 1),
                BackBufferHeight = Math.Max(height, 1),
                BackBufferFormat = SurfaceFormat.Color,
                DepthStencilFormat = DepthFormat.Depth24,
                DeviceWindowHandle = windowHandle,
                PresentationInterval = PresentInterval.Immediate,
                IsFullScreen = false,
                MultiSampleCount = 4
            };

            //graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter,
            //                                    DeviceType.Hardware,
            //                                    windowHandle,
            //                                    parameters);

            this.graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, this.Parameters);

            if (this.DeviceCreated != null)
            {
                this.DeviceCreated(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static GraphicsDeviceService AddRef(IntPtr windowHandle,
                                                   int width, int height)
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                singletonInstance = new GraphicsDeviceService(windowHandle,
                                                              width, height);
            }

            return singletonInstance;
        }


        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                // If this is the last control to finish using the
                // device, we should dispose the singleton instance.
                if (disposing)
                {
                    if (DeviceDisposing != null)
                        DeviceDisposing(this, EventArgs.Empty);

                    graphicsDevice.Dispose();
                }

                graphicsDevice = null;
            }
        }

        
        /// <summary>
        /// Resets the graphics device to whichever is bigger out of the specified
        /// resolution or its current size. This behavior means the device will
        /// demand-grow to the largest of all its GraphicsDeviceControl clients.
        /// </summary>
        public void ResetDevice(int width, int height)
        {
            if (DeviceResetting != null)
                DeviceResetting(this, EventArgs.Empty);

            Parameters.BackBufferWidth = Math.Max(Parameters.BackBufferWidth, width);
            Parameters.BackBufferHeight = Math.Max(Parameters.BackBufferHeight, height);

            graphicsDevice.Reset(Parameters);

            if (DeviceReset != null)
                DeviceReset(this, EventArgs.Empty);
        }

        
        /// <summary>
        /// Gets the current graphics device.
        /// </summary>
        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDevice; }
        }
    }
}
