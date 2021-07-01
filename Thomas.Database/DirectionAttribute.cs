using System;

namespace Thomas.Database
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DirectionAttribute : Attribute
    {
        public DirectionParameter Direction { get; set; }
        public DirectionAttribute(DirectionParameter direction)
        {
            Direction = direction;
        }
    }

}
