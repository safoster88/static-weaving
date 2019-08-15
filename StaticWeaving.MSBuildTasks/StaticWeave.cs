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
        [Required]
        public string AssemblyPath { get; set; }

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
                            var attribute = type.CustomAttributes.FirstOrDefault(x => x.Constructor.DeclaringType.FullName == typeof(NotifyPropertyChangedAttribute).FullName);

                            if (attribute == null)
                            {
                                continue;
                            }

                            Log.LogMessage($"Weaving into {type.FullName}...");

                            var notifyPropertyChangedInterfaceImplementation = type.Interfaces.FirstOrDefault(x => x.InterfaceType.FullName == typeof(INotifyPropertyChanged).FullName);

                            if (notifyPropertyChangedInterfaceImplementation == null)
                            {
                                Log.LogMessage($"{type.FullName} does not implement {typeof(INotifyPropertyChanged).Name}. Weaving implementation...");


                                var notifyPropertyChangedInterfaceDefinition = notifyPropertyChangedAssembly.MainModule.Types.FirstOrDefault(x => x.FullName == typeof(INotifyPropertyChanged).FullName);

                                if (notifyPropertyChangedInterfaceDefinition == null)
                                {
                                    Log.LogError($"No {typeof(INotifyPropertyChanged)} inteface found defined in {notifyPropertyChangedAssembly.FullName}.");
                                    continue;
                                }

                                type.Interfaces.Add(new InterfaceImplementation(notifyPropertyChangedInterfaceDefinition));
                            }

                            foreach (var property in type.Properties)
                            {
                                var ilProcessor = property.SetMethod.Body.GetILProcessor();

                                ilProcessor.InsertBefore(property.SetMethod.Body.Instructions[0], ilProcessor.Create(OpCodes.Nop));

                                var ldarg_0 = ilProcessor.Create(OpCodes.Ldarg_0);
                                ilProcessor.InsertBefore(property.SetMethod.Body.Instructions[property.SetMethod.Body.Instructions.Count - 1], ldarg_0);

                                var ldstr = ilProcessor.Create(OpCodes.Ldstr, property.Name);
                                ilProcessor.InsertAfter(ldarg_0, ldstr);

                                var notifyPropertyChanged = type.Methods.FirstOrDefault(x => x.Name == "NotifyPropertyChanged");
                                if (notifyPropertyChanged == null)
                                {
                                    Log.LogError($"No NotifyPropertyChanged function is defined on {type.FullName}.");
                                    continue;
                                }
                                var call = ilProcessor.Create(OpCodes.Call, notifyPropertyChanged);
                                ilProcessor.InsertAfter(ldstr, call);

                                ilProcessor.InsertAfter(call, ilProcessor.Create(OpCodes.Nop));

                                Log.LogMessage($"Weaved into property '{property.FullName}'");
                            }
                        }

                        assembly.Write();
                    }
                }

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
