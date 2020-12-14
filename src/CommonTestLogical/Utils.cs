using System;

namespace CommonTestLogical
{
    public static class Utils
    {
        private static readonly Random Random = new Random();

        public static string RandomEnv()
        {
            char[] c = new char[10];
            for (int i = 0; i < 10; i++)
            {
                var code = Random.Next(65, 123);
                if ((code >= 91 && code <= 96) || (code >= 58 && code <= 64))
                    code = Random.Next(65, 91);

                c[i] = (char) code;
            }

            return new string(c);
        }
    }
}