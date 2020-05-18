//https://github.com/zipferot3000/HWiNFO-Shared-Memory-Dump
//https://github.com/Antiserum420/HwInfoReader

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace GameActivity
{
    class HWiNFODumper
    {
        public const string HWiNFO_SHARED_MEM_FILE_NAME = "Global\\HWiNFO_SENS_SM2";
        public const int HWiNFO_SENSORS_STRING_LEN = 128;
        public const int HWiNFO_UNIT_STRING_LEN = 16;
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        private _HWiNFO_SHARED_MEM HWiNFOMemory;
        private List<JsonObj> data = new List<JsonObj>();

        public List<JsonObj> ReadMem()
        {
            HWiNFOMemory = new _HWiNFO_SHARED_MEM();
            try
            {
                mmf = MemoryMappedFile.OpenExisting(HWiNFO_SHARED_MEM_FILE_NAME, MemoryMappedFileRights.Read);
                accessor = mmf.CreateViewAccessor(0L, Marshal.SizeOf(typeof(_HWiNFO_SHARED_MEM)), MemoryMappedFileAccess.Read);
                accessor.Read(0L, out HWiNFOMemory);
                return ReadSensorNames();
            }
            catch (Exception ex)
            {
                //Console.WriteLine("An error occured while opening the HWiNFO shared memory! - " + ex.Message);
                //Console.WriteLine("Press ENTER to exit program...");
                //Console.ReadLine();
                //Environment.Exit(1);
                return new List<JsonObj>();
            }
        }

        public List<JsonObj> ReadSensorNames()
        {
            for (uint index = 0; index < HWiNFOMemory.dwNumSensorElements; ++index)
            {
                using (MemoryMappedViewStream viewStream = mmf.CreateViewStream(HWiNFOMemory.dwOffsetOfSensorSection + index * HWiNFOMemory.dwSizeOfSensorElement, HWiNFOMemory.dwSizeOfSensorElement, MemoryMappedFileAccess.Read))
                {
                    byte[] buffer = new byte[(int)HWiNFOMemory.dwSizeOfSensorElement];
                    viewStream.Read(buffer, 0, (int)HWiNFOMemory.dwSizeOfSensorElement);
                    GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    _HWiNFO_SENSOR structure = (_HWiNFO_SENSOR)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(_HWiNFO_SENSOR));
                    gcHandle.Free();
                    JsonObj obj = new JsonObj
                    {
                        szSensorNameOrig = structure.szSensorNameOrig,
                        szSensorNameUser = structure.szSensorNameUser,
                        szSensorSensorID = ""+structure.dwSensorID,
                        sensors = new List<_HWiNFO_ELEMENT>()
                    };
                    data.Add(obj);
                }
            }
            return ReadSensors();
        }

        public List<JsonObj> ReadSensors()
        {
            for (uint index = 0; index < HWiNFOMemory.dwNumReadingElements; ++index)
            {
                using (MemoryMappedViewStream viewStream = mmf.CreateViewStream(HWiNFOMemory.dwOffsetOfReadingSection + index * HWiNFOMemory.dwSizeOfReadingElement, HWiNFOMemory.dwSizeOfReadingElement, MemoryMappedFileAccess.Read))
                {
                    byte[] buffer = new byte[(int)HWiNFOMemory.dwSizeOfReadingElement];
                    viewStream.Read(buffer, 0, (int)HWiNFOMemory.dwSizeOfReadingElement);
                    GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    _HWiNFO_ELEMENT structure = (_HWiNFO_ELEMENT)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(_HWiNFO_ELEMENT));
                    gcHandle.Free();
                    data[(int)structure.dwSensorIndex].sensors.Add(structure);
                }
            }
            return saveDataToFile();
        }

        public List<JsonObj> saveDataToFile()
        {
            byte[] json = new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(data, Formatting.Indented));

            //File.WriteAllText("d:\\test.json", JsonConvert.SerializeObject(data, Formatting.Indented));

            //using (FileStream fs = File.Create("d:\\mem_dump.json"))
            //{
            //    fs.Write(json, 0, json.Length);
            //    Console.WriteLine("Success dumped");
            //    return data;
            //}

            return data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _HWiNFO_SHARED_MEM
        {
            public uint dwSignature;
            public uint dwVersion;
            public uint dwRevision;
            public long poll_time;
            public uint dwOffsetOfSensorSection;
            public uint dwSizeOfSensorElement;
            public uint dwNumSensorElements;
            public uint dwOffsetOfReadingSection;
            public uint dwSizeOfReadingElement;
            public uint dwNumReadingElements;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _HWiNFO_SENSOR
        {
            public uint dwSensorID;                         // dwSensorID	        a unique Sensor ID
            public uint dwSensorInst;                       // dwSensorInst	        the instance of the sensor (together with dwSensorID forms a unique ID)
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN)]
            public string szSensorNameOrig;                 // szSensorNameOrig	    original sensor name
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN)]
            public string szSensorNameUser;                 // szSensorNameUser	    sensor name displayed, which might have been renamed by user
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _HWiNFO_ELEMENT
        {
            public SENSOR_READING_TYPE tReading;            // tReading	            type of sensor reading (e.g Clock, Temp, Volt)
            public uint dwSensorIndex;                      // dwSensorIndex	    this is the index of sensor in the Sensors[] array to which this reading belongs to
            public uint dwSensorID;                         // dwReadingID	        a unique ID of the reading within a particular sensor
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN)]
            public string szLabelOrig;                      // szLabelOrig	        original label (e.g. "Chassis2 Fan")
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN)]
            public string szLabelUser;                      // szLabelUser	        label displayed, which might have been renamed by user
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_UNIT_STRING_LEN)]
            public string szUnit;                           // szUnit	            e.g. "RPM"
            public double Value;                            // Value	            value of the sensor
            public double ValueMin;                         // ValueMin             min value of the sensor
            public double ValueMax;                         // ValueMax             max value of the sensor
            public double ValueAvg;                         // ValueAvg     	    average value of the sensor
        }
        public enum SENSOR_READING_TYPE
        {
            SENSOR_TYPE_NONE,
            SENSOR_TYPE_TEMP,
            SENSOR_TYPE_VOLT,
            SENSOR_TYPE_FAN,
            SENSOR_TYPE_CURRENT,
            SENSOR_TYPE_POWER,
            SENSOR_TYPE_CLOCK,
            SENSOR_TYPE_USAGE,
            SENSOR_TYPE_OTHER,
        }

        public class JsonObj
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN)]
            public string szSensorNameOrig;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN)]
            public string szSensorNameUser;
            public string szSensorSensorID;
            public List<_HWiNFO_ELEMENT> sensors;
        }
    }
}
