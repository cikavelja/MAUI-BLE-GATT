using System;
using System.Text;

namespace MauiBleApp2.Core.Services.Bluetooth
{
    /// <summary>
    /// Implementation of IBleMessageParser for parsing BLE messages
    /// </summary>
    public class BleMessageParser : IBleMessageParser
    {
        /// <summary>
        /// Parse byte array to integer value
        /// </summary>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed integer value</returns>
        public int ParseInt(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            // Handle different byte array sizes
            switch (data.Length)
            {
                case 1:
                    return data[0];
                case 2:
                    return BitConverter.ToInt16(data, 0);
                case 4:
                    return BitConverter.ToInt32(data, 0);
                default:
                    // Default to 32-bit integer if size doesn't match expected
                    if (data.Length > 4)
                        return BitConverter.ToInt32(data, 0);
                    else
                    {
                        // Pad to 4 bytes for shorter arrays
                        byte[] paddedData = new byte[4];
                        Array.Copy(data, 0, paddedData, 0, data.Length);
                        return BitConverter.ToInt32(paddedData, 0);
                    }
            }
        }

        /// <summary>
        /// Parse byte array to float value
        /// </summary>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed float value</returns>
        public float ParseFloat(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                // Pad to 4 bytes if necessary
                if (data != null && data.Length > 0)
                {
                    byte[] paddedData = new byte[4];
                    Array.Copy(data, 0, paddedData, 0, data.Length);
                    return BitConverter.ToSingle(paddedData, 0);
                }
                return 0f;
            }

            return BitConverter.ToSingle(data, 0);
        }

        /// <summary>
        /// Parse byte array to UTF-8 string
        /// </summary>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed string value</returns>
        public string ParseString(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Convert integer to byte array for writing to characteristic
        /// </summary>
        /// <param name="value">Integer value to convert</param>
        /// <returns>Byte array representation</returns>
        public byte[] ToByteArray(int value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert float to byte array for writing to characteristic
        /// </summary>
        /// <param name="value">Float value to convert</param>
        /// <returns>Byte array representation</returns>
        public byte[] ToByteArray(float value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert string to UTF-8 byte array for writing to characteristic
        /// </summary>
        /// <param name="value">String value to convert</param>
        /// <returns>Byte array representation</returns>
        public byte[] ToByteArray(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<byte>();

            return Encoding.UTF8.GetBytes(value);
        }
    }
}