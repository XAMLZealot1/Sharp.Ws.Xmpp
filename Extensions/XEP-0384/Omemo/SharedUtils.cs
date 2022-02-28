using System.Text;

namespace Sharp.Xmpp.Omemo
{
    public static class SharedUtils
    {
        /// <summary>
        /// Returns a hex string representing an unique device ID.
        /// The device ID is a SHA256 hash, hex string of the actual device ID XOR a device nonce to prevent tracking between apps.
        /// </summary>
        //public static string GetUniqueDeviceId()
        //{
        //    byte[] deviceId;

        //    SystemIdentificationInfo systemId = SystemIdentification.GetSystemIdForPublisher();
        //    if (systemId.Source != SystemIdentificationSource.None)
        //    {
        //        deviceId = systemId.Id.ToArray();
        //    }
        //    else
        //    {
        //        // Fall back to generating a unique ID based on the hardware of the system.
        //        // This ID will change once the hardware changes.
        //        // Based on: https://montemagno.com/unique-device-id-for-mobile-apps/
        //        HardwareToken hwToken = HardwareIdentification.GetPackageSpecificToken(null);
        //        deviceId = hwToken.Id.ToArray();
        //    }
        //    byte[] nonce = GetDeviceNonce();
        //    // Ensure the device ID is long enough:
        //    deviceId = deviceId.Length >= 32 ? XorShorten(deviceId, nonce) : nonce;
        //    SHA256 sha = SHA256.Create();
        //    deviceId = sha.ComputeHash(deviceId);
        //    return ByteArrayToHexString(deviceId);
        //}

        /// <summary>
        /// Returns a 64 byte long nonce generated on the first call.
        /// This nonce is stored persistently.
        /// </summary>
        /// <returns>64 byte long device nonce.</returns>
        //public static byte[] GetDeviceNonce()
        //{
        //    object nonceObj = null;
        //    try
        //    { nonceObj = ApplicationData.Current.LocalSettings.Values["DEVICE_NONCE"]; }
        //    catch { }

        //    // Return the existing nonce:
        //    if (nonceObj is byte[] nonce && nonce.Length == 64)
        //    {
        //        return nonce;
        //    }

        //    // Create a new nonce:
        //    Random r = new Random((int)DateTime.Now.Ticks);
        //    nonce = new byte[64];
        //    r.NextBytes(nonce);
        //    ApplicationData.Current.LocalSettings.Values["DEVICE_NONCE"] = nonce;
        //    return nonce;
        //}

        private static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Calculates the a XOR b.
        /// The result has the length of the shorter array.
        /// </summary>
        /// <param name="a">First input byte array.</param>
        /// <param name="b">Second input byte array.</param>
        /// <returns>a XOR b with the length of the shorter array.</returns>
        private static byte[] XorShorten(byte[] a, byte[] b)
        {
            int len = a.Length;
            if (b.Length < len)
            {
                len = b.Length;
            }

            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
            return result;
        }

        /// <summary>
        /// Converts the given byte array to a hex-string and returns it.
        /// </summary>
        public static string ToHexString(byte[] data)
        {
            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

    }
}
