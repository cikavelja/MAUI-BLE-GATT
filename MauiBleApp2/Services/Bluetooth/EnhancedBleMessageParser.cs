using System;
using System.Collections.Generic;
using System.Text;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Enhanced BLE message parser with support for multiple data formats and modular design
    /// </summary>
    public class EnhancedBleMessageParser : IBleMessageParser
    {
        // The dictionary of parser strategies
        private readonly Dictionary<Type, IParserStrategy> _parserStrategies = new Dictionary<Type, IParserStrategy>();

        /// <summary>
        /// Constructor that configures the default parser strategies
        /// </summary>
        public EnhancedBleMessageParser()
        {
            // Register default parser strategies
            RegisterParser(new Int32ParserStrategy());
            RegisterParser(new FloatParserStrategy());
            RegisterParser(new StringParserStrategy());
            RegisterParser(new BoolParserStrategy());
            RegisterParser(new ByteParserStrategy());
            RegisterParser(new UInt16ParserStrategy());
            RegisterParser(new Int16ParserStrategy());
        }

        /// <summary>
        /// Register a new parser strategy
        /// </summary>
        /// <param name="strategy">The parser strategy to register</param>
        public void RegisterParser<T>(IParserStrategy<T> strategy)
        {
            _parserStrategies[typeof(T)] = strategy;
        }

        /// <summary>
        /// Parse byte array to a specific type
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed value</returns>
        public T Parse<T>(byte[] data)
        {
            if (_parserStrategies.TryGetValue(typeof(T), out var strategy))
            {
                return (T)strategy.Parse(data);
            }
            
            throw new InvalidOperationException($"No parser registered for type {typeof(T).Name}");
        }

        /// <summary>
        /// Convert a typed value to byte array
        /// </summary>
        /// <typeparam name="T">The source type</typeparam>
        /// <param name="value">Value to convert</param>
        /// <returns>Byte array representation</returns>
        public byte[] ToByteArray<T>(T value)
        {
            if (_parserStrategies.TryGetValue(typeof(T), out var strategy))
            {
                var typedStrategy = (IParserStrategy<T>)strategy;
                return typedStrategy.ToByteArray(value);
            }
            
            throw new InvalidOperationException($"No parser registered for type {typeof(T).Name}");
        }

        // IBleMessageParser implementation (for compatibility)
        public int ParseInt(byte[] data) => Parse<int>(data);
        public float ParseFloat(byte[] data) => Parse<float>(data);
        public string ParseString(byte[] data) => Parse<string>(data);
        public byte[] ToByteArray(int value) => ToByteArray<int>(value);
        public byte[] ToByteArray(float value) => ToByteArray<float>(value);
        public byte[] ToByteArray(string value) => ToByteArray<string>(value);
    }

    /// <summary>
    /// Base interface for parser strategies
    /// </summary>
    public interface IParserStrategy
    {
        Type GetSupportedType();
        object Parse(byte[] data);
    }

    /// <summary>
    /// Typed parser strategy interface
    /// </summary>
    public interface IParserStrategy<T> : IParserStrategy
    {
        new T Parse(byte[] data);
        byte[] ToByteArray(T value);
    }

    /// <summary>
    /// Base implementation for parser strategies
    /// </summary>
    public abstract class ParserStrategy<T> : IParserStrategy<T>
    {
        public Type GetSupportedType() => typeof(T);
        
        public abstract T Parse(byte[] data);
        
        public abstract byte[] ToByteArray(T value);
        
        object IParserStrategy.Parse(byte[] data) => Parse(data);
    }

    #region Parser Strategy Implementations

    /// <summary>
    /// Parser strategy for int values
    /// </summary>
    public class Int32ParserStrategy : ParserStrategy<int>
    {
        public override int Parse(byte[] data)
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

        public override byte[] ToByteArray(int value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    /// <summary>
    /// Parser strategy for float values
    /// </summary>
    public class FloatParserStrategy : ParserStrategy<float>
    {
        public override float Parse(byte[] data)
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

        public override byte[] ToByteArray(float value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    /// <summary>
    /// Parser strategy for string values
    /// </summary>
    public class StringParserStrategy : ParserStrategy<string>
    {
        public override string Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(data);
        }

        public override byte[] ToByteArray(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<byte>();

            return Encoding.UTF8.GetBytes(value);
        }
    }

    /// <summary>
    /// Parser strategy for boolean values
    /// </summary>
    public class BoolParserStrategy : ParserStrategy<bool>
    {
        public override bool Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
                return false;

            return data[0] != 0;
        }

        public override byte[] ToByteArray(bool value)
        {
            return new byte[] { (byte)(value ? 1 : 0) };
        }
    }

    /// <summary>
    /// Parser strategy for byte values
    /// </summary>
    public class ByteParserStrategy : ParserStrategy<byte>
    {
        public override byte Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            return data[0];
        }

        public override byte[] ToByteArray(byte value)
        {
            return new byte[] { value };
        }
    }

    /// <summary>
    /// Parser strategy for UInt16 values
    /// </summary>
    public class UInt16ParserStrategy : ParserStrategy<ushort>
    {
        public override ushort Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            if (data.Length >= 2)
            {
                return BitConverter.ToUInt16(data, 0);
            }
            else
            {
                byte[] paddedData = new byte[2];
                Array.Copy(data, 0, paddedData, 0, data.Length);
                return BitConverter.ToUInt16(paddedData, 0);
            }
        }

        public override byte[] ToByteArray(ushort value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    /// <summary>
    /// Parser strategy for Int16 values
    /// </summary>
    public class Int16ParserStrategy : ParserStrategy<short>
    {
        public override short Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            if (data.Length >= 2)
            {
                return BitConverter.ToInt16(data, 0);
            }
            else
            {
                byte[] paddedData = new byte[2];
                Array.Copy(data, 0, paddedData, 0, data.Length);
                return BitConverter.ToInt16(paddedData, 0);
            }
        }

        public override byte[] ToByteArray(short value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    #endregion
}