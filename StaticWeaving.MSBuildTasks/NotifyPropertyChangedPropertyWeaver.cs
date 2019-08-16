using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StaticWeaving.Common;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StaticWeaving.MSBuildTasks
{
    public class NotifyPropertyChangedPropertyWeaver
    {
        private readonly TaskLoggingHelper _log;

        public NotifyPropertyChangedPropertyWeaver(TaskLoggingHelper log)
        {
            _log = log;
        }

        public void WeaveAssembly(AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
            {
                WeaveType(type);
            }
        }

        public bool WeaveType(TypeDefinition type)
        {
            if (!type.CustomAttributes.Any(x => x.Constructor.DeclaringType.FullName == typeof(NotifyPropertyChangedAttribute).FullName))
            {
                // Do not weave if the NotifyPropertyChangedAttribute is missing.
                return false;
            }

            _log.LogMessage($"Weaving into {type.FullName}...");

            foreach (var property in type.Properties)
            {
                WeaveProperty(type, property);
            }

            return true;
        }

        public bool WeaveProperty(TypeDefinition type, PropertyDefinition property)
        {
            if (!property.SetMethod.CustomAttributes.Any(x => x.Constructor.DeclaringType.FullName == typeof(CompilerGeneratedAttribute).FullName))
            {
                _log.LogMessage($"Detected non-compiler generated property '{property.FullName}'. Ignoring during weaving process.");
                return false;
            }

            var ilProcessor = property.SetMethod.Body.GetILProcessor();

            ilProcessor.InsertBefore(property.SetMethod.Body.Instructions[0], ilProcessor.Create(OpCodes.Nop));

            var ldarg_0 = ilProcessor.Create(OpCodes.Ldarg_0);
            ilProcessor.InsertBefore(property.SetMethod.Body.Instructions[property.SetMethod.Body.Instructions.Count - 1], ldarg_0);

            var ldstr = ilProcessor.Create(OpCodes.Ldstr, property.Name);
            ilProcessor.InsertAfter(ldarg_0, ldstr);

            var notifyPropertyChanged = type.Methods.FirstOrDefault(x => x.Name == "NotifyPropertyChanged");
            if (notifyPropertyChanged == null)
            {
                _log.LogError($"No NotifyPropertyChanged function is defined on {type.FullName}.");
                return false;
            }
            var call = ilProcessor.Create(OpCodes.Call, notifyPropertyChanged);
            ilProcessor.InsertAfter(ldstr, call);

            ilProcessor.InsertAfter(call, ilProcessor.Create(OpCodes.Nop));

            _log.LogMessage($"Weaved into property '{property.FullName}'");
            return true;
        }
    }
}
