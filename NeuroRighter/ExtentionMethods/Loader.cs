using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace ExtensionMethods
{

    class Loader : MarshalByRefObject
    {

        public void LoadDll(string filename)
        {

            if (File.Exists(filename))
            {

                //Loads file into Byte array - prevent file locking
                byte[] rawAssembly = NRExtensionMethods.LoadFile(filename);

                //algorithmAssembly = Assembly.LoadFrom(filename);
                Assembly algorithmAssembly = Assembly.Load(rawAssembly);

                foreach (Type TypeAlgorithm in algorithmAssembly.GetExportedTypes())
                {
                    if (TypeAlgorithm.IsClass == true)
                    {
                        Console.WriteLine("Type is {0}", TypeAlgorithm);
                        Object ObjAlgorithmInstance = Activator.CreateInstance(TypeAlgorithm);

                        //Invoke method - start() within the dll
                        MethodInfo methodInfo = ObjAlgorithmInstance.GetType().GetMethod("start");
                        methodInfo.Invoke(ObjAlgorithmInstance, new object[] { });
                    }
                }
            }
        }
    } 
}