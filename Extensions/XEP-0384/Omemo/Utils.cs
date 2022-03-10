using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public static class Utils
    {
        private static Random random = new Random();

        public static int GenerateRandomNumber(int lowerBound = 0, int upperBound = int.MaxValue)
        {
            return random.Next(lowerBound, upperBound);
        }

        public static uint GenerateRandomUint(int lowerBound = 0, int upperBound = int.MaxValue)
        {
            return Convert.ToUInt32(GenerateRandomNumber(lowerBound, upperBound));
        }

        /// <summary>
        /// Generates a random alphanumeric string.
        /// </summary>
        /// <param name="length">The desired length of the string</param>
        /// <returns>The string which has been generated</returns>
        public static string GenerateRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var randomString = new string(Enumerable.Repeat(chars, length)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }

        public static T SelectRandomItem<T>(this IEnumerable<T> input)
        {
            return input.ElementAt(GenerateRandomNumber(0, input.Count() - 1));
        }

    }
}
