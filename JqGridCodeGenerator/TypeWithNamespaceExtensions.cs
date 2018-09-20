using JqGridCodeGenerator.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JqGridCodeGenerator
{
    public static class TypeWithNamespaceExtensions
    {
        public static string GetNamespaceForType(this List<TypeWithNamespace> list,string typeName)
        {
            foreach(var typeWithNamespace in list)
            {
                if (typeWithNamespace.Name == typeName)
                    return typeWithNamespace.Nmspc;
            }

            return "unknownNamespaceForType_" + typeName;
        }
    }
}
