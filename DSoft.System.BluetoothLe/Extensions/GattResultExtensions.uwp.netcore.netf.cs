using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe;

namespace System.BluetoothLe.Extensions
{
    public static class GattResultExtensions
    {
        public static void ThrowIfError(this GattWriteResult result, [CallerMemberName]string tag = null)
            => result.Status.ThrowIfError(tag, result.ProtocolError);

        public static void ThrowIfError(this GattCharacteristicsResult result, [CallerMemberName]string tag = null)
            => result.Status.ThrowIfError(tag, result.ProtocolError);

        public static void ThrowIfError(this GattDescriptorsResult result, [CallerMemberName]string tag = null)
            => result.Status.ThrowIfError(tag, result.ProtocolError);

        public static void ThrowIfError(this GattDeviceServicesResult result, [CallerMemberName]string tag = null)
            => result.Status.ThrowIfError(tag, result.ProtocolError);


        public static byte[] GetValueOrThrowIfError(this GattReadResult result, [CallerMemberName]string tag = null)
        {
            var errorMessage = result.Status.GetErrorMessage(tag, result.ProtocolError);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new CharacteristicReadException(errorMessage);
            }

            return result.Value?.ToArray() ?? new byte[0];
        }

        public static void ThrowIfError(this GattCommunicationStatus status, [CallerMemberName]string tag = null, byte? protocolError = null)
        {
            var errorMessage = status.GetErrorMessage(tag, protocolError);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new Exception(errorMessage);
            }
        }

        private static string GetErrorMessage(this GattCommunicationStatus status, string tag, byte? protocolError)
        {
            switch (status)
            {
                //output trace message with status of update
                case GattCommunicationStatus.Success:
                    Trace.Message($"[{tag}] success.");
                    return null;
                case GattCommunicationStatus.ProtocolError when protocolError != null:
                    return $"[{tag}] failed with status: {status} and protocol error {protocolError.GetErrorString()}";
                case GattCommunicationStatus.AccessDenied:
                case GattCommunicationStatus.ProtocolError:
                case GattCommunicationStatus.Unreachable:
                    return $"[{tag}] failed with status: {status}";
            }

            return null;
        }


        /*
        Windows Community Toolkit
        Copyright (c) .NET Foundation and Contributors

        All rights reserved.

        MIT License (MIT)
        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, 
        publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
        DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
         */

        /// <summary>
        /// Helper to convert an Gatt error value into a string
        /// </summary>
        /// <param name="errorValue"> the byte error value.</param>
        /// <returns>String representation of the error</returns>
        public static string GetErrorString(this byte? errorValue)
        {
            var errorString = "Protocol Error";

            if (errorValue.HasValue == false)
            {
                return errorString;
            }

            if (errorValue == GattProtocolError.AttributeNotFound)
            {
                return "Attribute Not Found";
            }

            if (errorValue == GattProtocolError.AttributeNotLong)
            {
                return "Attribute Not Long";
            }

            if (errorValue == GattProtocolError.InsufficientAuthentication)
            {
                return "Insufficient Authentication";
            }

            if (errorValue == GattProtocolError.InsufficientAuthorization)
            {
                return "Insufficient Authorization";
            }

            if (errorValue == GattProtocolError.InsufficientEncryption)
            {
                return "Insufficient Encryption";
            }

            if (errorValue == GattProtocolError.InsufficientEncryptionKeySize)
            {
                return "Insufficient Encryption Key Size";
            }

            if (errorValue == GattProtocolError.InsufficientResources)
            {
                return "Insufficient Resources";
            }

            if (errorValue == GattProtocolError.InvalidAttributeValueLength)
            {
                return "Invalid Attribute Value Length";
            }

            if (errorValue == GattProtocolError.InvalidHandle)
            {
                return "Invalid Handle";
            }

            if (errorValue == GattProtocolError.InvalidOffset)
            {
                return "Invalid Offset";
            }

            if (errorValue == GattProtocolError.InvalidPdu)
            {
                return "Invalid Pdu";
            }

            if (errorValue == GattProtocolError.PrepareQueueFull)
            {
                return "Prepare Queue Full";
            }

            if (errorValue == GattProtocolError.ReadNotPermitted)
            {
                return "Read Not Permitted";
            }

            if (errorValue == GattProtocolError.RequestNotSupported)
            {
                return "Request Not Supported";
            }

            if (errorValue == GattProtocolError.UnlikelyError)
            {
                return "UnlikelyError";
            }

            if (errorValue == GattProtocolError.UnsupportedGroupType)
            {
                return "Unsupported Group Type";
            }

            if (errorValue == GattProtocolError.WriteNotPermitted)
            {
                return "Write Not Permitted";
            }

            return errorString;
        }
    }
}