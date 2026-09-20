# MiniEcommerce — Full-Stack E-Commerce API & Storefront

Modern yazılım geliştirme pratiklerine uygun olarak geliştirilmiş; **ASP.NET Core 8 Web API** backend ve **Vite + React + TypeScript** frontend mimarisine sahip uçtan uca e-ticaret uygulaması.

---

## Mimari ve Öne Çıkan Özellikler

### Backend (.NET 8 Web API)

- **Katmanlı Mimari:** Controller, Service, Repository ve Data Transfer Object (DTO) katman ayrımı.
- **Güvenlik & Yetkilendirme:** JWT (JSON Web Token) tabanlı Bearer kimlik doğrulama.
- **Veritabanı & Concurrency:** Entity Framework Core ve SQLite altyapısı; sipariş tamamlama aşamasında veri tutarlılığını garanti altına alan **atomik veritabanı transaction yönetimi** ve stok kontrolü.
- **Merkezi Hata Yönetimi:** Standartlaştırılmış API hata yanıtları için Exception Handling Middleware.
- **API Dokümantasyonu:** Swagger / OpenAPI entegrasyonu.

### Frontend (React + TypeScript + Vite)

- **Katalog & Arama:** Kategori filtreleme sekmeleri, anlık ürün/açıklama araması ve fiyata göre sıralama opsiyonları.
- **Sepet & Stok Kontrolü:** İstemci tarafında dinamik adet kontrolleri, stok sınırı doğrulamaları ve dinamik kargo barı.
- **Kupon Yönetimi:** Kupon kodu (`INDIRIM10`) ile anlık sepet indirimi uygulama.
- **Sipariş Geçmişi:** Kullanıcı oturumuna bağlı geçmiş sipariş kayıtlarını listeleme.

---

## Proje Dizini

```text
MiniEcommerce/
├── src/
│   └── MiniEcommerce.Api/     # ASP.NET Core 8 Web API katmanı
│       ├── Controllers/       # API Controller sınıfları
│       ├── Services/          # İş mantığı ve servis katmanı
│       ├── Data/              # EF Core AppDbContext ve Seeder
│       ├── Models/            # Domain modelleri
│       └── DTOs/              # Veri transfer nesneleri
├── client/                    # Vite + React + TypeScript frontend uygulaması
│   ├── src/
│   │   ├── services/          # Axios tabanlı API istemcisi
│   │   └── App.tsx            # Vitrin, sepet ve sipariş bileşeni
└── MiniEcommerce.sln          # Solution dosyası
```

1. Backend (API)

# Bağımlılıkları yükleyin ve derleyin

dotnet restore
dotnet build

# API servisini başlatın (Varsayılan port: 5080)

dotnet run --project src/MiniEcommerce.Api

Swagger dokümantasyonuna http://localhost:5080/swagger üzerinden erişebilirsiniz.

2. Frontend (Client)
   cd client

# Paketleri yükleyin

npm install

# Geliştirme sunucusunu başlatın (Varsayılan port: 5173)

npm run dev

Demo Kullanıcı Bilgileri
Uygulamayı arayüz üzerinden veya Swagger ile doğrudan test etmek için tanımlı kullanıcı:

E-posta: arda@toker.local

Şifre: arda123

Örnek Kupon Kodu: INDIRIM10 (%10 İndirim)
