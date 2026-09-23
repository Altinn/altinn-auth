using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core.Configuration
{
    /// <summary>
    /// Configuration of config options. Temporary fix until we have a better solution for translations in place. The dictionary is structured as follows:
    /// </summary>
    public class ActionTranslationsOptions : Dictionary<string, Dictionary<string, string>>
    {
    }
}
