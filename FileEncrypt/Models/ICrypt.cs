namespace FileEncrypt.Models
{
    public interface ICrypt
    {
        byte[] Encrypt(byte[] src, byte[] key);
    }
}
