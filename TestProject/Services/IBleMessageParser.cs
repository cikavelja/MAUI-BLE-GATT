namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Interface for parsing BLE message data of different types
    /// </summary>
    public interface IBleMessageParser
    {
        /// <summary>
        /// Parse byte array to integer value
        /// </summary>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed integer value</returns>
        int ParseInt(byte[] data);
        
        /// <summary>
        /// Parse byte array to float value
        /// </summary>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed float value</returns>
        float ParseFloat(byte[] data);
        
        /// <summary>
        /// Parse byte array to UTF-8 string
        /// </summary>
        /// <param name="data">Raw byte array</param>
        /// <returns>Parsed string value</returns>
        string ParseString(byte[] data);
        
        /// <summary>
        /// Convert integer to byte array for writing to characteristic
        /// </summary>
        /// <param name="value">Integer value to convert</param>
        /// <returns>Byte array representation</returns>
        byte[] ToByteArray(int value);
        
        /// <summary>
        /// Convert float to byte array for writing to characteristic
        /// </summary>
        /// <param name="value">Float value to convert</param>
        /// <returns>Byte array representation</returns>
        byte[] ToByteArray(float value);
        
        /// <summary>
        /// Convert string to UTF-8 byte array for writing to characteristic
        /// </summary>
        /// <param name="value">String value to convert</param>
        /// <returns>Byte array representation</returns>
        byte[] ToByteArray(string value);
    }
}