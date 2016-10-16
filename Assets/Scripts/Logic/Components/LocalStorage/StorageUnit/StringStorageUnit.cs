using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Components
{
    public class StringStorageUnit : LocalStorageUnit
    {
        public static readonly short ValueType = 3;

        public override short StorageValueType
        {
            get { return ValueType; }
        }

        public StringStorageUnit(string value = "")
        {
            Value = value;
        }

        public static KeyValuePair<string, LocalStorageUnit> ParseData(byte[] dataBuff, ref int offset)
        {
            var keyDataLength = BitConverter.ToInt32(dataBuff, offset);
            offset += 4;

            var key = Encoding.UTF8.GetString(dataBuff, offset, keyDataLength);
            offset += keyDataLength;

            var valueDataLength = BitConverter.ToInt32(dataBuff, offset);
            offset += 4;

            var value = Encoding.UTF8.GetString(dataBuff, offset, valueDataLength);
            offset += valueDataLength;

            return new KeyValuePair<string, LocalStorageUnit>(key, new StringStorageUnit(value));
        }

        public override int ToBinaryData(string key, ref byte[] dataBuff, int offset)
        {
            var typeBytes = BitConverter.GetBytes(ValueType);
            dataBuff = CheckDataBuffer(dataBuff, offset, typeBytes.Length);
            AddDataToBuffer(dataBuff, offset, typeBytes);
            offset += typeBytes.Length;

            var keyBytes = Encoding.UTF8.GetBytes(key);
            dataBuff = CheckDataBuffer(dataBuff, offset, keyBytes.Length + 4);
            AddDataToBuffer(dataBuff, offset, BitConverter.GetBytes(keyBytes.Length));
            offset += 4;
            AddDataToBuffer(dataBuff, offset, keyBytes);
            offset += keyBytes.Length;

            var valueBytes = Encoding.UTF8.GetBytes((string) Value);
            dataBuff = CheckDataBuffer(dataBuff, offset, valueBytes.Length + 4);
            AddDataToBuffer(dataBuff, offset, BitConverter.GetBytes(valueBytes.Length));
            offset += 4;
            AddDataToBuffer(dataBuff, offset, valueBytes);
            offset += valueBytes.Length;

            return offset;
        }
    }
}