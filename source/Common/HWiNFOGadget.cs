using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity
{
    public static class HWiNFOGadget
    {
        public static string GetData(long idx)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\HWiNFO64\VSB"))
            {
                object value = key?.GetValue($"ValueRaw{idx}");
                if (value == null)
                {
                    return null;
                }

                return value.ToString();
            }
        }
    }
}
