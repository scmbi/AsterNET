using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace AsterNET.FastAGI.MappingStrategies
{

    public class ScriptMapping
    {
        public ScriptMapping(string scriptName, Type scriptClass, Assembly scriptAssembly, Assembly? preLoadedAssembly = null)
        {
            ScriptName = scriptName;
            ScriptClass = scriptClass;
            ScriptAssembly = scriptAssembly;
            PreLoadedAssembly = preLoadedAssembly;
        }

        /// <summary>
        /// The name of the script as called by FastAGI
        /// </summary>
        public string ScriptName { get; set; }

        public Type ScriptClass { get; set; }

        /// <summary>
        /// The class containing the AGIScript to be run
        /// </summary>
        public string ScriptClassName => ScriptClass.ToString();

        public Assembly ScriptAssembly { get; set; }

        /// <summary>
        /// The name of the assembly to load, that contains the ScriptClass. Optional, if not specified, the class will be loaded from the current assembly
        /// </summary>
        public string ScriptAssemblyLocation => ScriptAssembly.Location;

        [XmlIgnoreAttribute]
        public Assembly? PreLoadedAssembly { get; set; }

        public static List<ScriptMapping> LoadMappings(string pathToXml)
        {
            // Load ScriptMappings XML File
            var xs = new XmlSerializer(typeof(List<ScriptMapping>));            
            using (FileStream fs = File.OpenRead(pathToXml))
            {
                var deserialized = xs.Deserialize(fs);
                if(deserialized != null && deserialized is List<ScriptMapping> list)
                    return list;
            }            
            return new List<ScriptMapping>();
        }

        public static void SaveMappings(string pathToXml, List<ScriptMapping> resources)
        {
            // Save ScriptMappings XML File
            XmlSerializer xs = new XmlSerializer(typeof(List<ScriptMapping>));
            using (var fs = File.Open(pathToXml, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                lock (resources)
                {
                    xs.Serialize(fs, resources);
                }
            }
        }
    }
}
