namespace AzureIoTEdgeModbus.Instrumentation
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EventAttribute : Attribute
    {
        public int Id { get; }

        public string Description { get; set; }

        public string Name { get; set; }

        public EventAttribute(int id)
        {
            this.Id = id;
        }
    }
}