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

                using (var notifyPropertyChangedAssembly = AssemblyDefinition.ReadAssembly(typeof(INotifyPropertyChanged).Assembly.Location))
                {
                    using (var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters { ReadWrite = true }))
                    {
                        foreach (var type in assembly.MainModule.Types)
                        {
                            if (type.CustomAttributes.Any(x => x.Constructor.DeclaringType.FullName == typeof(NotifyPropertyChangedAttribute).FullName))
                            {
                                if (!type.Interfaces.Any(x => x.InterfaceType.FullName == typeof(INotifyPropertyChanged).FullName))
                                {
                                    Log.LogMessage($"{type.FullName} has {nameof(NotifyPropertyChangedAttribute)} but no {nameof(INotifyPropertyChanged)} implementation. Weaving...");

                                    var notifyPropertyChangedType = notifyPropertyChangedAssembly.MainModule.Types.FirstOrDefault(x => x.FullName == typeof(INotifyPropertyChanged).FullName);

                                    Log.LogMessage(notifyPropertyChangedType.FullName);

                                    type.Interfaces.Add(new InterfaceImplementation(notifyPropertyChangedType));
                                }
                            }
                        }

                        assembly.Write();
                    }
                }

                //using (var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters { ReadWrite = true }))
                //{
                //    _notifyPropertyChangedWeaver.WeaveAssembly(assembly);
                //    assembly.Write();
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
