# 07. Fayl yükləmə (şəkil və video)

Faylın yerləşdiyi yer: `Service/Services/FileService.cs`, `Service/Helpers/ImageFileRules.cs`, `Service/Helpers/VideoFileRules.cs`.

## 1. Nəzəriyyə

### 1.1 Niyə fayl yükləmə təhlükəlidir?
İstifadəçi nə göndərsə, ona etibar etmək olmaz:
- **Ad**: `../../Program.cs` kimi yol göndərib başqa yerə yaza bilər (**path traversal**).
- **Uzantı/tip**: `virus.exe`-ni `foto.png` adı ilə göndərə bilər. `Content-Type` başlığı da saxtalaşdırıla bilər.
- **Ölçü**: 10 GB fayl göndərib serveri doldura bilər.
- **Üzərinə yazmaq**: eyni adla başqa faylı əvəz edə bilər.

### 1.2 Müdafiə qaydaları (bizdə hamısı var)
1. **Adı özümüz veririk**: `Guid.NewGuid().ToString("N")` + uzantı. Müştərinin adı **heç vaxt** istifadə edilmir.
2. **Tipi faylın içindən təyin edirik** (magic bytes / fayl imzası): hər fayl formatının ilk baytları sabitdir:
   - PNG: `89 50 4E 47 0D 0A 1A 0A`
   - JPEG: `FF D8 FF`
   - WebP: `RIFF....WEBP` (ilk 4 bayt `RIFF`, 8–11-ci `WEBP`)
   - MP4: 5–8-ci baytlar `ftyp`
   - WebM: `1A 45 DF A3`
   Uzantı və `Content-Type` nəzərə alınmır.
3. **Ölçü limiti**: şəkil 2 MB, video 50 MB. Limit faylı yaddaşa oxumazdan **əvvəl** yoxlanılır.
4. **Qovluq məhdud**: yalnız `wwwroot/images/<qovluq>/` və `wwwroot/videos/<qovluq>/`.
5. **Silmə də məhdud**: yalnız bu sistemin təsadüfi adla yazdığı fayllar silinir.

### 1.3 `multipart/form-data`
Fayl göndərmək üçün HTTP gövdəsi bu formatda olmalıdır. `[FromBody]` (JSON) fayl qəbul etmir (`415 Unsupported Media Type`). API-də `[FromForm]` + `[Consumes("multipart/form-data")]`, MVC-də `MultipartFormDataContent`.

## 2. Bizim layihədə

### 2.1 `FileService`
```csharp
Task<string> UploadFileAsync(IFormFile file, string folder);   // şəkil
Task<string> UploadVideoAsync(IFormFile file, string folder);  // video
Task DeleteFileAsync(string webPath);
```
Qaytarılan dəyər **veb yolu**dur: `/images/brands/3f9a....png`. Bazada məhz bu yol saxlanır.

**Şəkil yükləmə (`UploadFileAsync`) addımları:**
1. Fayl boşdursa → `BadRequestException("Choose an image.")`.
2. `file.Length > 2 MB` → `BadRequestException("The image must be up to 2 MB.")`.
3. Fayl yaddaşa (`MemoryStream`) oxunur (2 MB-dır, problem yoxdur).
4. `ImageFileRules.DetectExtension(bytes)`: imzaya görə `.png`, `.jpg` və ya `.webp`; yoxdursa `BadRequestException("Only PNG, JPEG or WebP images are allowed.")`.
5. Qovluq yaradılır (`Directory.CreateDirectory`), fayl `Guid.N + uzantı` adı ilə yazılır.

**Video yükləmə (`UploadVideoAsync`):**
- Video böyükdür, **tam yaddaşa oxunmur**. Yalnız ilk 12 bayt oxunur (imza üçün), qalanı axınla (`CopyToAsync`) birbaşa fayla yazılır.
- Yazma yarımçıq qalsa (xəta) qismən fayl **silinir** (`try/catch`).

**Silmə (`DeleteFileAsync`)**: yalnız bu regex-ə uyğun yollar silinir:
```
^/(images|videos)/<qovluq>/<32 hex simvol>.(png|jpg|webp|mp4|webm)$
```
Nəticə:
- Başlanğıc fayllar (`regular.png`, `homeVideo.mp4`, `logo.png`) **heç vaxt silinmir**: adları 32-hex təsadüfi deyil.
- Yolu manipulyasiya etmək cəhdləri (`../`) uyğun gəlmir.
- Əlavə olaraq `Path.GetFullPath` ilə yol `wwwroot`-dan çıxmır yoxlanılır.

### 2.2 Şəkillərin istifadə olunduğu yerlər
| Qovluq | Nə üçün |
|---|---|
| `brands` | Brend loqoları |
| `cards` | Kart dizaynları |
| `settings` | Sayt loqosu |
| `services` | Service ikonları |
| `pillars` (About sütunları) | About şəkilləri |
| `videos/about` | About videosu |

### 2.3 Şəkilləri kim göstərir?
Şəkillər **API-nin** `wwwroot`-undadır (`UseStaticFiles`). MVC-də göstərmək üçün tam ünvan lazımdır:
- Bazada: `/images/cards/gold.png`.
- MVC: `ApiUrl.ToAbsolute("https://localhost:7293/", "/images/cards/gold.png")` → `https://localhost:7293/images/cards/gold.png`.

### 2.4 Servis tərəfindəki "yetim fayl" qaydaları
(Bax `04-service-layer.md`.)
- Yaratma: əvvəl fayl yüklə, sonra baza; baza uğursuz olsa faylı sil.
- Yeniləmə: yeni faylı yüklə, baza uğurlu olsa köhnəni sil; köhnənin yolu həmişə **bazadan** alınır.
- Silmə: əvvəl baza, sonra fayl.

## Özünü yoxla

1. Path traversal nədir? Bizim sistem ondan necə qorunur?
2. Fayl tipi niyə uzantıya yox, faylın içindəki imzaya görə təyin olunur?
3. Video niyə tam yaddaşa oxunmur?
4. `DeleteFileAsync` niyə `regular.png` kimi başlanğıc faylları silmir?
5. Niyə `[FromBody]` ilə fayl göndərmək olmur?
6. Brand yenilənəndə şəkil dəyişirsə, köhnə şəkil nə vaxt silinir?
