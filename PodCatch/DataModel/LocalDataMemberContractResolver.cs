using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PodCatch.DataModel
{
    public class LocalDataMemberContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = new List<JsonProperty>();

            foreach (MemberInfo memberInfo in type.GetTypeInfo().DeclaredMembers)
            {
                if (memberInfo.GetCustomAttributes(typeof(LocalDataMemberAttribute), false).Count() > 0)
                {
                    properties.Add(CreateProperty(memberInfo, memberSerialization));
                }
            }
            return properties;
        }
    }
}
