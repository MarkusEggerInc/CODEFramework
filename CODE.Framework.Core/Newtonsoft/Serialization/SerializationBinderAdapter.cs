using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Newtonsoft.Serialization
{
    internal class SerializationBinderAdapter : ISerializationBinder
    {
#pragma warning disable 618
        public readonly SerializationBinder SerializationBinder;
#pragma warning restore 618

#pragma warning disable 618
        public SerializationBinderAdapter(SerializationBinder serializationBinder)
        {
            SerializationBinder = serializationBinder;
        }
#pragma warning restore 618

        public Type BindToType(string assemblyName, string typeName)
        {
            return SerializationBinder.BindToType(assemblyName, typeName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
#if HAVE_SERIALIZATION_BINDER_BIND_TO_NAME
            SerializationBinder.BindToName(serializedType, out assemblyName, out typeName);
#else
            assemblyName = null;
            typeName = null;
#endif
        }
    }
}