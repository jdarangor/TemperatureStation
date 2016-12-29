﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace TemperatureStation.Web.Extensions
{
    public class CalculatorProvider : ICalculatorProvider
    {
        private static IList<Type> CalculatorTypes;
        private static object _locker = new object();

        private IServiceProvider _serviceProvider;

        public CalculatorProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            LoadCalculatorTypes();
        }

        public IDictionary<string,ICalculator> GetCalculators()
        {
            var result = new Dictionary<string, ICalculator>();

            foreach(var type in CalculatorTypes)
            {
                //var calc = (ICalculator)Activator.CreateInstance(type);
                var calc = (ICalculator)_serviceProvider.GetService(type);
                var name = type.GetTypeInfo().GetCustomAttribute<CalculatorAttribute>().Name;

                result.Add(name, calc);
            }

            return result;
        }

        public IEnumerable<string> GetNames()
        {
            return CalculatorTypes
                    .Select(t => t.GetTypeInfo().GetCustomAttribute<CalculatorAttribute>()?.Name)
                    .Where(t => !string.IsNullOrWhiteSpace(t));            
        }

        private static void LoadCalculatorTypes()
        {
            if(CalculatorTypes != null)
            {
                return;
            }

            lock (_locker)
            {
                if(CalculatorTypes != null)
                {
                    return;
                }

                var calcs = from a in GetReferencingAssemblies()
                            from t in a.GetTypes()
                            where t.GetTypeInfo().GetCustomAttribute<CalculatorAttribute>() != null
                            select t;

                CalculatorTypes = calcs.ToList();
            }
        }

        private static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependencies)
            {
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
                catch(FileNotFoundException)
                { }
                catch(Exception)
                {
                    throw;
                }
            }
            return assemblies;
        }
    }
}
