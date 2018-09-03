using JqGridCodeGenerator.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JqGridCodeGenerator
{
    public static class ColumnsExtension
    {
        public static string GetPrimaryKey(this List<Column> columns)
        {
            foreach (var column in columns)
                if (column.IsPrimaryKey)
                    return column.Name;
            return null;
        }
    }
}
