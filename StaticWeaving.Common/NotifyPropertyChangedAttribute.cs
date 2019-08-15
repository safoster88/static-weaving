using System;

namespace StaticWeaving.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NotifyPropertyChangedAttribute : Attribute
    {
    }
}
