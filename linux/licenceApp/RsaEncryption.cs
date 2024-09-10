using LicenceApp;
using RSA_Cryptography;
using System.Security.Cryptography;

public class RSAEncryption
{
    private readonly string publicKey;
    private readonly string privateKey;
    public byte[] sendAdmin;
    public RSAEncryption()
    {
    }

    public byte[] GenerateAndSaveForAdmin(byte[] data, bool onlineStatus)
    {
        using (var rsa = new RSACryptoServiceProvider(1024))
        {
            try
            {
                if (File.Exists(FileManager.primaryPublicKeyPath))
                {
                    // Anahtarları alın
                    byte[] publicKey = rsa.ExportCspBlob(false); // Sadece public key
                    byte[] privateKey = rsa.ExportCspBlob(true); // Hem public hem de private key

                    byte[] allDataBytes = Combiner(data, publicKey);
                    byte[] primaryPublicKey =File.ReadAllBytes(FileManager.primaryPublicKeyPath);
                    byte[] encryptedData = EncryptText(allDataBytes, primaryPublicKey);
                    File.WriteAllBytes(FileManager.privateKey, privateKey);
                    if (onlineStatus)
                    {
                        this.sendAdmin = encryptedData;
                    }
                    else { 
                        File.WriteAllBytes(FileManager.sendAdminPath, encryptedData);
                    }
                    return privateKey;
                }
                else
                {
                    Logger.Log("[Installation] PrimaryPublicKey missing.");
                    return new byte[] { 0 };
                }
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
    }

    public (byte[], byte[]) getKeys()
    {
        using (var rsa = new RSACryptoServiceProvider(2048))
        {

            byte[] publicKey = rsa.ExportCspBlob(false);
            byte[] privateKey = rsa.ExportCspBlob(true);

            return (publicKey, privateKey);
        }
    }

    public byte[] Combiner(params byte[][] arrays)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            foreach (byte[] array in arrays)
            {
                // Her bir verinin uzunluğunu başına yaz
                writer.Write(array.Length);
                // Veriyi yaz
                writer.Write(array);
            }
            // Birleştirilmiş byte dizisini döndür
            return ms.ToArray();
        }
    }

    public byte[][] Separator(byte[] combinedData)
    {
        using (MemoryStream ms = new MemoryStream(combinedData))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            var separatedData = new System.Collections.Generic.List<byte[]>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                // Verinin uzunluğunu oku
                int length = reader.ReadInt32();
                // Veriyi oku
                byte[] data = reader.ReadBytes(length);
                // Veriyi listeye ekle
                separatedData.Add(data);
            }

            return separatedData.ToArray();
        }
    }

    public byte[] EncryptText(byte[] data, byte[] publicKey)
    {
        using (var rsa = new RSACryptoServiceProvider(4096))
        {
            rsa.ImportCspBlob(publicKey);
            byte[] encryptedData = rsa.Encrypt(data, false);
            return encryptedData;
        }
    }

    public bool DecryptLicence(byte[] encryptedText, byte[] privateKey, byte[] deviceInfo)
    {
        using (var rsa = new RSACryptoServiceProvider(1024))
        {
            rsa.ImportCspBlob(privateKey);
            byte[] decryptedData = rsa.Decrypt(encryptedText, false);

            byte[][] separatedInfos = Separator(decryptedData);
            byte[] licenceData = separatedInfos[0];
            byte[] separatedKey = separatedInfos[1];
            byte[] separatedIV = separatedInfos[2];
            AESControl aes = new AESControl();
            if (aes.Control(deviceInfo, separatedKey, separatedIV, licenceData))
            {
                aes.WriteKeyAndIvToFile(separatedKey, separatedIV);
                File.WriteAllBytes(FileManager.AESTextPath, licenceData);
                return true;
            }
            else
            {
                return false;
            }

        } 
    }
    public byte[] DecryptResult(byte[] encryptedText)
    {
        byte[] privateKey = File.ReadAllBytes(FileManager.privateKey);
        using (var rsa = new RSACryptoServiceProvider(1024))
        {
            rsa.ImportCspBlob(privateKey);
            return rsa.Decrypt(encryptedText, false);
        }
    }

}
