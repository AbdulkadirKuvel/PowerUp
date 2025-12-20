# 🏋️ PowerUp Fitness - Spor Salonu Yönetim Sistemi

> **Sakarya Üniversitesi Bilgisayar Mühendisliği** > **Web Programlama Dersi Proje Ödevi (2025)**

Bu proje, ASP.NET Core MVC mimarisi kullanılarak geliştirilmiş kapsamlı bir spor salonu ve üye yönetim sistemidir. Kullanıcıların spor salonlarını inceleyebileceği, hizmetleri görebileceği; yöneticilerin ise tüm içerikleri dinamik olarak yönetebileceği bir platform sunar.

---

## 👨‍🎓 Öğrenci Bilgileri

| Alan | Bilgi |
| :--- | :--- |
| **Adı Soyadı** | Abdulkadir Kuvel |
| **Öğrenci No** | B221210002 |
| **Bölüm** | Bilgisayar Mühendisliği |
| **Ders** | Web Programlama (6. Yarıyıl) |
| **Şube** | 1. Öğretim C Grubu |

---

## 🚀 Proje Özellikleri

### 🎨 Arayüz ve UX
* **Modern Responsive Tasarım:** Bootstrap 5 ile her cihazda uyumlu görünüm.
* **Gelişmiş Sidebar:** Masaüstünde genişletilebilir/daraltılabilir, mobilde off-canvas çalışan dinamik yan menü.
* **Dark/Light Mode:** Kullanıcı tercihine göre veya sistem ayarlarına duyarlı tema desteği.
* **Animasyonlar:** Sayfa geçişleri ve kartlar için yumuşak animasyonlar.

### ⚙️ Backend ve Fonksiyonlar
* **ASP.NET Core Identity:** Güvenli üyelik sistemi (Giriş, Kayıt, Rol Yönetimi).
* **Admin Paneli:** Yetkili kullanıcılar için içerik yönetimi.
* **CRUD İşlemleri:** Spor salonu, antrenör ve hizmet ekleme/silme/güncelleme özellikleri.
* **Veritabanı Seeding:** Proje ilk çalıştığında otomatik admin oluşturma.

---

## 🛠 Kullanılan Teknolojiler

* **Platform:** .NET 8.0 (ASP.NET Core MVC)
* **Veritabanı:** MS SQL Server / Entity Framework Core (Code First)
* **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5, jQuery
* **İkon Seti:** FontAwesome / Bootstrap Icons

---

## 💻 Kurulum ve Çalıştırma

Projeyi yerel makinenizde çalıştırmak için aşağıdaki adımları izleyin:

1.  **Projeyi İndirin:**
    Proje dosyalarını klasöre çıkartın.

2.  **Veritabanını Güncelleyin:**
    `appsettings.json` dosyasındaki ConnectionString'in (LocalDB veya SQL Server) bilgisayarınıza uygun olduğundan emin olun. Ardından Package Manager Console üzerinden şu komutu çalıştırın:
    ```powershell
    Update-Database
    ```

3.  **Projeyi Başlatın:**
    Visual Studio üzerinden `IIS Express` veya `http` profili ile projeyi çalıştırın.

---

## 🔐 Giriş Bilgileri (Admin)

Veritabanı oluşturulduğunda otomatik olarak tanımlanan yetkili hesap bilgileri:

* **Email:** `B221210002@sakarya.edu.tr`
* **Şifre:** `sau`

---

## 📂 Proje Yapısı

* `/Controllers`: Sayfa yönlendirmeleri ve iş mantığı.
* `/Models`: Veritabanı tabloları ve View modelleri.
* `/Views`: Kullanıcı arayüzü dosyaları (.cshtml).
* `/wwwroot`: CSS, JS ve Resim dosyaları.