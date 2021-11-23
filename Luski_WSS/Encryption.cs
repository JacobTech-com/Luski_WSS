using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Luski_WSS
{
    internal static class Encryption
    {
        private static readonly UnicodeEncoding _encoder = new();

        public static byte[] Encrypt(string data, string key)
        {
            using RSACryptoServiceProvider rsa = new();
            rsa.FromXmlString(key);
            byte[] dataToEncrypt = _encoder.GetBytes(data);
            double x = ((double)dataToEncrypt.Length / (double)500);
            int bbb = int.Parse(x.ToString().Split('.')[0]);
            if (x.ToString().Contains('.')) bbb++;
            byte[][] datasplit = Array.Empty<byte[]>();
            byte[] datasplitout = Array.Empty<byte>();
            Array.Resize(ref datasplit, bbb);
            for (int i = 0; i < bbb; i++)
            {
                byte[] fff = dataToEncrypt.Skip(i * 500).Take(500).ToArray();
                datasplit[i] = fff;
                datasplitout = Combine(datasplitout, rsa.Encrypt(datasplit[i], false));
            }
            return datasplitout;
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }
    }
}
