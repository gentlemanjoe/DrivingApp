using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTBrowser
{
    public class PeriodicUpdateInfo
    {
        public PeriodicUpdateInfo(bool isActive)
        {
            IsActive = isActive;
        }

        public bool IsActive { get; set; }
    }
}
