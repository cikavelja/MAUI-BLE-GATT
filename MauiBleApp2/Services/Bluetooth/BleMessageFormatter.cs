using System;
using System.Collections.Generic;
using System.Text;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Specialized message formatter for handling structured data in BLE characteristics
    /// </summary>
    public class BleMessageFormatter
    {
        private readonly EnhancedBleMessageParser _parser;
        private readonly Dictionary<string, IMessageFormat> _messageFormats = new Dictionary<string, IMessageFormat>();

        public BleMessageFormatter(EnhancedBleMessageParser parser)
        {
            _parser = parser;
            
            // Register common message formats
            RegisterFormat("Health", new HealthDataFormat());
            RegisterFormat("Environment", new EnvironmentalFormat());
            RegisterFormat("CustomStructure", new CustomStructuredFormat());
        }

        /// <summary>
        /// Register a new message format
        /// </summary>
        /// <param name="formatId">Unique identifier for the format</param>
        /// <param name="format">Message format implementation</param>
        public void RegisterFormat(string formatId, IMessageFormat format)
        {
            _messageFormats[formatId] = format;
        }

        /// <summary>
        /// Format data for sending to a BLE device
        /// </summary>
        /// <param name="formatId">The format to use</param>
        /// <param name="data">Data dictionary to format</param>
        /// <returns>Formatted byte array</returns>
        public byte[] FormatMessage(string formatId, Dictionary<string, object> data)
        {
            if (_messageFormats.TryGetValue(formatId, out var format))
            {
                return format.FormatData(data, _parser);
            }
            
            throw new InvalidOperationException($"No format registered with ID '{formatId}'");
        }

        /// <summary>
        /// Parse received data according to a specific format
        /// </summary>
        /// <param name="formatId">The format to use</param>
        /// <param name="data">Raw byte array from BLE characteristic</param>
        /// <returns>Parsed data dictionary</returns>
        public Dictionary<string, object> ParseMessage(string formatId, byte[] data)
        {
            if (_messageFormats.TryGetValue(formatId, out var format))
            {
                return format.ParseData(data, _parser);
            }
            
            throw new InvalidOperationException($"No format registered with ID '{formatId}'");
        }
    }

    /// <summary>
    /// Interface for message format implementations
    /// </summary>
    public interface IMessageFormat
    {
        /// <summary>
        /// Convert structured data to byte array
        /// </summary>
        byte[] FormatData(Dictionary<string, object> data, EnhancedBleMessageParser parser);
        
        /// <summary>
        /// Parse byte array to structured data
        /// </summary>
        Dictionary<string, object> ParseData(byte[] data, EnhancedBleMessageParser parser);
    }

    #region Message Format Implementations

    /// <summary>
    /// Format for health-related data (heart rate, steps, etc.)
    /// </summary>
    public class HealthDataFormat : IMessageFormat
    {
        // Format: [1 byte header][1 byte flags][2 bytes heart rate][4 bytes steps][2 bytes calories]
        private const byte HEADER = 0x48; // 'H'
        
        public byte[] FormatData(Dictionary<string, object> data, EnhancedBleMessageParser parser)
        {
            byte[] result = new byte[10]; // Fixed size message
            
            // Header
            result[0] = HEADER;
            
            // Flags (bit field: 0x01=hasHeartRate, 0x02=hasSteps, 0x04=hasCalories)
            byte flags = 0;
            
            // Heart Rate (2 bytes)
            if (data.TryGetValue("heartRate", out var hrObject) && hrObject is int heartRate)
            {
                flags |= 0x01;
                BitConverter.GetBytes((ushort)heartRate).CopyTo(result, 2);
            }
            
            // Steps (4 bytes)
            if (data.TryGetValue("steps", out var stepsObject) && stepsObject is int steps)
            {
                flags |= 0x02;
                BitConverter.GetBytes(steps).CopyTo(result, 4);
            }
            
            // Calories (2 bytes)
            if (data.TryGetValue("calories", out var calObject) && calObject is int calories)
            {
                flags |= 0x04;
                BitConverter.GetBytes((ushort)calories).CopyTo(result, 8);
            }
            
            // Set flags byte
            result[1] = flags;
            
            return result;
        }

        public Dictionary<string, object> ParseData(byte[] data, EnhancedBleMessageParser parser)
        {
            if (data == null || data.Length < 10 || data[0] != HEADER)
            {
                throw new ArgumentException("Invalid health data format");
            }
            
            var result = new Dictionary<string, object>();
            byte flags = data[1];
            
            // Parse Heart Rate
            if ((flags & 0x01) != 0)
            {
                result["heartRate"] = BitConverter.ToUInt16(data, 2);
            }
            
            // Parse Steps
            if ((flags & 0x02) != 0)
            {
                result["steps"] = BitConverter.ToInt32(data, 4);
            }
            
            // Parse Calories
            if ((flags & 0x04) != 0)
            {
                result["calories"] = BitConverter.ToUInt16(data, 8);
            }
            
            return result;
        }
    }

    /// <summary>
    /// Format for environmental data (temperature, humidity, pressure)
    /// </summary>
    public class EnvironmentalFormat : IMessageFormat
    {
        // Format: [1 byte header][1 byte flags][4 bytes temperature][4 bytes humidity][4 bytes pressure]
        private const byte HEADER = 0x45; // 'E'
        
        public byte[] FormatData(Dictionary<string, object> data, EnhancedBleMessageParser parser)
        {
            byte[] result = new byte[14]; // Fixed size message
            
            // Header
            result[0] = HEADER;
            
            // Flags (bit field: 0x01=hasTemperature, 0x02=hasHumidity, 0x04=hasPressure)
            byte flags = 0;
            
            // Temperature (4 bytes float)
            if (data.TryGetValue("temperature", out var tempObject))
            {
                flags |= 0x01;
                float temp = Convert.ToSingle(tempObject);
                BitConverter.GetBytes(temp).CopyTo(result, 2);
            }
            
            // Humidity (4 bytes float)
            if (data.TryGetValue("humidity", out var humObject))
            {
                flags |= 0x02;
                float humidity = Convert.ToSingle(humObject);
                BitConverter.GetBytes(humidity).CopyTo(result, 6);
            }
            
            // Pressure (4 bytes float)
            if (data.TryGetValue("pressure", out var pressObject))
            {
                flags |= 0x04;
                float pressure = Convert.ToSingle(pressObject);
                BitConverter.GetBytes(pressure).CopyTo(result, 10);
            }
            
            // Set flags byte
            result[1] = flags;
            
            return result;
        }

        public Dictionary<string, object> ParseData(byte[] data, EnhancedBleMessageParser parser)
        {
            if (data == null || data.Length < 14 || data[0] != HEADER)
            {
                throw new ArgumentException("Invalid environmental data format");
            }
            
            var result = new Dictionary<string, object>();
            byte flags = data[1];
            
            // Parse Temperature
            if ((flags & 0x01) != 0)
            {
                result["temperature"] = BitConverter.ToSingle(data, 2);
            }
            
            // Parse Humidity
            if ((flags & 0x02) != 0)
            {
                result["humidity"] = BitConverter.ToSingle(data, 6);
            }
            
            // Parse Pressure
            if ((flags & 0x04) != 0)
            {
                result["pressure"] = BitConverter.ToSingle(data, 10);
            }
            
            return result;
        }
    }

    /// <summary>
    /// Format for custom structured data with variable fields
    /// </summary>
    public class CustomStructuredFormat : IMessageFormat
    {
        // Format: [1 byte header][1 byte field count][field entries]
        // Each field entry: [1 byte field type][1 byte field length][N bytes field data]
        private const byte HEADER = 0x43; // 'C'
        
        // Field types
        private const byte TYPE_INT8 = 0x01;
        private const byte TYPE_UINT8 = 0x02;
        private const byte TYPE_INT16 = 0x03;
        private const byte TYPE_UINT16 = 0x04;
        private const byte TYPE_INT32 = 0x05;
        private const byte TYPE_FLOAT = 0x06;
        private const byte TYPE_STRING = 0x07;
        private const byte TYPE_BOOL = 0x08;
        
        public byte[] FormatData(Dictionary<string, object> data, EnhancedBleMessageParser parser)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                // Header
                stream.WriteByte(HEADER);
                
                // Field count
                stream.WriteByte((byte)data.Count);
                
                // Write each field
                foreach (var kvp in data)
                {
                    string fieldName = kvp.Key;
                    object value = kvp.Value;
                    byte[] fieldNameBytes = Encoding.UTF8.GetBytes(fieldName);
                    
                    // Field name length
                    stream.WriteByte((byte)fieldNameBytes.Length);
                    
                    // Field name
                    stream.Write(fieldNameBytes, 0, fieldNameBytes.Length);
                    
                    // Determine field type and write data
                    if (value is byte b)
                    {
                        stream.WriteByte(TYPE_UINT8);
                        stream.WriteByte(1); // Length
                        stream.WriteByte(b);
                    }
                    else if (value is sbyte sb)
                    {
                        stream.WriteByte(TYPE_INT8);
                        stream.WriteByte(1); // Length
                        stream.WriteByte((byte)sb);
                    }
                    else if (value is short s)
                    {
                        stream.WriteByte(TYPE_INT16);
                        stream.WriteByte(2); // Length
                        byte[] bytes = BitConverter.GetBytes(s);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else if (value is ushort us)
                    {
                        stream.WriteByte(TYPE_UINT16);
                        stream.WriteByte(2); // Length
                        byte[] bytes = BitConverter.GetBytes(us);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else if (value is int i)
                    {
                        stream.WriteByte(TYPE_INT32);
                        stream.WriteByte(4); // Length
                        byte[] bytes = BitConverter.GetBytes(i);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else if (value is float f)
                    {
                        stream.WriteByte(TYPE_FLOAT);
                        stream.WriteByte(4); // Length
                        byte[] bytes = BitConverter.GetBytes(f);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else if (value is string str)
                    {
                        stream.WriteByte(TYPE_STRING);
                        byte[] strBytes = Encoding.UTF8.GetBytes(str);
                        stream.WriteByte((byte)strBytes.Length);
                        stream.Write(strBytes, 0, strBytes.Length);
                    }
                    else if (value is bool boolean)
                    {
                        stream.WriteByte(TYPE_BOOL);
                        stream.WriteByte(1); // Length
                        stream.WriteByte(boolean ? (byte)1 : (byte)0);
                    }
                    else
                    {
                        // For unsupported types, convert to string
                        stream.WriteByte(TYPE_STRING);
                        byte[] strBytes = Encoding.UTF8.GetBytes(value.ToString());
                        stream.WriteByte((byte)strBytes.Length);
                        stream.Write(strBytes, 0, strBytes.Length);
                    }
                }
                
                return stream.ToArray();
            }
        }

        public Dictionary<string, object> ParseData(byte[] data, EnhancedBleMessageParser parser)
        {
            if (data == null || data.Length < 2 || data[0] != HEADER)
            {
                throw new ArgumentException("Invalid custom structured data format");
            }
            
            var result = new Dictionary<string, object>();
            int fieldCount = data[1];
            int position = 2;
            
            for (int i = 0; i < fieldCount && position < data.Length; i++)
            {
                // Read field name
                int fieldNameLength = data[position++];
                string fieldName = Encoding.UTF8.GetString(data, position, fieldNameLength);
                position += fieldNameLength;
                
                // Read field type and data
                byte fieldType = data[position++];
                byte fieldDataLength = data[position++];
                
                switch (fieldType)
                {
                    case TYPE_INT8:
                        result[fieldName] = (sbyte)data[position];
                        break;
                    case TYPE_UINT8:
                        result[fieldName] = data[position];
                        break;
                    case TYPE_INT16:
                        result[fieldName] = BitConverter.ToInt16(data, position);
                        break;
                    case TYPE_UINT16:
                        result[fieldName] = BitConverter.ToUInt16(data, position);
                        break;
                    case TYPE_INT32:
                        result[fieldName] = BitConverter.ToInt32(data, position);
                        break;
                    case TYPE_FLOAT:
                        result[fieldName] = BitConverter.ToSingle(data, position);
                        break;
                    case TYPE_STRING:
                        result[fieldName] = Encoding.UTF8.GetString(data, position, fieldDataLength);
                        break;
                    case TYPE_BOOL:
                        result[fieldName] = data[position] != 0;
                        break;
                }
                
                position += fieldDataLength;
            }
            
            return result;
        }
    }

    #endregion
}