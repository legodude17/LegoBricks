using System;

namespace ModBase
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DefaultAttribute : Attribute
    {
        public string Field;
        public string Getter;
        public bool GetterWantsObject;
        public object Static;

        public DefaultAttribute(object d)
        {
            Static = d;
        }

        public DefaultAttribute(string getter, bool passObject)
        {
            Getter = getter;
            GetterWantsObject = passObject;
        }

        public DefaultAttribute(string field)
        {
            Field = field;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SettingsSetterAttribute : Attribute
    {
        public string MethodName;

        public SettingsSetterAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}