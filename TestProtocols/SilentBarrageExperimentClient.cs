using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NeuroRighter.Output
{
    class SilentBarrageExperimentClient
    {
        IPEndPoint ipep;
        Socket server;
        TcpClient s2;
        double[] motorPoles;
        double[] motorHeight;
        double[] sensorData;
        private bool connected = false;
        const int expectedInputs = 32;
        internal SilentBarrageExperimentClient(string IPaddress, int port)
        {

            IPAddress[] addresslist = Dns.GetHostAddresses(IPaddress);

             ipep = new IPEndPoint(addresslist[0], port);
             //server = new Socket(AddressFamily.InterNetwork,
             //         SocketType.Stream, ProtocolType.Tcp);
             s2 = new TcpClient();
             sensorData = new double[expectedInputs];
             motorPoles = new double[expectedInputs];
             motorHeight = new double[expectedInputs];
             for (int i = 0; i < expectedInputs; i++)
             {
                 motorPoles[i] = i+1;
                motorHeight[i] = 50;

             }

        }

        internal void connect()
        {
            try
            {
                s2.Connect(ipep);
                toServer("C");
                
               // server.Connect(ipep);
                //server.Send();
                connected = true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Unable to connect to server.");
                Console.WriteLine(e.ToString());
                connected = false;
                return;
            }
        }

        //check for a request for motor data, respond to it if it has been made.
        //check for updates on sensor information
        internal void synch()
        {
            try
            {
                if (s2.GetStream().DataAvailable)
                {
                    byte[] data = new byte[1024];

                    int recv = s2.GetStream().Read(data, 0, data.Length);
                    //server.ReceiveAsync(data);
                    string stringData = Encoding.ASCII.GetString(data, 0, recv);
                    Console.WriteLine("incoming: " + stringData);
                    parseInput(stringData);
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine("Unable to synch with server.");
                Console.WriteLine(e.ToString());
                connected = false;
                return;
            }
        }
        internal void updateMotor(double[] pole, double[] height)
        {
            lock (motorPoles)
            {
                motorPoles = pole;
                motorHeight = height;
            }
        }
        private void toServer(string toSend)
        {
            byte[] outt = Encoding.ASCII.GetBytes(toSend);
            s2.GetStream().Write(outt,0,outt.Length);
        }
        private void parseInput(string input)
        {
            while (input.Length > 0)
            {
                if (input.Substring(0, 4).Equals("moto"))
                {
                    //request for data
                    toServer(motorOut());
                    //server.Send(Encoding.ASCII.GetBytes(motorOut()));
                    Console.WriteLine(motorOut());

                    input = input.Substring(4);

                }
                else if (input.Substring(0, 4).Equals("sens"))
                {
                    input = input.Substring(4);
                    char[] delimiterChars = { ' ' };

                    string[] words = input.Split(delimiterChars);
                    
                    for (int i = 0; i < expectedInputs; i++)
                    {
                        sensorData[i] = Convert.ToDouble(words[i]);
                    }
                    input = "";
                    for (int i = expectedInputs; i < words.Length; i++)
                    {
                        input += " " + words[i];
                    }

                    

                }
                else
                {
                    Console.WriteLine("unknown command from server: " + input);
                    break;
                }
            }
        }

        private string motorOut()
        {
            lock (motorPoles)
            {
                string output = "m " + motorPoles.Length.ToString();
                for (int i = 0; i < motorPoles.Length; i++)
                {
                    output += " " + motorPoles[i].ToString() + " " + motorHeight[i].ToString();
                }
                return output;
            }
        }

        internal void close()
        {
            toServer("Q");
            server.Shutdown(SocketShutdown.Both);
            server.Close();
            connected = false;
        }

        private char[] convertToChar(double[] input)
        {
            string output = "";
            for (int i = 0; i < input.Length; i++)
            {
                output += input[i].ToString()+" ";
            }
            return output.ToCharArray();
        }
        public bool isConnected()
        {
            return connected;
        }
    }
    
}
