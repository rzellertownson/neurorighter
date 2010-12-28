using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using pnpCL;
using System.Windows.Forms;

namespace NeuroRighter
{
    class pnpclFinder
    {
        internal static List<pnpClosedLoopAbs> find()
        {
            return getPlugins(LoadPluginAssemblies());
        }
        
        //load all dll's
        private static List<Assembly> LoadPluginAssemblies()
        {
            DirectoryInfo dInfo = new DirectoryInfo(Path.Combine(Application.StartupPath, "Plugins"));
            FileInfo[] files = dInfo.GetFiles("*.dll");
            List<Assembly> plugInAssemblyList = new List<Assembly>();
            if (null != files)
            {
                foreach (FileInfo file in files)
                {
                    plugInAssemblyList.Add(Assembly.LoadFile(file.FullName));
                }
            }
            files = null;
            dInfo = null;
            return plugInAssemblyList;
        }

        //sift through the dlls for the ones we actually want (pnpClosedLoop)
        private static List<pnpClosedLoopAbs> getPlugins(List<Assembly> assemblies)
        {
            List<Type> availableTypes = new List<Type>();
            foreach (Assembly currentAssembly in assemblies)
                availableTypes.AddRange(currentAssembly.GetTypes());

            //go through the assemblies you found and report back with each one that inherits directly from pnpClosedLoopAbs
            List<Type> experimentList = availableTypes.FindAll(delegate(Type t)
            {
                return t.BaseType.Equals(typeof(pnpClosedLoopAbs));
            }
            );
            return experimentList.ConvertAll<pnpClosedLoopAbs>(delegate(Type t)
            {
                return Activator.CreateInstance(t) as pnpClosedLoopAbs; 
            });
        }
    }
}
