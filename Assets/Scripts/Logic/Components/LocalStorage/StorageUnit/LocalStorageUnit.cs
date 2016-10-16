using System;

namespace GameLogic.Components
{
    public abstract class LocalStorageUnit
    {
        public object Value { get; set; }
        public virtual short StorageValueType { get {return 0;} }
 
        public abstract int ToBinaryData(string key,ref byte[] dataBuff, int offset);

        public static byte[] CheckDataBuffer(byte[] data, int offset, int deltaLength)
        {
            if (deltaLength + offset <= data.Length)
            {
                return data;
            }

            var count = 2 * data.Length;
            while (offset + deltaLength > count)
            {
                count = 2 * count;
            }

            byte[] tempData = new byte[count];
            Array.Copy(data, tempData, data.Length);
            return tempData;
        }

        public static void AddDataToBuffer(byte[] source, int offset, byte[] destination)
        {
            for (int i = 0; i < destination.Length; ++i)
            {
                source[offset + i] = destination[i];
            }
        }
    }
}
