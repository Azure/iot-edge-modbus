namespace AzureIoTEdgeModbus.Instrumentation
{
    using System;
    using System.Reflection;

    public abstract class EventLog
    {
        public void LogTrace(MemberInfo mi, params object[] args)
        {
            var eventAttribute = (EventAttribute) Attribute.GetCustomAttribute(mi, typeof(EventAttribute));
            this.LogTrace(eventAttribute.Id, eventAttribute.Name??mi.Name, eventAttribute.Description, args);
        }

        protected abstract void LogTrace(int id, string name, string description, params object[] args);

        public void LogInformation(MemberInfo mi, params object[] args)
        {
            var eventAttribute = (EventAttribute)Attribute.GetCustomAttribute(mi, typeof(EventAttribute));
            this.LogInformation(eventAttribute.Id, eventAttribute.Name ?? mi.Name, eventAttribute.Description, args);
        }

        protected abstract void LogInformation(int id, string name, string description, params object[] args);

        public void LogWarning(MemberInfo mi, params object[] args)
        {
            var eventAttribute = (EventAttribute)Attribute.GetCustomAttribute(mi, typeof(EventAttribute));
            this.LogInformation(eventAttribute.Id, eventAttribute.Name ?? mi.Name, eventAttribute.Description, args);
        }

        protected abstract void LogWarning(int id, string name, string description, params object[] args);


        public void LogError(MemberInfo mi, Exception exception, params object[] args)
        {
            var eventAttribute = (EventAttribute)Attribute.GetCustomAttribute(mi, typeof(EventAttribute));
            this.LogError(eventAttribute.Id, eventAttribute.Name ?? mi.Name, exception, eventAttribute.Description, args);
        }

        protected abstract void LogError(int id, string name, Exception exception, string description, params object[] args);
    }
}