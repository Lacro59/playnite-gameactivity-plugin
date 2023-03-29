using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity
{
    class HWiNFOGadget
    {
        public static string GetData(long idx)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\HWiNFO64\VSB"))
            {
                return key?.GetValue($"ValueRaw{idx}").ToString();
            }
        }
    }
}
