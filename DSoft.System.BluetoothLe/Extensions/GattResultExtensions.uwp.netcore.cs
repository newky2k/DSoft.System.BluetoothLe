using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe;
using System.BluetoothLe.Exceptions;

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