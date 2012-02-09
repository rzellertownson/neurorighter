using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;



namespace ExtensionMethods
{

    public class ClassLoader
    {

        public void NewAssembly(String filename)
        {

            // Create application domain setup information.
            AppDomainSetup domainSetup = new AppDomainSetup();
            domainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            domainSetup.ApplicationName = "NeuroRighter Closed Loop DLL";

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