using System;
using System.Collections;
using System.Resources;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace AsterNET.FastAGI.MappingStrategies
{

    internal class MappingAssembly
    {
        public Type ScriptClass { get; set; }
        public string ClassName => ScriptClass?.ToString();
        public Assembly LoadedAssembly { get; set; }

        public AGIScript CreateInstance(IServiceProvider serviceProvider = default)
        {
            if (serviceProvider != null)
            {
                using var scope = serviceProvider.CreateScope();
                var script = scope.ServiceProvider.GetService(ScriptClass);
                if (script != null) return script as AGIScript;
            }

            AGIScript rtn = null;
            try
            {
                if (LoadedAssembly != null)
                    rtn = (AGIScript)LoadedAssembly.CreateInstance(ClassName);
                else
                    rtn = (AGIScript)Assembly.GetEntryAssembly().CreateInstance(ClassName);
            }
            catch { }
            return rtn;
        }
    }

    public class ScriptMapping
    {
        /// <summary>
        /// The name of the script as called by FastAGI
        /// </summary>
        public string ScriptName { get; set; }

        public Type ScriptClass { get; set; }

        /// <summary>
        /// The class containing the AGIScript to be run
        /// </summary>
        public string ScriptClassName => ScriptClass?.ToString();

        public Assembly ScriptAssembly { get; set; }

        /// <summary>
        /// The name of the assembly to load, that contains the ScriptClass. Optional, if not specified, the class will be loaded from the current assembly
        /// </summary>
        public string ScriptAssemblyLocation => ScriptAssembly?.Location;

        [XmlIgnoreAttribute]
        public Assembly PreLoadedAssembly { get; set; }

        public static List<ScriptMapping> LoadMappings(string pathToXml)
        {
            // Load ScriptMappings XML File
            XmlSerializer xs = new XmlSerializer(typeof(List<ScriptMapping>));
            try
            {
                using (FileStream fs = File.OpenRead(pathToXml))
                {
                    return (List<ScriptMapping>)xs.Deserialize(fs);
                }
            }
            catch
            {
                return new List<ScriptMapping>();
            }
        }

        public static void SaveMappings(string pathToXml, List<ScriptMapping> resources)
        {
            // Save ScriptMappings XML File
            XmlSerializer xs = new XmlSerializer(typeof(List<ScriptMapping>));
            using (FileStream fs = File.Open(pathToXml, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                lock (resources)
                {
                    xs.Serialize(fs, resources);
                }
            }
        }
    }

    /// <summary>
    /// A MappingStrategy that is configured via a an XML file
    /// or used by passing in a single or list of SciptMapping
    /// This is useful as a general mapping strategy, rather than 
    /// using the default Resource Reference method.
    /// </summary>
    public class GeneralMappingStrategy : IMappingStrategy
    {
#if LOGGER
        private Logger logger = Logger.Instance();
#endif
        private readonly IServiceProvider provider;
        private List<ScriptMapping> mappings;
        private Dictionary<string, MappingAssembly> mapAssemblies;

        /// <summary>
        /// 
        /// </summary>
        public GeneralMappingStrategy()
        {
            this.mappings = null;
            this.mapAssemblies = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resources"></param>
        public GeneralMappingStrategy(List<ScriptMapping> resources)
        {
            this.mappings = resources;
            this.mapAssemblies = null;
        }

        public GeneralMappingStrategy(IServiceProvider provider, List<ScriptMapping> resources)
        {
            this.provider = provider;
            this.mappings = resources;
            this.mapAssemblies = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlFilePath"></param>
        public GeneralMappingStrategy(string xmlFilePath)
        {
            this.mappings = ScriptMapping.LoadMappings(xmlFilePath);
            this.mapAssemblies = null;
        }

        public AGIScript DetermineScript(AGIRequest request)
        {
            AGIScript script = null;
            if (mapAssemblies != null)
                lock (mapAssemblies)
                {
                    if (mapAssemblies.ContainsKey(request.Script))
                        script = mapAssemblies[request.Script].CreateInstance(provider);
                }
            return script;
        }

        public void Load()
        {
            if (mapAssemblies == null)
                mapAssemblies = new Dictionary<string, MappingAssembly>();
            lock (mapAssemblies)
            {
                mapAssemblies.Clear();

                if (this.mappings == null || this.mappings.Count == 0)
                    throw new AGIException("No mappings were added, before Load method called.");

                foreach (var de in this.mappings)
                {
                    MappingAssembly ma;
                    if(de.ScriptClass != null)
                    {
                        ma = new MappingAssembly()
                        {
                            ScriptClass = de.ScriptClass,
                            LoadedAssembly = de.ScriptClass.Assembly
                        };
                        mapAssemblies.Add(de.ScriptName, ma);
                        continue;
                    }


                    if (mapAssemblies.ContainsKey(de.ScriptName))
                        throw new AGIException(String.Format("Duplicate mapping name '{0}'", de.ScriptName));
                    if (!string.IsNullOrEmpty(de.ScriptAssemblyLocation))
                    {
                        try
                        {
                            ma = new MappingAssembly()
                            {
                                ScriptClass = de.ScriptClass,
                                LoadedAssembly = Assembly.LoadFile(de.ScriptAssemblyLocation)
                            };
                        }
                        catch (FileNotFoundException fnfex)
                        {
                            throw new AGIException(string.Format("Unable to load AGI Script {0}, file not found.", Path.Combine(Environment.CurrentDirectory, de.ScriptAssemblyLocation)), fnfex);
                        }
                    }
                    else
                    {
                        ma = new MappingAssembly()
                        {
                            ScriptClass = de.ScriptClass
                        };
                        if (de.PreLoadedAssembly != null)
                            ma.LoadedAssembly = de.PreLoadedAssembly;
                    }

                    mapAssemblies.Add(de.ScriptName, ma);
                }
                
            }
        }

    }
}
