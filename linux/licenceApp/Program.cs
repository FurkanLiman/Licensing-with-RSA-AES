using RSA_Cryptography;
using System.Text;
using LicenceApp;
using System.IO.Pipes;
using System.Diagnostics;
using System.CodeDom;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Security.Cryptography;
using System;
using System.Web;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

class Program
{
    static bool appStatus;
    static int? pid = null;
    static string connectionLink = "https://localhost:7133";
    static async Task Main(string[] args)
    {
        FileManager.CreateDirectories();

        Logger.Log("[Start] licenceApp starting.");
        Console.WriteLine(FileManager.LogDirectory);
        Task listenTask = pipeLine();
        if (IsInternetAvailable())
        {
            if (setUpControl())
            {
                await backgroundLicenceCheckOnline();
                await listenTask;
            }
            else
            {
                if (await setUpOnline())
                {
                    Console.WriteLine("The installation has been completed successfully.");
                    await backgroundLicenceCheckOnline();
                    await listenTask;
                }

            }
        }
        else
        {
            if (setUpControl())
            {
                await backgroundLicenceCheck();
                await listenTask;
            }
            else
            {
                if (setUp())
                {
                    Console.WriteLine("The installation has been completed successfully.");
                    await backgroundLicenceCheck();
                    await listenTask;
                }
            }
        }
    }
    
    static bool IsInternetAvailable()
    {
        try
        {
            using (var ping = new Ping())
            {
                var reply = ping.Send("8.8.8.8", 1000);
                return reply.Status == IPStatus.Success;
            }
        }
        catch
        {
            return false;
        }
    }
    static async Task<DateTime> GetServerTimeAsync()
    {
        try
        {
            HttpClient client = new HttpClient();
            // Lisans sunucusundaki API'ye GET isteği gönder
            var response = await client.GetStringAsync("https://localhost:7133/license/GetTime");
            // JSON yanıtını parse et
            var jsonResponse = JObject.Parse(response);

            // JSON içerisindeki utc_time değerini al
            var serverTime = DateTime.Parse(jsonResponse["utc_time"].ToString());

            return serverTime.ToUniversalTime(); // UTC zamanına dönüştür
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Zaman alımı başarısız oldu: {ex.Message}");
            return DateTime.MinValue; // Başarısız durumda en düşük DateTime değeri döner
        }
    }

    static async Task<bool> IsSystemTimeManipulated()
    {
        DateTime serverTime = await GetServerTimeAsync();
        DateTime localTime = DateTime.UtcNow;
        TimeSpan difference = localTime - serverTime;
        // Eğer fark belirli bir eşiğin üzerinde ise manipülasyon var demektir
        Logger.Log($"[Time] Server time difference checked. Difference(hour):{difference.TotalHours}");
        return Math.Abs(difference.TotalHours) < 24; // 5 dakika tolerans
    }
    static async Task backgroundLicenceCheckOnline()
    {
        Random r= new Random();
        while (IsInternetAvailable())
        {
            int looptime= r.Next(5,15);
            Console.WriteLine($"Next request in {looptime} seconds...");
            await Task.Delay(looptime*1000);
            if (!appStatus)
            {//time validate
                if (!(await IsSystemTimeManipulated() && await UserCheckRequest()))
                {
                    if (pid.HasValue)
                    {
                        processKill(pid.Value);
                    }
                    int selfPId = Process.GetCurrentProcess().Id;
                    processKill(selfPId);
                }
                else
                {
                    Logger.Log("[Time Checked]");
                    Logger.Log("[Licence Checked]");
                    Console.WriteLine("Licence Checked.");
                }
            }
            else
            {//time validate
                if (!await IsSystemTimeManipulated())
                {
                    if (pid.HasValue)
                    {
                        processKill(pid.Value);
                    }
                    int selfPId = Process.GetCurrentProcess().Id;
                    processKill(selfPId);
                }
                else
                {
                    Logger.Log("[Time Checked]");
                    Console.WriteLine("Licence Checked.");
                }
            }

        }
        await backgroundLicenceCheck();
    }

    private static async Task<bool> UserCheckRequest()
    {
        string username = Environment.UserName;
        DeviceInfo deviceInfo = new DeviceInfo();
        RSAEncryption rsa = new RSAEncryption();
        byte[] combined = rsa.Combiner(Encoding.UTF8.GetBytes(username), deviceInfo.getInfoBytes());
        byte[] encrypted = rsa.EncryptText(combined, File.ReadAllBytes(FileManager.primaryPublicKeyPath));
        string encodedEncrypted = HttpUtility.UrlEncode(Convert.ToBase64String(encrypted));
        string url = $"{connectionLink}/license/UserLicenceDB?encrypted={encodedEncrypted}";
        Console.WriteLine("?Requesting User-Licence DB Check...");
        Logger.Log($"[Network] User:{username} deviceInfo:{deviceInfo.Info} DB checking request.");
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string encResult= await response.Content.ReadAsStringAsync();
                    byte[] result = rsa.DecryptResult(Convert.FromBase64String(encResult));

                    if (result.Length == 1 && result[0] == 1)
                    {
                        Console.WriteLine("+User-Licence checked.");
                        Logger.Log($"[Network] DB checking completed successfully.");
                        AESControl aes = new AESControl();
                        return await aes.OnlineAESCheck();
                    }
                    else if(result.Length == 1 && result[0] == 0) 
                    { 
                        Console.WriteLine($"-User:{username} not in DB or User dont have any licence.");
                        Logger.Log($"[Network] User:{username} not in DB or User dont have any licence.");
                        return false;
                    }
                    return false;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Logger.Log($"[Network] Error: { response.StatusCode}");
                    return false; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Requesting Error: {ex.Message}");
                return false;
            }
        }
    }
    
    static async Task<bool> setUpOnline()
    {
        try
        {
            Logger.Log("[Installation-Online] has started.");

            DeviceInfo deviceInfo = new DeviceInfo(); //Cihaz bilgilerini al
            RSAEncryption rsaEncryption = new RSAEncryption();

            //SendAdmin.dat dosyasını yazıyor.
            byte[] privateKey = rsaEncryption.GenerateAndSaveForAdmin(deviceInfo.getInfoBytes(), true);
            byte[] sendAdmin = rsaEncryption.sendAdmin;

            Logger.Log($"[Installation-Online] data has been created.");
            byte[] getLicence=  await SetUpOnlineRequest(sendAdmin);
            File.WriteAllBytes(FileManager.getLicencePath, getLicence);
            //getlicence webden gelecek.
             if (getLicence != null)
            {
                if (rsaEncryption.DecryptLicence(getLicence, privateKey, deviceInfo.getInfoBytes()))
                {
                    byte[] time = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o"));

                    (byte[] pubKey, byte[] privKey) = rsaEncryption.getKeys();
                    byte[] encData = rsaEncryption.EncryptText(time, pubKey);
                    byte[] encTimeDelta = rsaEncryption.EncryptText(Encoding.UTF8.GetBytes("0"), pubKey);
                    File.WriteAllBytes(FileManager.timePublicKeyPath, pubKey);
                    File.WriteAllBytes(FileManager.timePrivateKeyPath, privKey);
                    File.WriteAllBytes(FileManager.initialTimePath, encData);
                    File.WriteAllBytes(FileManager.setUpTime, encData);
                    File.WriteAllBytes(FileManager.timeDeltaPath, encTimeDelta);

                    Logger.Log("[Installation] Completed");
                    return true;
                }
                else
                {
                    Logger.Log($"[Installation] Decryption failed.");
                    return false;
                }
            }
            else
            {
                Logger.Log($"[Installation] Could not get file 'getLicence.dat'. {FileManager.getLicencePath} file not found");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
            return false;
        }

    }
    
    private static async Task<byte[]> SetUpOnlineRequest(byte[] sendAdmin)
    {
        
        RSAEncryption rsa = new RSAEncryption();
        string username = HttpUtility.UrlEncode(Environment.UserName);
        string encodedEncrypted = HttpUtility.UrlEncode(Convert.ToBase64String(sendAdmin));
        string url = $"https://localhost:7133/license/LicenceSetUp?username={username}&sendAdmin={encodedEncrypted}";
        Console.WriteLine("?Requesting online setup data control...");
        Logger.Log($"[Network] Requesting online setup data control..");
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string encResult = await response.Content.ReadAsStringAsync();
                   
                    return Convert.FromBase64String(encResult);
                   
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Logger.Log($"[Network] Error: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Requesting Error: {ex.Message}");
                return null;
            }
        }
    }
    static async Task backgroundLicenceCheck()
    {

        TimeValidator timeValidator = new TimeValidator();
        Random r = new Random();
        int periodSecond;

        while (!IsInternetAvailable())
        {
            periodSecond = r.Next(5, 10);
            Thread.Sleep(periodSecond * 1000);

            if (!appStatus)
            {
                if (!(timeValidator.timeValidation(periodSecond) && AESCheck()))
                {
                    if (pid.HasValue)
                    {
                        processKill(pid.Value);
                    }
                    int selfPId = Process.GetCurrentProcess().Id;
                    processKill(selfPId);
                }
                else
                {
                    Logger.Log("[Time Checked]");
                    Logger.Log("[Licence Checked]");
                    Console.WriteLine("Licence Checked.");
                }
            }
            else
            {
                if (!timeValidator.timeValidation(periodSecond))
                {
                    if (pid.HasValue)
                    {
                        processKill(pid.Value);
                    }
                    int selfPId = Process.GetCurrentProcess().Id;
                    processKill(selfPId);
                }
                else
                {
                    Logger.Log("[Time Checked]");
                    Console.WriteLine("Licence Checked.");
                }
            }
        }
        await backgroundLicenceCheckOnline();
    }
    static void processKill(int id)
    {
        try
        {
            // Hedef süreci al ve öldür
            Process targetProcess = Process.GetProcessById(id);
            targetProcess.Kill();

            Console.WriteLine("Process " + id + " başarıyla sonlandırıldı.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata: " + ex.Message);
        }
    }
    static async Task pipeLine()
    {

        Console.WriteLine("Lisans sunucusu başlatıldı, istemcilerden bağlantı bekleniyor...");

        Logger.Log("Licence App Started");
        // Birden fazla istemciyi aynı anda işleyebilmek için bir ThreadPool kullanıyoruz
        while (true)
        {
            var pipeServer = new NamedPipeServerStream("LicensePipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await pipeServer.WaitForConnectionAsync();

            _ = Task.Run(() => handleClient(pipeServer));
        }
    }
    static async Task handleClient(NamedPipeServerStream pipeServer)
    {
        int periodSecond;
        string request;
        bool results;

        try
        {
            using (var reader = new StreamReader(pipeServer))
            using (var writer = new StreamWriter(pipeServer) { AutoFlush = true })
            {
                string message = await reader.ReadLineAsync();
                Console.WriteLine($"İstemciden gelen istek: {message}");
                if (message != null)
                {

                    AESControl aes = new AESControl();
                    message = aes.DecryptStringFromBytes_Aes(message);

                    string[] messagelist = message.Split("-");
                    periodSecond = int.Parse(messagelist[0]);
                    pid = int.Parse(messagelist[1]);
                    request = messagelist[2];

                    Console.WriteLine("Request Type: " + request);

                    if (request == "checklicence")
                    {
                        appStatus = true;
                        if (IsInternetAvailable()) {
                            results = await UserCheckRequest();
                        }
                        else { 
                            results = AESCheck();
                        }
                        if (!results)
                        {
                            processKill(pid.Value);
                            Logger.Log($"[AES] control failed. App closing PID:{pid.Value}");
                        }
                        else
                        {
                            Logger.Log("[Licence Checked]");
                        }
                        await writer.WriteLineAsync(results.ToString());
                    }
                    else if (request == "start")
                    {
                        appStatus = true;
                        Logger.Log($"[Main App] started. PID:{pid.Value}");
                    }
                    else if (request == "end")
                    {
                        pid = null;
                        appStatus = false;
                        Logger.Log($"[Main App] has closed. PID:{pid.Value}");
                    }
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Bağlantı hatası: {ex.Message}");
        }
        finally
        {
            // Kaynakları serbest bırakıyoruz
            pipeServer.Dispose();
        }
    }
    static bool AESCheck()
    {
        DeviceInfo deviceInfo = new DeviceInfo();
        AESControl aes = new AESControl(deviceInfo.getInfoBytes(),"0");
        bool res = aes.ControlAES();
        return res;
    }
    static bool setUp()
    {
        try
        {
            Logger.Log("[Installation] has started.");

            DeviceInfo deviceInfo = new DeviceInfo(); //Cihaz bilgilerini al
            RSAEncryption rsaEncryption = new RSAEncryption();

            //SendAdmin.dat dosyasını yazıyor.
            byte[] privateKey = rsaEncryption.GenerateAndSaveForAdmin(deviceInfo.getInfoBytes(), false);


            Logger.Log($"[Installation] 'sendAdmin.dat' file has been created. {FileManager.sendAdminPath}");
            Console.WriteLine($"Send 'sendAdmin.dat' file from '{FileManager.sendAdminPath}' and wait for admins file\nWhen admin sends you the file, press enter.");
            Console.ReadLine();

            if (File.Exists(FileManager.getLicencePath))
            {

                byte[] getLicence = File.ReadAllBytes(FileManager.getLicencePath);

                if (rsaEncryption.DecryptLicence(getLicence, privateKey, deviceInfo.getInfoBytes()))
                {
                    byte[] time = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o"));

                    (byte[] pubKey, byte[] privKey) = rsaEncryption.getKeys();
                    byte[] encData = rsaEncryption.EncryptText(time, pubKey);
                    byte[] encTimeDelta = rsaEncryption.EncryptText(Encoding.UTF8.GetBytes("0"), pubKey);
                    File.WriteAllBytes(FileManager.timePublicKeyPath, pubKey);
                    File.WriteAllBytes(FileManager.timePrivateKeyPath, privKey);
                    File.WriteAllBytes(FileManager.initialTimePath, encData);
                    File.WriteAllBytes(FileManager.timeDeltaPath, encTimeDelta);

                    Logger.Log("[Installation] Completed");
                    return true;
                }
                else
                {
                    Logger.Log($"[Installation] Decryption failed.");
                    return false;
                }
            }
            else
            {
                Logger.Log($"[Installation] Could not get file 'getLicence.dat'. {FileManager.getLicencePath} file not found");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
            return false;
        }

    }
    
    static bool setUpControl()
    {
        if (File.Exists(FileManager.AESKeyIVPath) &&
            File.Exists(FileManager.AESTextPath) &&
            File.Exists(FileManager.initialTimePath) &&
            File.Exists(FileManager.LogFilePath))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
