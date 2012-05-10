using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace ExtensionMethods
{
    /// <summary>
    /// DLL Loader for NRTasks created with the NeuroRighter API
    /// </summary>
    public class ClassLoader
    {
        /// <summary>
        /// Creates a new assembly from NeuroRighter for the excecution of extension DLLs.
        /// </summary>
        /// <param name="filename"></param>
        public void NewAssembly(String filename)
        {

            // Create application domain setup information.
            AppDomainSetup domainSetup = new AppDomainSetup();
            domainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            domainSetup.ApplicationName = "NRTask DLL";

            //Create new domain with domain setup details
            AppDomain myDomain = AppDomain.CreateDomain("LoaderDomain", null, domainSetup);

            //Create new instance of Loader class, where the dll is loaded.
            //This is loaded into the new application domian so it can be
            //unloaded later
            Loader objGenerator = (Loader)myDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "NeuroRighter.ExtensionMethods.Loader");

            //Call LoadDll method to load the assembly into the doamin

            objGenerator.LoadDll(filename);

            //Unlload domain

            AppDomain.Unload(myDomain);

        }//end new assembly

    }//end class

}//end namespace