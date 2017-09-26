namespace FileEncrypt.Models
{
    public class CryptXor : ICrypt
    {
        public byte[] Encrypt(byte[] source, byte[] key)
        {
            var res = new byte[source.Length];

            for (var i = 0; i < res.Length; i++)
                res[i] = (byte)(source[i] ^ key[i % key.Length]);

            return res;
        }
    }
}
