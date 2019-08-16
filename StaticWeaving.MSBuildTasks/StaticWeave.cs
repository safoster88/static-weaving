using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StaticWeaving.Common;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace StaticWeaving.MSBuildTasks
{
    public class StaticWeave : Task
    {
        private readonly NotifyPropertyChangedPropertyWeaver _notifyPropertyChangedWeaver;

        [Required]
        public string AssemblyPath { get; set; }

        public StaticWeave()
        {
            _notifyPropertyChangedWeaver = new NotifyPropertyChangedPropertyWeaver(Log);
        }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage("Beginning static weave...");

                if (!File.Exists(AssemblyPath))
                {
                    Log.LogError($"Could not find target assembly at {AssemblyPath}");
                    return false;
                }

                using (var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters { ReadWrite = true }))
                {
                    _notifyPropertyChangedWeaver.WeaveAssembly(assembly);
                    assembly.Write();
                }


                //using (var notifyPropertyChangedAssembly = AssemblyDefinition.ReadAssembly(typeof(INotifyPropertyChanged).Assembly.Location))
                //{
                //    using (var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters { ReadWrite = true }))
                //    {
                //        foreach (var type in assembly.MainModule.Types)
                //        {
                //            var attribute = type.CustomAttributes.FirstOrDefault(x => x.Constructor.DeclaringType.FullName == typeof(NotifyPropertyChangedAttribute).FullName);

                //            if (attribute == null)
                //            {
                //                continue;
                //            }

                //            Log.LogMessage($"Weaving into {type.FullName}...");

                //            var notifyPropertyChangedInterfaceImplementation = type.Interfaces.FirstOrDefault(x => x.InterfaceType.FullName == typeof(INotifyPropertyChanged).FullName);

                //            if (notifyPropertyChangedInterfaceImplementation == null)
                //            {
                //                Log.LogMessage($"{type.FullName} does not implement {typeof(INotifyPropertyChanged).Name}. Weaving implementation...");


                //                var notifyPropertyChangedInterfaceDefinition = notifyPropertyChangedAssembly.MainModule.Types.FirstOrDefault(x => x.FullName == typeof(INotifyPropertyChanged).FullName);

                //                if (notifyPropertyChangedInterfaceDefinition == null)
                //                {
                //                    Log.LogError($"No {typeof(INotifyPropertyChanged)} inteface found defined in {notifyPropertyChangedAssembly.FullName}.");
                //                    continue;
                //                }

                //                type.Interfaces.Add(new InterfaceImplementation(notifyPropertyChangedInterfaceDefinition));
                //            }

                //            foreach (var property in type.Properties)
                //            {
                //                _notifyPropertyChangedWeaver.WeaveProperty(type, property);
                //            }
                //        }

                //        assembly.Write();
                //    }
                //}

                Log.LogMessage("Static weaving complete.");

                return true;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
        }
    }
}
