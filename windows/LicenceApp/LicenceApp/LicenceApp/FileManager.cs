using System;
using System.IO;

public static class FileManager
{
    // Uygulamanın temel dizinini tanımlar (örneğin, AppData içinde bir klasör)
    public static string BaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyLicenseApp");
    // Uygulamanın çalıştırıldığı dizin (örneğin, bin klasörü)
    public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;
    // Config dosyalarını içeren dizin
    public static string ConfigDirectory => Path.Combine(BaseDirectory, "config");

    // Data dosyalarını içeren dizin
    public static string DataDirectory => Path.Combine(BaseDirectory, "data");

    // Log dosyalarını içeren dizin
    public static string LogDirectory => Path.Combine(BaseDirectory, "logs");

    // Dosya yollarını tanımlayan özellikler
    public static string primaryPublicKeyPath => Path.Combine(AppDirectory, "primaryPublicKey.dat");
    public static string privateKey => Path.Combine(DataDirectory, "PrivateKey.dat");

    public static string sendAdminPath => Path.Combine(DataDirectory, "SendAdmin.dat");
    public static string AESKeyIVPath => Path.Combine(ConfigDirectory, "encryptedKeyIV.dat");
    public static string AESTextPath => Path.Combine(ConfigDirectory, "encryptedText.dat");
    public static string LogFilePath => Path.Combine(LogDirectory, "log.txt");
    public static string initialTimePath => Path.Combine(DataDirectory, "initialTime.dat");
    public static string timePublicKeyPath => Path.Combine(DataDirectory, "timePublicKey.dat");
    public static string timePrivateKeyPath => Path.Combine(DataDirectory, "timePrivateKey.dat");
    public static string setUpTime => Path.Combine(DataDirectory, "setUpTime.dat");

    public static string timeDeltaPath => Path.Combine(DataDirectory, "timeDelta.dat");
    public static string getLicencePath => Path.Combine(DataDirectory, "getLicence.dat");

    // Gerekli dizinleri oluşturur
    public static void CreateDirectories()
    {
        Console.WriteLine(AppDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
