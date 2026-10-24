# 94. Reyestr toxum datası: QARALAMA (hələ koda yazılmayıb)

> Bu fayl sənin baxıb dəyişməyin üçündür. Hər şey uydurmadır (ad, FİN, vəsiqə, VÖEN, bank adları). Təsdiqdən sonra seed koduna çevriləcək.
> Tarixlərin "bu gün"ü: **2026-10-12**. Aylıq ödəniş rəqəmləri təxminidir, sistem özü hesablayacaq.

## Əsas qayda
- Reyestrdə **email və telefon yoxdur**. Yalnız dövlət datası var.
- Qeydiyyatsız vətəndaşla qeydiyyatdan keçən adam **öz real emailini və telefonunu** yazır (OTP öz poçtuna gəlir). Reyestr yalnız "bu FİN + vəsiqə nömrəsi doğrudan var və uyğundur" deyir.
- Qeydiyyatlı 6 müştərinin email-i `SENINGMAIL+etiket@gmail.com` formasında olur. Gmail `+etiket`-i atır və hamısı sənin poçtuna düşür, yəni OTP (kart bloku və s.) onlarla da işləyir. Parollar `appsettings.Local.json`-da, repoda yox.

---

## A. Vətəndaşlar (36)
`Customer` cədvəli. Ata adı: atanın adı. Qeydiyyatlılar (**R**) 1–6.

| # | FİN | Vəsiqə | Ad Soyad | Ata adı | Ana adı | Doğum | Doğum yeri | Ünvan | Status |
|---|---|---|---|---|---|---|---|---|---|
| 1 R | 5KM2R9A | AZE10482931 | Aysel Məmmədova | Elçin | Sevinc | 1992-03-14 | Bakı | Bakı, Nərimanov r., Əhmədli küç. 14/8 | təmiz |
| 2 R | 7XQ4D1B | AZE10573402 | Elçin Hüseynov | Rəşid | Zəminə | 1988-07-02 | Sumqayıt | Sumqayıt, 9-cu mkr., 21/45 | təmiz |
| 3 R | 3HT8V6C | AZE10691157 | Nigar Əliyeva | Vüqar | Gülnarə | 1995-11-23 | Gəncə | Bakı, Yasamal r., H. Əliyev küç. 7/31 | təmiz |
| 4 R | 9PL2N5D | AZE10735620 | Rauf Quliyev | Tahir | Aynur | 1990-01-30 | Lənkəran | Bakı, Binəqədi r., 8-ci mkr. 3/12 | təmiz |
| 5 R | 1BW7Z4E | AZE10828446 | Leyla Cəfərova | Nadir | Şəfiqə | 1999-05-09 | Şəki | Bakı, Xətai r., Z. Əliyeva küç. 22/5 | təmiz |
| 6 R | 6FG3J8H | AZE10914073 | Tural Rzayev | Akif | Mehriban | 1985-12-18 | Mingəçevir | Mingəçevir, H. Əliyev pr. 44/9 | təmiz |
| 7 | 4CD9S2K | AZE11027589 | Günel Həsənova | Fuad | Rəna | 1993-02-27 | Bakı | Bakı, Səbail r., Rəsul Rza küç. 5/17 | təmiz |
| 8 | 8RY5T1L | AZE11136204 | Orxan Nəbiyev | Ramiz | Samirə | 1991-08-11 | Quba | Bakı, Suraxanı r., Qaraçuxur 10/3 | təmiz |
| 9 | 2NA6Q7M | AZE11249871 | Səbinə Vəliyeva | Azər | Lalə | 1997-04-04 | Şirvan | Şirvan, H. Əliyev pr. 12/6 | təmiz |
| 10 | AJ4X8W3 | AZE11358026 | Kamran İsmayılov | Qasım | Südabə | 1983-09-19 | Gəncə | Gəncə, Kəpəz r., Atatürk pr. 80/14 | təmiz |
| 11 | B7E2U5P | AZE11463715 | Ülviyyə Hacıyeva | Faiq | Təranə | 1994-06-21 | Bakı | Bakı, Yasamal r., Şərifzadə küç. 9/22 | təmiz |
| 12 | C3S9K6V | AZE11572948 | Vüqar Səfərov | Kamal | Gülşən | 1989-10-05 | Zaqatala | Bakı, Nəsimi r., 28 May küç. 31/10 | təmiz |
| 13 | D8M1H4Y | AZE11681350 | Zəhra Qasımova | Eldar | Ülkər | 2000-01-17 | Naxçıvan | Bakı, Nərimanov r., Təbriz küç. 15/4 | təmiz |
| 14 | E5V7L2Z | AZE11794602 | Rəşad Əhmədov | Mübariz | Nərgiz | 1986-03-08 | Bakı | Bakı, Xəzər r., Şüvəlan 17 | təmiz |
| 15 | F2T4B9N | AZE11803179 | Fidan Bağırova | Nizami | Sevil | 1996-09-30 | Şamaxı | Bakı, Binəqədi r., Biləcəri 5/2 | təmiz |
| 16 | G6Q8C3R | AZE11916544 | Samir Abbasov | Etibar | Rafiqə | 1992-12-25 | Bərdə | Bərdə, Cavad xan küç. 3 | təmiz |
| 17 | H9Z1D5S | AZE12025887 | Məryəm Nəsirova | Hikmət | Arzu | 1990-07-14 | Bakı | Bakı, Səbail r., Neftçilər pr. 60/33 | təmiz |
| 18 | J4P3F7T | AZE12134093 | Cavid Mustafayev | Zaur | Elnarə | 1987-05-22 | Sumqayıt | Sumqayıt, Sülh küç. 18/60 | təmiz |
| 19 | K1U6G8X | AZE12247618 | Nurlan Rüstəmov | Tofiq | Fəridə | 1993-11-03 | Lənkəran | Bakı, Xətai r., H. Aslanov küç. 14/7 | təmiz |
| 20 | L7W2A4Q | AZE12359720 | Aygün Kərimova | Nüsrət | Kəmalə | 1998-02-16 | Qəbələ | Qəbələ, Nizami küç. 27 | təmiz |
| 21 | M3Y5E9B | AZE12468135 | Elvin Məhərrəmov | Cəlal | Ramilə | 1984-08-28 | Bakı | Bakı, Nəsimi r., Ü. Hacıbəyov küç. 9/12 | təmiz |
| 22 | N8X1R6C | AZE12571469 | Sevinc Talıbova | Anar | Zöhrə | 1996-01-12 | Gəncə | Gəncə, Kəpəz r., Cavad xan küç. 33/5 | təmiz |
| 23 | P2H7K3D | AZE12683902 | Ramin Quliyev | Yusif | Zemfira | 1988-04-06 | Bakı | Bakı, Suraxanı r., Hövsan 8/1 | **gecikmələr** (xarici kredit) |
| 24 | Q6D4M8F | AZE12796254 | Gülnar Əsgərova | Maqsud | Aliyə | 1991-06-19 | Mingəçevir | Mingəçevir, Nizami küç. 10/2 | **gecikmələr** |
| 25 | R9J2V5G | AZE12804371 | Tahir Nağıyev | Ədalət | Mətanət | 1985-02-09 | Şəki | Şəki, M. F. Axundov küç. 6 | **gecikmələr** (çox) |
| 26 | S4L8N1H | AZE12917865 | Ilahə Mikayılova | Sabir | Gültəkin | 1994-10-27 | Bakı | Bakı, Yasamal r., Şəhriyar küç. 4/9 | 1 kiçik gecikmə |
| 27 | T1C3Z7J | AZE13026498 | Anar Şahbazov | Teymur | Şövkət | 1982-12-01 | Bakı | Bakı, Sabunçu r., Bakıxanov 21/6 | **QARA SİYAHI**: adam öldürmə, hökm qüvvədədir |
| 28 | U5B9P2K | AZE13139520 | Vüsal Dadaşov | Rövşən | Mina | 1990-03-15 | Gəncə | Gəncə, Nizami r., Heydər Əliyev pr. 5/3 | **QARA SİYAHI**: bank soyğunçuluğu |
| 29 | V8F6W4L | AZE13248073 | Kənan Orucov | Samir | Aytən | 1987-09-08 | Bakı | Bakı, Nəsimi r., Rəşid Behbudov 12/9 | **QARA SİYAHI**: saxta sənədlərlə kredit dələduzluğu |
| 30 | W3A1S9M | AZE13351786 | Ağaəli Mirzəyev | Hüseyn | Xədicə | 1955-05-05 | Quba | Quba, Heydər Əliyev küç. 40 | **VƏFAT**: 2025-11-03 |
| 31 | X7G5Y2N | AZE13467209 | Zeynəb Hüseynli | Ədil | Firəngiz | 1949-08-08 | Şəki | Şəki, Gənclik küç. 8/2 | **VƏFAT**: 2026-02-17 |
| 32 | Y2K8T6P | AZE13578312 | Murad Əliyev | Ceyhun | Nərmin | 2010-06-12 | Bakı | Bakı, Nərimanov r., Atatürk pr. 3/5 | təmiz, **16 yaş** (kredit yox) |
| 33 | Z6R4Q1R | AZE13684957 | Qəşəm Ağayev | Mahir | Gövhər | 1958-04-10 | Gəncə | Gəncə, Kəpəz r., Təbriz küç. 11/1 | təmiz, **təqaüdçü** (iş yoxdur) |
| 34 | 1D9H3X5 | AZE13792640 | Pərvanə Ələsgərova | Elman | Sədaqət | 2001-07-24 | Bakı | Bakı, Nizami r., Q. Qarayev pr. 29/14 | təmiz, **işsiz** |
| 35 | 2E7M6C8 | AZE13805163 | İsmayıl Babayev | Rafael | Nailə | 1989-01-26 | Bakı | Bakı, Xətai r., Əhmədli 22/3 | təmiz, **iki iş yeri** (köhnə, yeni) |
| 36 | 3J5V2B4 | AZE13914728 | Xədicə Orucova | Zakir | Pakizə | 1992-09-02 | Lənkəran | Lənkəran, Seyid Əzim küç. 15/2 | **məhkəmədə xarici kredit**, amma qara siyahıda DEYİL |

Qeyd: 27, 28, 29 qeydiyyatdan keçə bilir, amma **kredit** müraciəti rədd olunur. 30, 31 (vəfat) qeydiyyat edə bilməz. 32 qeydiyyat edə bilər, kredit ala bilməz (18 yaşdan kiçik).

---

## B. İş yerləri (13)
| # | Ad | VÖEN | Sektor | Ünvan |
|---|---|---|---|---|
| W1 | Xəzər Logistika MMC | 1400123451 | Nəqliyyat | Bakı, Suraxanı r. |
| W2 | Odlar IT Solutions MMC | 1400234562 | İnformasiya texnologiyaları | Bakı, Nəsimi r. |
| W3 | Lider Market ASC | 1400345673 | Pərakəndə satış | Bakı, Yasamal r. |
| W4 | Qafqaz Tikinti MMC | 1400456784 | Tikinti | Bakı, Binəqədi r. |
| W5 | Şəfa Klinikası MMC | 1400567895 | Səhiyyə | Bakı, Səbail r. |
| W6 | Şəhər Poliklinikası №5 | 1400678906 | Səhiyyə (dövlət) | Şirvan |
| W7 | Nizami rayon 12 saylı məktəb | 1400789017 | Təhsil (dövlət) | Bakı, Yasamal r. |
| W8 | Aran Aqro MMC | 1400890128 | Kənd təsərrüfatı | Bərdə |
| W9 | Mavi Dəniz Turizm MMC | 1400901239 | Turizm | Qəbələ |
| W10 | Sumqayıt Kimya Zavodu ASC | 1401012340 | Sənaye | Sumqayıt |
| W11 | Avtoservis Sürət | 1401123451 | Xidmət | Bakı, Xətai r. |
| W12 | Orxan Hüquq Məsləhət | 1401234562 | Hüquq | Gəncə |
| W13 | Bakı Texno Qrup MMC | 1401345673 | Mühəndislik | Bakı, Nərimanov r. |

## C. Əmək fəaliyyəti (`Employment`)
`Son tarix` boşdursa, hazırda işləyir.

| Vətəndaş | İş yeri | Vəzifə | Maaş | Başlama | Son tarix | Çıxma səbəbi |
|---|---|---|---|---|---|---|
| 1 | W2 | Mühasib | 2400 | 2019-02-01 | | |
| 2 | W1 | Anbar müdiri | 1900 | 2016-05-10 | | |
| 3 | W3 | Kassir rəhbəri | 1100 | 2021-03-01 | | |
| 4 | W4 | Fəhlə | 750 | 2024-03-01 | | |
| 5 | W5 | Tibb bacısı | 1000 | 2022-08-15 | | |
| 6 | W11 | Usta | 1500 | 2014-04-01 | | |
| 7 | W2 | Layihə meneceri | 3200 | 2020-01-15 | | |
| 8 | W4 | Prorab | 1700 | 2017-06-01 | | |
| 9 | W6 | Həkim köməkçisi | 1300 | 2020-09-01 | | |
| 10 | W8 | Aqronom | 1600 | 2012-03-01 | | |
| 11 | W7 | Müəllim | 900 | 2018-09-15 | | |
| 12 | W3 | Mağaza müdiri | 1800 | 2015-02-01 | | |
| 13 | W9 | Rezervasiya əməkdaşı | 1050 | 2023-02-01 | | |
| 14 | W1 | Sürücü-ekspeditor | 1400 | 2013-07-01 | | |
| 15 | W2 | Dizayner | 1900 | 2021-11-01 | | |
| 16 | W8 | Traktorçu | 1000 | 2019-04-01 | | |
| 17 | W5 | Həkim | 2800 | 2016-10-01 | | |
| 18 | W10 | Operator | 1500 | 2011-01-10 | | |
| 19 | W11 | Usta | 1300 | 2020-06-01 | | |
| 20 | W9 | Qonaq xidmətləri əməkdaşı | 900 | 2022-05-01 | | |
| 21 | W13 | Mühəndis | 2600 | 2014-10-01 | | |
| 22 | W12 | Hüquqşünas | 1500 | 2021-09-01 | | |
| 23 | W1 | Yükləyici | 1200 | 2018-01-01 | | |
| 24 | W3 | Satıcı | 800 | 2022-02-01 | | |
| 25 | W8 | Fermer köməkçisi | 1100 | 2015-05-01 | | |
| 26 | W2 | Tester | 1700 | 2022-03-01 | | |
| 27 | W4 | Fəhlə | 900 | 2015-01-01 | 2019-06-30 | Həbs olunub |
| 28 | W3 | Təhlükəsizlik işçisi | 800 | 2014-01-01 | 2018-12-31 | Müqaviləyə xitam |
| 29 | W13 | Satış meneceri | 2000 | 2016-01-01 | 2022-04-30 | İşdən çıxarılıb |
| 33 | W10 | Texnik | 700 | 1985-09-01 | 2018-04-30 | Təqaüd |
| 35 | W3 | Satıcı | 700 | 2012-03-01 | 2020-08-31 | İxtisar |
| 35 | W13 | Satış meneceri | 2100 | 2020-09-01 | | |
| 36 | W1 | Katib | 950 | 2020-05-01 | | |

İş yeri olmayanlar: 30, 31 (vəfat), 32 (16 yaş), 34 (işsiz).

## D. Xarici banklar (6)
| # | Ad | BIN (8 rəqəm) |
|---|---|---|
| EB1 | Kapital Bank | 41697388 |
| EB2 | Azəri Kredit Bank | 52361402 |
| EB3 | Bakı Milli Bank | 40154877 |
| EB4 | Şəfəq Bank | 53780916 |
| EB5 | Qızıl Körpü Bank | 41298654 |
| EB6 | Dəniz Bank | 45067213 |

Caspian Bank: BIN **11223456**. `Kapital Bank` real adı sənin seçimin idi; istəsən uydurma ad qoyarıq.

## E. Xarici kreditlər (`ExternalLoan`)
Məbləğlər AZN. `Gec.` = gecikmə sayı, `Maks.gün` = ən uzun gecikmə, `Borc` = hazırda gecikmiş məbləğ.

| Vətəndaş | Bank | Məqsəd | Alınıb | Ödənilib | Aylıq | Başlama | Bitmə | Bağlı | Gec. | Maks.gün | Borc | Məhkəmə |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 3 | EB4 | Consumer | 1500 | 1500 | 135 | 2023-05-01 | 2024-04-01 | bəli | 0 | 0 | 0 | yox |
| 4 | EB3 | Consumer | 5000 | 1900 | 225 | 2025-03-01 | 2027-02-01 | yox | 4 | 28 | 700 | yox |
| 5 | EB2 | Consumer | 1200 | 1200 | 105 | 2023-04-01 | 2024-03-01 | bəli | 0 | 0 | 0 | yox |
| 6 | EB1 | Consumer | 2000 | 2000 | 180 | 2021-06-01 | 2022-05-01 | bəli | 0 | 0 | 0 | yox |
| 8 | EB1 | Consumer | 6000 | 6000 | 270 | 2021-03-01 | 2023-03-01 | bəli | 0 | 0 | 0 | yox |
| 9 | EB2 | Medical | 3000 | 1300 | 130 | 2025-09-01 | 2027-09-01 | yox | 0 | 0 | 0 | yox |
| 10 | EB3 | Repair | 12000 | 5400 | 420 | 2024-05-01 | 2027-05-01 | yox | 1 | 6 | 0 | yox |
| 12 | EB4 | Consumer | 4500 | 1500 | 195 | 2025-12-01 | 2027-11-01 | yox | 0 | 0 | 0 | yox |
| 14 | EB5 | Car | 8000 | 3900 | 340 | 2025-02-01 | 2027-01-01 | yox | 2 | 12 | 680 | yox |
| 16 | EB1 | Consumer | 2500 | 800 | 115 | 2026-02-01 | 2027-12-01 | yox | 0 | 0 | 0 | yox |
| 18 | EB3 | Mortgage | 15000 | 6000 | 280 | 2024-01-01 | 2029-01-01 | yox | 0 | 0 | 0 | yox |
| 18 | EB6 | Consumer | 2000 | 2000 | 100 | 2022-01-01 | 2023-12-01 | bəli | 0 | 0 | 0 | yox |
| 21 | EB1 | Mortgage | 20000 | 7400 | 330 | 2024-09-01 | 2029-09-01 | yox | 0 | 0 | 0 | yox |
| 22 | EB2 | Education | 1500 | 1500 | 130 | 2023-09-01 | 2024-08-01 | bəli | 0 | 0 | 0 | yox |
| 23 | EB1 | Consumer | 7000 | 2800 | 310 | 2025-03-01 | 2027-02-01 | yox | 4 | 35 | 640 | yox |
| 24 | EB5 | Consumer | 4000 | 1500 | 175 | 2025-06-01 | 2027-05-01 | yox | 3 | 21 | 380 | yox |
| 25 | EB4 | Business | 9000 | 3000 | 400 | 2024-12-01 | 2026-11-30 | yox | 5 | 48 | 1500 | yox |
| 26 | EB6 | Consumer | 3500 | 900 | 155 | 2026-03-01 | 2028-02-01 | yox | 1 | 5 | 0 | yox |
| 29 | EB5 | Business | 10000 | 2000 | 450 | 2023-06-01 | 2026-05-31 | yox | 6 | 90 | 4200 | **bəli** |
| 35 | EB1 | Consumer | 3000 | 3000 | 135 | 2022-05-01 | 2024-04-01 | bəli | 0 | 0 | 0 | yox |
| 36 | EB6 | Consumer | 5000 | 1500 | 230 | 2024-08-01 | 2026-07-01 | yox | 7 | 120 | 2300 | **bəli** |

Qaydaya görə çarpan: gecikmə sayı 0 → ×24, 1–2 → ×12, 3 və çox → ×4 (maaş üzərinə, ən çox 50 000).

---

## F. Qeydiyyatlı 6 müştəri (login ilə girilir)

| # | Vətəndaş | Email (nümunə) | Telefon | Kartlar (toxum) |
|---|---|---|---|---|
| R1 | Aysel Məmmədova | `SENINGMAIL+aysel@gmail.com` | 0557843838 | Standard 1250.00, Cashback 35.20 |
| R2 | Elçin Hüseynov | `SENINGMAIL+elcin@gmail.com` | 0777656565 | Standard 410.00, Cashback 78.90, Gold 5200.00 |
| R3 | Nigar Əliyeva | `SENINGMAIL+nigar@gmail.com` | 0503112131 | Standard 90.00, Cashback 12.00 |
| R4 | Rauf Quliyev | `SENINGMAIL+rauf@gmail.com` | 0773245566 | Standard 300.00, Cashback 0.00 |
| R5 | Leyla Cəfərova | `SENINGMAIL+leyla@gmail.com` | 0999895474 | Standard 2000.00, Cashback 0.00 |
| R6 | Tural Rzayev | `SENINGMAIL+tural@gmail.com` | 0102132345 | Standard 640.00, Cashback 41.50, Silver 900.00 |

### Caspian kreditləri (ödəniş kodu = `ContractNumber`)

| Müştəri | Kod | Vəziyyət | Məbləğ | Müddət | Məqsəd | Detallar |
|---|---|---|---|---|---|---|
| R1 Aysel | KR-2025-000012 | **Closed** | 6000 | 12 ay | Consumer | 2025-01-20-də verilib, vaxtında ödənilib, 2025-12-31-də bağlanıb. İndi aktiv krediti yoxdur. |
| R2 Elçin | KR-2025-000041 | **Active** | 8000 | 24 ay | Car | 2025-11-20-də verilib, 10 taksit vaxtında ödənilib (aylıq ≈369). 2026-06-15-də 1000 AZN hissəvi vaxtından əvvəl ödəniş, faiz **10% → 9%**. |
| R3 Nigar | KR-2026-000007 | **Active, gecikməli** | 3000 | 12 ay | Repair | 2026-01-20-də verilib (aylıq ≈264). Fevral–iyul ödənilib. **Avqust və sentyabr gecikib** (cərimə yığılır), bildiriş getməli. |
| R4 Rauf | KR-2026-000019 | **Offered** | istənilib 5000, təklif 3000 | 24 ay | Education | 2026-10-10-da müraciət. Maaş 750 və xarici gecikmə 4 (çarpan ×4 = 3000). Aylıq ≈138. Təklif **2026-10-19**-a qədər etibarlıdır, Rauf qəbul və ya rədd edə bilər. |
| R5 Leyla | (yoxdur) | | | | | Yeni müştəri, kredit tarixi yoxdur. Müraciəti sınamaq üçün. |
| R6 Tural | KR-2025-000003 | **Closed** (ClosedEarly) | 2000 | 6 ay | Consumer | 2025-03-10-da verilib, 2025-06-20-də tam vaxtından əvvəl ödənilib. |
| R6 Tural | KR-2026-000021 | **Pending** | 4000 | 18 ay | Business | 2026-10-11-də müraciət, işçi baxışı gözləyir. |

Kart əməliyyat tarixçəsini də real görünsün deyə hər qeydiyyatlı müştəriyə 5–10 sətir (top-up, köçürmə, kredit ödənişi) əlavə edərəm. Xüsusi istəyin yoxdursa mən qərar verirəm.

---

## G. Yoxlamalı hallar (hansı vətəndaşla nə sınanır)
| Vəziyyət | Kim |
|---|---|
| Qeydiyyat, təmiz | 7–22 (öz emailinlə) |
| Qeydiyyat, FİN reyestrdə yoxdur → xəta | uydurma FİN |
| Qeydiyyat, FİN düzdür, vəsiqə nömrəsi səhvdir → xəta | hər kəs |
| Qeydiyyat, FİN artıq qeydiyyatlıdır → xəta | 1–6 |
| Qeydiyyat, vəfat etmiş → xəta | 30, 31 |
| Kredit, normal (×24) | 7, 8, 9, 11–22 |
| Kredit, azaldılmış təklif | R4 (Rauf) |
| Kredit, qara siyahı → rədd | 27, 28, 29 |
| Kredit, məhkəmə var, qara siyahı yox | 36 |
| Kredit, 18 yaşdan kiçik | 32 |
| Kredit, gəlir yoxdur | 33, 34 |
| Kredit, iki iş yeri (ən yeni maaş götürülür) | 35 |
| Gecikmə bildirişi | R3 (Nigar) |
| Hissəvi erkən ödəniş və faiz azalması | R2 (Elçin) |

---

## H. Kodda nə yazıldı (addım 1, 12 oktyabr 2026): modellər, migration, seeder
Bu addım yalnız reyestri bazaya qoyur. Qeydiyyat, profil və kredit hələ dəyişməyib.

**Domain:** `Customer`, `Workplace`, `Employment`, `ExternalBank`, `ExternalLoan`, `OwnBank` (entities + `*Configuration`), enum `LoanPurpose`. `AppUser`-ə `CustomerId` (int?, unikal, boş ola bilər) və `Customer` əlavə olundu. `Customer.PhotoUrl` = `/citizens/{FİN}.jpg`.

**Repository:** `AppDbContext`-ə 6 yeni `DbSet`; migration `AddedStateRegistry` (`20261012170316`); `Data/Seed/registry.json` (DLL-in içinə embedded resource kimi qoyulur), `RegistrySeedModels.cs` (json-un quruluşu), `RegistrySeeder.cs`.

**Seeder necə işləyir:** `Program.cs`-də migration və admin seeder-dən sonra `RegistrySeeder.SeedAsync` çağırılır. İş yeri VÖEN ilə, bank BIN ilə, vətəndaş FİN ilə yoxlanılır: artıq varsa toxunulmur, yoxdursa əlavə olunur (idempotent). Deməli `registry.json`-a sonradan yeni vətəndaş yazsan, növbəti işə düşmədə əlavə olunur. Mövcud vətəndaşı dəyişmək üçün isə SQL-dən silib yenidən işə salmaq lazımdır (və ya əl ilə UPDATE).

**Yoxlama:** boş reyestrdə işə salınanda `36 vətəndaş, 13 iş yeri, 33 əmək qeydi, 6 bank, 21 xarici kredit, 1 OwnBank` yazıldı (3 qara siyahı, 2 vəfat). İkinci işə düşmədə heç nə təkrar yazılmadı.

**Şəkillər:** API-nin `wwwroot/citizens/` qovluğuna `5KM2R9A.jpg` kimi (FİN.jpg) qoyulacaq. Qovluq hələ yoxdur.

**Sonrakı addımlar:** (2) qeydiyyat FİN + vəsiqə ilə, ad/soyad/doğum tarixi reyestrdən, ilk kartlar avtomatik; (3) MVC qeydiyyat və profil; (4) 6 qeydiyyatlı müştəri və Caspian kreditləri (`Loan`, `LoanInstallment`, `LoanPaymentRecord` ilə).

---

## I. Təqdimat üçün 6 əlavə vətəndaş (12 oktyabr 2026)
Bunlar qeydiyyatdan KEÇİRİLMİR: təqdimatda canlı qeydiyyat üçün saxlanılır. Hamısı təmiz, uzun iş stajı, yaxşı maaş, xarici krediti yoxdur. Şəkilləri var.

| FİN | Vəsiqə | Ad Soyad | Doğum | İş yeri, vəzifə, maaş |
|---|---|---|---|---|
| 4WN7P2D | AZE14023651 | Lalə Qurbanova | 1994-04-18 | Odlar IT, Data analitik, 2700 |
| 6TB3K8M | AZE14136972 | Emil Kazımov | 1991-09-09 | Bakı Texno Qrup, Mühəndis-konstruktor, 2900 |
| 9HV5C2R | AZE14248035 | Nərmin Rəhimova | 1996-12-03 | Şəfa Klinikası, Həkim, 2200 |
| 2QF8J6X | AZE14359148 | Ruslan Hacıyev | 1987-06-27 | Lider Market, Region müdiri, 2600 |
| 7MD4Y9G | AZE14461259 | Aynur Süleymanova | 1993-08-14 | Odlar IT, Layihə əlaqələndiricisi, 2000 |
| 5ZC2T7L | AZE14572360 | Tofiq Rəsulov | 1990-02-21 | Xəzər Logistika, Logistika meneceri, 2400 |

Ümumi say indi: 42 vətəndaş (6 qeydiyyatlı, 36 qeydiyyatsız).

---

## J. Reyestr datası koddan SQL skriptinə köçürüldü (12 oktyabr 2026)
- Data indi `Repository/Data/Seed/registry.sql` faylındadır (42 vətəndaş, 13 iş yeri, 6 bank, 39 əmək qeydi, 21 xarici kredit, Caspian Bank qaydaları). **H bölməsindəki `RegistrySeeder` və `registry.json` silinib**: API işə düşəndə artıq reyestri özü yazmır.
- **Necə işlədilir:** SSMS-də `CaspianBankDataBase` seçilir, `registry.sql` açılıb Execute edilir. Əvvəl API bir dəfə işləməlidir (migration cədvəlləri yaradır). Skript təkrar işlədilə bilər: FİN, VÖEN və BIN ilə yoxlayır, olanlara toxunmur, olmayanı əlavə edir. Nəticə sətri: `42 / 13 / 6 / 39 / 21 / 1`.
- **Yoxlama:** skript tam boş bazada (migration-dan sonra) iki dəfə işlədildi, nəticə real bazanın datası ilə checksum müqayisəsində eyni çıxdı.
- **Sonra data dəyişmək:** SQL faylında dəyiş, sonra ya yeni bazada işlət, ya mövcud bazada `UPDATE` et. Mövcud vətəndaşı fayldan dəyişib skripti təkrar işlətsən dəyişmir (artıq var). Yeni vətəndaş əlavə etsən əlavə olunur.
- **Diqqət:** boş bazada reyestr boşdur → qeydiyyat "citizen not found" deyir və `SeedCustomers` 6 müştərini yaratmır (xəbərdarlıq loglanır). Müəllimə baza export ilə gedəndə data içində olur.
- Şəkillər SQL-də yoxdur, yalnız `PhotoUrl` yolu var. Fayllar `wwwroot/citizens/` qovluğundadır.
