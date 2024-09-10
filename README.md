# Licensing-with-RSA-AES

## Proje Açıklaması
LicenceApp, çeşitli yazılımların lisanslarını güvenli bir şekilde kurmak ve kontrol etmek amacıyla geliştirilmiş bir .NET Core projesidir. AES ve RSA şifrelemeleri kullanarak lisans verilerini korur ve her cihazın benzersiz bir lisansa sahip olmasını sağlar. Cihaz sistem zamanını düzenli aralıklarla kontrol ederek sistemde yaşanabilecek olası zaman manipülasyonlarını önler.

## Özellikler
- Cihaz bilgilerini ve lisansı yönetmek için AES şifreleme.
- Lisans kurulumu ve Web server ile iletişiminde anahtar yönetimi için RSA şifreleme.
- Benzersiz lisans oluşturma için cihaz bilgisi toplama.
- Lisans verilerini saklamak için dosya yönetimi.
- Hataları, uyarıları ve bilgi mesajlarını izlemek için log sistemi.
- Lisans süresinin geçerliliğini kontrol etmek için zamanlayıcı.
- Windows ve Linux desteği.
- x86/x64 ve ARM mimarisi desteği.

## Kurulum

### Gereksinimler
- .NET Core SDK 8.0 veya üzeri
- Windows ve Linux sistemleriyle uyumlu
- ARM ve x86 mimarisi desteği

### Kurulum Adımları:
1. Depoyu klonlayın:
   ```bash
   git clone https://github.com/FurkanLiman/Licensing-with-RSA-AES.git
   cd LicenceApp
   ```

2. Gerekli paketleri yükleyin:
   ```bash
   dotnet restore
   ```

3. Projeyi derleyin:
   ```bash
   dotnet build
   ```

4. Projeyi çalıştırın:
   ```bash
   dotnet run
   ```

## Kullanım
LicenceApp, arka planda otomatik olarak lisans kontrolleri yapar, lisans verilerini şifreler ve çözer, ayrıca oluşabilecek sorunları loglar. Temel kullanım adımları:
1. Kurulumda RSA'yı başlatır ve Web servisi ile lisans ve key paylaşımları yapılır.
2. Zaman bilgileri tutarak manipülasyonları önler.
3. CPU Kimliği, anakart kimliği ve BIOS kimliği gibi cihaz bilgilerini alır ve AES şifrelemesi ile saklar.
4. Düzenli aralıklarla saklanan lisans verilerine karşı lisans doğrulaması yapar.
5. Sonuçları analiz için loglar.

## Desteklenen Platformlar ve Sürümler
- Windows 10, 11
- Linux (Ubuntu, Debian, vb.)
- .NET Core SDK 8.0 veya üzeri
- ARM ve x86/x64 mimarileri destekler

## Paket Gereksinimleri
Projede kullanılan NuGet paketleri:
- `Newtonsoft.Json`: JSON verilerini serileştirmek ve deserialize etmek için.
- `System.Diagnostics.EventLog`: Olay günlüğüne log yazmak ve okumak için.
- `System.Management`: Sistem yönetimi ve donanım bilgisi almak için.
- `System.Security.Cryptography.ProtectedData`: Şifreleme işlemleri ve verilerin korunması için.

Paketleri yüklemek için:
```bash
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet add package System.Diagnostics.EventLog --version 8.0.0
dotnet add package System.Management --version 8.0.0
dotnet add package System.Security.Cryptography.ProtectedData --version 8.0.0



# WebServer


Bu depo, **ASP.NET MVC** kullanılarak geliştirilmiş bir **Lisans Kontrol Servisi** içermektedir. API linkleri ile lisans kullanıcı ve lisans kontrolleri gerçekleştirir. Admin panel ile kullanıcı ekleme, lisans planı ekleme, kullanıcıya lisans kurulumu gibi hizmetler sunar.

## Proje Yapısı

- **Controllers**: Gelen istekleri işleyen API ve web denetleyicilerini içerir.
- **Models**: Lisans ve ilgili varlıkları temsil eden veri modelleri.
- **Views**: Web arayüzü için Razor sayfaları.
- **wwwroot**: Web uygulaması için statik dosyalar.

## Özellikler

- API tabanlı lisans doğrulaması (API anahtarı ile).
- Lisansları yönetmek için basit bir web arayüzü (ekleme, görüntüleme, silme).
- Güvenli iletişim ve lisans yönetimi için **RSA** ve **AES** şifreleme kullanır.
- Çevrimiçi ve çevrimdışı lisans doğrulama desteği.

## Kurulum ve Yapılandırma

### Gerekli Yazılımlar

- [.NET Core SDK](https://dotnet.microsoft.com/download)
- `appsettings.json` dosyasında yapılandırılmış bir veritabanı sistemi (ör. SQL Server, MySQL).

### Adımlar

1. **Projeyi klonlayın**:
   ```bash
   git clone https://github.com/FurkanLiman/Licensing-with-RSA-AES.git
   cd license-control-service
   ```

2. **Gerekli paketleri yükleyin**:
   ```bash
   dotnet restore
   ```

3. **Veritabanı ayarları**:
   - `appsettings.json` dosyasındaki veritabanı bağlantı dizesini güncelleyin.
   - Gerekli tabloları oluşturmak için Entity Framework migration işlemini çalıştırın:
     ```bash
     dotnet ef database update
     ```

4. **Uygulamayı çalıştırın**:
   ```bash
   dotnet run
   ```

## API Belgeleri

### Lisans Doğrulama

`POST /api/license/UserLicenceDB`

- **Parametreler**:
  - `encrypted` (string): kullanıcı bilgisi ve cihaz bilgisi içeren RSA şifreli metin.
- **Yanıt**: Tüm yanıtlar RSA şifreli olarak gitmekte ve lisans içerisindeki publicKey ile şifrelenmektedir.
  - `{[1]}`: kullanıcı ve lisans bulundu ve lisans tarihi henüz geçmemiş.
  - `{[0]}`: kullanıcı, lisans tespiti veya lisans tarihinde sorun.


`POST /api/license/LicenceAktivationCheck`

- **Parametreler**:
  - `state` (string): serviste yapılan son kontrolün online veye offline durumunu içeren veri.
  - `encrypted` (string): kullanıcı bilgisi ve cihaz bilgisi içeren RSA şifreli metin.
- **Yanıt**: Tüm yanıtlar RSA şifreli olarak gitmekte ve lisans içerisindeki publicKey ile şifrelenmektedir.
  - `key,iv`: lisans kontrolü yapar ve yeni key, iv değerleri döndürür.
  - `{[1]}`: servisteki aes key, iv değerlerini alır ve senkronizasyon yapar.
  - `{[0]}`: kullanıcı, lisans tespiti veya lisans aktivasyon sorunu.


`POST /api/license/LicenceSetUp`

- **Parametreler**:
  - `username` (string): cihazdan gelen kullanıcı adı.
  - `sendAdmin` (string): cihaz bilgisi ve cihaz public keyi içeren RSA şifreli metin.
- **Yanıt**: Tüm yanıtlar RSA şifreli olarak gitmekte ve lisans içerisindeki publicKey ile şifrelenmektedir.
  - `cryptedDeviceInfo,key,iv`: kurulumu sonucunda karşı tarafın akreditasyonu için cihaz bilgileri key, iv değerlerini döndürür.


`POST /api/license/UploadFile`

- **Parametreler**:
  - `fileUpload` (IFormFile): admin panelinden yüklenen 'sendAdmin.dat' dosyası.
  - `username` (string): kullanıcı adı.
- **Yanıt**: Tüm yanıtlar RSA şifreli olarak gitmekte ve lisans içerisindeki publicKey ile şifrelenmektedir.
  - `getLicence.dat`: kurulumu sonucunda adminin karşı tarafa iletmesi için oluşturulan dosya.


`POST /api/license/GetTime`

  - **Yanıt**: Sunucu saatini döndürür.
  - `utc_time`: Servisin online saat kontrolü için sunucu saatini döndürür.


`POST /api/license/CreateNewPrimaryKeys`

  - **Yanıt**: SQL veritabanına yeni Primary Keyler ekler.



## Yapılandırma

### `appsettings.json`

Bu yapılandırma dosyası, veritabanı bağlantıları, API anahtarları ve ortam spesifik seçenekler gibi ayarları içerir.

### Örnek Yapılandırma:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=your-db;User Id=your-username;Password=your-password;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Sürüm Desteği

- .NET Core 3.1+
- .NET 5.0+
- .NET 6.0+


![SQL Table](Software/Design/DB/sql.jpg)
