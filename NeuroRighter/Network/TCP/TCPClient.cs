using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace NeuroRighter.Network
{
    /// <summary>
    /// Simple TCP Client for sending simple messages accorss LAN
    /// </summary>
    public class TCPClient
    {
        private Int32 port;
        private string server;
        TcpClient client;
        NetworkStream stream;

        /// <summary>
        /// A TCP Client Object
        /// </summary>
        /// <param name="server"> Server IP address </param>
        /// <param name="port"> Port Number </param>
        public TCPClient(string server, string port)
        {
            this.server = server;
            this.port = Convert.ToInt32(port);
        }

        /// <summary>
        /// Connect to the specified port on the specified host
        /// </summary>
        public void Connect()
        {
            try
            {
                // Attempt a connection to the RACS server
                Console.WriteLine("Attempting a connection to the specified server...");
                client = new TcpClient(server, port);

                // Setup data stream for reading and writing to server
                Console.WriteLine("Configuring data socket...");
                stream = client.GetStream();

                // Data socket has been configured. Ready send data.
                Console.WriteLine("Data socket has been configured.");

            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }


        }

        /// <summary>
        /// Send a single string to the server specified in constructor.
        /// </summary>
        /// <param name="message"> String to send. </param>
        public void SendString(string message)
        {
            try
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\n");
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        /// <summary>
        /// Send a single string to the server specified in constructor.
        /// </summary>
        /// <param name="message">String to send.</param>
        /// <param name="verbose">Echo the sent string to command line after it has been sent.</param>
        public void SendString(string message, bool verbose)
        {
            try
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\n");
                stream.Write(data, 0, data.Length);
                stream.Flush();
                if (verbose)
                    Console.WriteLine("Sent: {0}", message);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        /// <summary>
        /// Close the TCP Client
        /// </summary>
        public void Close()
        {
            stream.Close();
            client.Close();
        }

        /// <summary>
        /// Read a message from the TCP Server.
        /// </summary>
        /// <returns> The message.</returns>
        public string ReadString()
        {
            // Buffer to store the response bytes.
            byte[] data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TCPServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            
            // Get the string
            return System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        }

        /// <summary>
        /// Return the current port number
        /// </summary>
        public Int32 PortNumber
        {
            get
            {
                return port;
            }
        }

        /// <summary>
        /// Return the current server address
        /// </summary>
        public string Server
        {
            get
            {
                return server;
            }
        }
    }
}
