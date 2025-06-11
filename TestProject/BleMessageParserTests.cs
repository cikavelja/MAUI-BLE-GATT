using System.Text;
using MauiBleApp2.Core.Services.Bluetooth;

namespace TestProject
{
    [TestFixture]
    public class BleMessageParserTests
    {
        private IBleMessageParser _parser = null!;

        [SetUp]
        public void Setup()
        {
            _parser = new BleMessageParser();
        }

        [Test]
        public void ParseInt_SingleByte_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = new byte[] { 42 };
            
            // Act
            var result = _parser.ParseInt(data);
            
            // Assert
            Assert.That(result, Is.EqualTo(42));
        }
        
        [Test]
        public void ParseInt_TwoBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = BitConverter.GetBytes((short)1234);
            
            // Act
            var result = _parser.ParseInt(data);
            
            // Assert
            Assert.That(result, Is.EqualTo(1234));
        }
        
        [Test]
        public void ParseInt_FourBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = BitConverter.GetBytes(987654321);
            
            // Act
            var result = _parser.ParseInt(data);
            
            // Assert
            Assert.That(result, Is.EqualTo(987654321));
        }

        [Test]
        public void ParseFloat_FourBytes_ReturnsCorrectValue()
        {
            // Arrange
            float expectedValue = 3.14159f;
            byte[] data = BitConverter.GetBytes(expectedValue);
            
            // Act
            var result = _parser.ParseFloat(data);
            
            // Assert
            Assert.That(result, Is.EqualTo(expectedValue).Within(0.0001));
        }

        [Test]
        public void ParseString_UTF8Bytes_ReturnsCorrectString()
        {
            // Arrange
            string originalString = "Hello, BLE World!";
            byte[] data = Encoding.UTF8.GetBytes(originalString);
            
            // Act
            var result = _parser.ParseString(data);
            
            // Assert
            Assert.That(result, Is.EqualTo(originalString));
        }

        [Test]
        public void ToByteArray_Int_ReturnsCorrectByteArray()
        {
            // Arrange
            int value = 12345678;
            byte[] expected = BitConverter.GetBytes(value);
            
            // Act
            var result = _parser.ToByteArray(value);
            
            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }
        
        [Test]
        public void ToByteArray_Float_ReturnsCorrectByteArray()
        {
            // Arrange
            float value = 2.71828f;
            byte[] expected = BitConverter.GetBytes(value);
            
            // Act
            var result = _parser.ToByteArray(value);
            
            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }
        
        [Test]
        public void ToByteArray_String_ReturnsCorrectByteArray()
        {
            // Arrange
            string value = "BLE Test String";
            byte[] expected = Encoding.UTF8.GetBytes(value);
            
            // Act
            var result = _parser.ToByteArray(value);
            
            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }
        
        [Test]
        public void ParseString_EmptyArray_ReturnsEmptyString()
        {
            // Arrange
            byte[] data = Array.Empty<byte>();
            
            // Act
            var result = _parser.ParseString(data);
            
            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }
        
        [Test]
        public void ParseString_NullArray_ReturnsEmptyString()
        {
            // Act
            var result = _parser.ParseString(null);
            
            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }
    }
}