using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JqGridCodeGenerator.View.Pages
{
    public interface IBasePage
    {
        void GoToNextPage(JqGridCodeGeneratorWindow baseWindow);
        void GoToPreviousPage(JqGridCodeGeneratorWindow baseWindow);
    }
}
