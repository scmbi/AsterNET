using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace AsterNET.FastAGI.MappingStrategies
{

    internal class MappingAssembly
    {
        public MappingAssembly(Type scriptClass, Assembly? loadedAssembly = null)
        {
            ScriptClass = scriptClass;
            LoadedAssembly = loadedAssembly;
        }

        public Type ScriptClass { get; set; }
        public string ClassName => ScriptClass.ToString();

        public Assembly? LoadedAssembly { get; set; }

        public AGIScript CreateInstance(IServiceProvider? serviceProvider = null)
        {
            if (serviceProvider != null)
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetService(ScriptClass);
                if (service != null && service is AGIScript script) return script;
            }

            object? rtn;
            if (LoadedAssembly != null)
            {
                rtn = LoadedAssembly.CreateInstance(ClassName);
            }
            else
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null) throw new Exception("null assembly on creating instance");

                rtn = assembly.CreateInstance(ClassName);
            }
            
            if(rtn == null)
                throw new Exception("null object after create instance");

            if (rtn is AGIScript assembled)
                return assembled;
            else 
                throw new Exception("object is not an AGIScript");
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
        private readonly IServiceProvider? provider;
        private List<ScriptMapping> mappings;
        private Dictionary<string, MappingAssembly>? mapAssemblies;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resources"></param>
        public GeneralMappingStrategy(List<ScriptMapping> resources)
        {
            this.mappings = resources;
        }

        public GeneralMappingStrategy(IServiceProvider provider, List<ScriptMapping> resources)
        {
            this.provider = provider;
            this.mappings = resources;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlFilePath"></param>
        public GeneralMappingStrategy(string xmlFilePath)
        {
            this.mappings = ScriptMapping.LoadMappings(xmlFilePath);
        }

        public AGIScript? DetermineScript(AGIRequest request)
        {
            AGIScript? script = null;
            if (mapAssemblies != null && !string.IsNullOrWhiteSpace(request.Script))
            {
                lock (mapAssemblies)
                {
                    if (mapAssemblies.ContainsKey(request.Script))
                        script = mapAssemblies[request.Script].CreateInstance(provider);
                }
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

                if (mappings == null || !mappings.Any())
                    throw new AGIException("No mappings were added, before Load method called.");

                foreach (var de in mappings)
                {
                    // secure check of null mappings
                    if (de == null) continue;

                    MappingAssembly ma;
                    if (de.ScriptClass != null)
                    {
                        ma = new MappingAssembly(de.ScriptClass, de.ScriptClass.Assembly);
                        mapAssemblies.Add(de.ScriptName, ma);
                        continue;
                    }

                    if (mapAssemblies.ContainsKey(de.ScriptName))
                        throw new AGIException(string.Format("Duplicate mapping name '{0}'", de.ScriptName));

                    if (!string.IsNullOrWhiteSpace(de.ScriptAssemblyLocation))
                    {
                        try
                        {
                            var assembly = Assembly.LoadFile(de.ScriptAssemblyLocation);
                            ma = new MappingAssembly(de.ScriptClass, assembly);
                        }
                        catch (FileNotFoundException fnfex)
                        {
                            throw new AGIException(string.Format("Unable to load AGI Script {0}, file not found.", Path.Combine(Environment.CurrentDirectory, de.ScriptAssemblyLocation)), fnfex);
                        }
                    }
                    else
                    {
                        ma = new MappingAssembly(de.ScriptClass, de.PreLoadedAssembly);
                    }

                    mapAssemblies.Add(de.ScriptName, ma);                    
                }                
            }
        }

    }
}
