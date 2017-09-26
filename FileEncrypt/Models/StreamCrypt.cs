using System.IO;

namespace FileEncrypt.Models
{
    public class StreamCrypt
    {
        private ICrypt crypter;

        public StreamCrypt(ICrypt crypter)
        {
            this.crypter = crypter;
        }

        public int Crypt(Stream stream, long offset, byte[] buff, byte[] key)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var readBytes = stream.Read(buff, 0, buff.Length);

            // encode
            var encrypted = crypter.Encrypt(buff, key);

            stream.Seek(offset, SeekOrigin.Begin);
            stream.Write(encrypted, 0, readBytes);

            return readBytes;
        }
    }
}
