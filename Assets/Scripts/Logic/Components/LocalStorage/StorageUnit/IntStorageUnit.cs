using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Components
{
    public class IntStorageUnit : LocalStorageUnit
    {
        public static readonly short ValueType = 1;

        public override short StorageValueType
        {
            get { return ValueType; }
        }

        public IntStorageUnit(int value = 0)
        {
            Value = value;
        }

        public static KeyValuePair<string, LocalStorageUnit> ParseData(byte[] dataBuff, ref int offset)
        {
            var keyDataLength = BitConverter.ToInt32(dataBuff, offset);
            offset += 4;

            var key = Encoding.UTF8.GetString(dataBuff, offset, keyDataLength);
            offset += keyDataLength;

            var value = BitConverter.ToInt32(dataBuff, offset);
            offset += 4;

            return new KeyValuePair<string, LocalStorageUnit>(key, new IntStorageUnit(value));
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

            var valueBytes = BitConverter.GetBytes((int) Value);
            dataBuff = CheckDataBuffer(dataBuff, offset, valueBytes.Length);
            AddDataToBuffer(dataBuff, offset, valueBytes);
            offset += valueBytes.Length;
            return offset;
        }
    }
}