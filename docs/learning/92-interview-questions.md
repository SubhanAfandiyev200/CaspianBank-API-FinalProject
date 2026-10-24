# 92. Müsahibə və müdafiə sualları

Hər sualın altında **cavab planı** var (tam mətn deyil, əsas nöqtələr). Cavabı öz sözünlə, 1–2 dəqiqəyə danışmağa çalış. Əlavə olaraq hər sualın yanında hansı fayla baxmaq lazım olduğu yazılıb.

> Dürüstlük qaydası: layihədə AI vasitəsindən istifadə edildiyini soruşsalar, düzünü de: "AI ilə işlədim, kodu oxuyub yoxlayıram və hər hissəsini izah edə bilərəm və dəyişə bilərəm." Sonra canlı tapşırığa hazır ol (məs. "bu endpoint-ə filtr əlavə et"). Bunun üçün `91-rebuild-roadmap.md`-dəki mərhələləri özün yaz.

## A. Layihə və arxitektura
**1. Layihəni 1 dəqiqəyə izah et.** (`01`)
- Onlayn bank simulyasiyası: qeydiyyat (OTP), kartlar, köçürmə, tarixçə, admin panel.
- İki layihə: API (qaydalar, baza) + MVC (səhifələr).
- N-Tier: Domain, Repository, Service, API.

**2. Niyə API və MVC ayrıdır?** (`01`)
- Qaydalar bir yerdə; başqa müştəri (mobil) də istifadə edə bilər; MVC bazaya toxunmur.

**3. N-Tier nədir, qatların rolu?** (`01`, `03`, `04`)
- Domain: entity; Repository: baza; Service: qaydalar; API: HTTP. Asılılıq yalnız aşağı.

**4. Repository nə üçündür?** (`03`)
- Bazaya çıxışı bir yerdə toplayır; Service `DbContext` bilmir; dəyişmək və test asan.

**5. DTO və Entity fərqi?** (`04`)
- Entity: baza forması; DTO: API-nin məlumat forması; daxili sahələr sızmasın.

**6. DI nədir, `AddScoped` nə edir?** (`03`)
- Asılılıqları konteyner verir; Scoped: hər sorğuya bir nüsxə.

**7. Middleware nədir, sıra niyə vacibdir?** (`06`)
- Sorğu zənciri. ExceptionHandling birinci ki, hamısını tutsun; Authentication Authorization-dan əvvəl.

## B. Verilənlər bazası və EF Core
**8. EF Core nədir, migration nədir?** (`02`)
**9. Pul üçün hansı tip və niyə?** `decimal(18,2)`, `double` yuvarlaqlaşdırma xətası verir.
**10. Unikal indeks nə üçündür? Hansılar var?** Kart nömrəsi, `(Reference, Type)`, `Setting.Key`, `UX_Cards_UserId_Cashback`, FİN.
**11. RowVersion nədir?** (`02`, `10`) Paralel dəyişikliyi aşkar edir.
**12. Tracking və AsNoTracking?** (`03`)
**13. N+1 problemi nədir?** Siyahıda hər sətir üçün ayrıca sorğu; `Include` ilə həll. (`GetPageForUserAsync`-də `Include(Card)` var.)
**14. Tranzaksiya nədir, harada istifadə etdiniz?** Köçürmə, ödənişli kart.

## C. Təhlükəsizlik və giriş
**15. JWT necə işləyir?** (`05`) Header.Payload.Signature; imza açarla; stateless; claim-lər.
**16. Parol necə saxlanır?** PBKDF2 hash + salt (Identity); düz saxlanmır.
**17. Hash və şifrələmə fərqi?** Hash geri çevrilmir.
**18. OTP-ni necə qorudunuz?** Hash (HMAC), ömür 5 dəq, 5 cəhd, 60 san. gözləmə, FixedTimeEquals, təsdiq tokeni bir dəfəlik.
**19. Brute-force-dan necə qorunursunuz?** Rate limiting (10/dəq), lockout (5 yanlış → 5 dəq), eyni xəta mesajı.
**20. CSRF/XSS nədir və necə qorunursunuz?** (`13`) AntiForgery token + SameSite; HttpOnly cookie.
**21. JWT niyə cookie içindədir?** (`13`) JS oxuya bilmir (XSS).
**22. User enumeration nədir?** (`05`) Eyni mesaj + rate limit.
**23. Rol sistemi necə işləyir?** (`05`, `14`) 6 rol (Customer + 5 işçi rolu; WebDesigner silinib); `[Authorize(Roles)]`; MVC və API ikisində.
**24. Fayl yükləməni necə qoruyursunuz?** (`07`) Magic bytes, təsadüfi ad, ölçü limiti, təhlükəsiz silmə.
**25. UserId niyə tokendən götürülür?** (`06`) Başqasının adından əməliyyatın qarşısı.

## D. Pul məntiqi (ən vacib)
**26. Köçürməni izah et.** (`10`) Preview → Transfer; ResolveAsync; ExecuteAsync; tranzaksiya; 3 jurnal sətri.
**27. Komissiya necə hesablanır?** Limitdən aşan hissə × faiz; göndərən kartın tier-i; 2 onluğa AwayFromZero.
**28. İki nəfər eyni anda pul göndərsə?** RowVersion + retry (8 cəhd).
**29. Düyməni iki dəfə klikləsə pul iki dəfə köçərmi?** Xeyr: RequestId → Reference → unikal indeks + mövcud qəbzin qaytarılması.
**30. Niyə servis də, repository də yoxlayır?** TOCTOU.
**31. Cashback qaydaları?** Ona köçürmə yoxdur; ondan yalnız öz kartlarına.
**32. Balans necə izah edilir?** Jurnal + `BalanceAfter`; çıxarışda açılış + giriş − çıxış = bağlanış.

## E. API və MVC
**33. Xəta idarəetməsi necədir?** (`04`, `06`) Exception + middleware; ServiceResult; vahid JSON format.
**34. MVC API ilə necə danışır?** (`12`) Adlandırılmış HttpClient; BearerTokenHandler; ApiControllerBase.
**35. PRG nədir?** (`12`)
**36. ViewComponent nədir?** (`12`) Home bölmələri.
**37. Area nədir?** (`14`)
**38. API işləmirsə Home nə edir?** Hər ViewComponent `HttpRequestException` tutur; boş bölmə.
**39. Server tərəfdə səhifələmə necə işləyir?** (`11`) Skip/Take; count/sum ayrı sorğu; son səhifəyə çəkmə.
**40. Vaxt zonalarını necə idarə etdiniz?** (`11`) UTC saxla; Bakı +4 ilə gün sərhədləri.

## F. Praktik tapşırıqlar (canlı kodlaşdırma üçün məşq)
Layihəni açmadan özün yazmağa çalış:
1. Brand kimi yeni `Testimonial` modeli (5 addım).
2. `GET api/cards/summary`: istifadəçinin kartlarının cəm balansını qaytar.
3. Tarixçəyə `minAmount`/`maxAmount` filtri əlavə et (validator + repository + MVC).
4. Köçürmədə gündəlik limit əlavə et (bu gün çıxan cəm ≤ X).
5. `Card` üçün `Nickname` sahəsi (migration, DTO, MVC).
6. Rate limit siyasəti dəyişdir, test et.
7. Servisdə bir bug tap (məs. komissiya `>=` əvəzinə `>`) və unit test yaz.

## G. "Bu layihədə nəyi fərqli edərdin?" (düşünməyə sual)
- Real SMTP və gizli açarların `appsettings`-də olmaması (secret manager).
- Refresh token və JWT ləğvi.
- Audit log və dəyişməz jurnal.
- Cashback və kredit məntiqinin tamamlanması.
- Unit və integration testlər.
- Caching (Home məzmunu üçün).
- Docker.
