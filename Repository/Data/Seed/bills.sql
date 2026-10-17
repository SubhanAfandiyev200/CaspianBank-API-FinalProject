/*
    Odenis (Bills) ucun saxta melumat: teschizatcilar (operatorlar, kommunal, internet) ve onlarin saxta abunecileri.
    SSMS-de CaspianBankDataBase secilib iceri Execute edilir (sqlcmd ile: -f 65001). Skript TEKRAR islede biler: artiq olan teschizatciya (Code)
    ve abuneciye (teschizatci + nomre/kod) toxunmur, yalniz olmayanlari elave edir. Ilk once API bir defe isledilmelidir (migration cedvelleri yaradir).

    Bu melumat koddan yox bazadan gelir: kod silinse de bazada qalir.
    - 30 mobil nomre: Nar (070, 077), Azercell (010, 050, 051), Bakcell (055, 099), her operatordan 10 nomre.
    - Kommunal ve internet abunecileri: her teschizatcinin oz kod uzunlugu var (Azerisiq 10, Azeriqaz 9, Azersu 8, CityNet 7 reqem).
    - Balance: menfi = borc, musbet = qabaqcadan odenilmis mebleg. Aylig hesabi API-nin arxa plan xidmeti her ay yazir.
    Teschizatcinin adi, loqosu ve aktivliyi sonradan admin paneldeki "Bill providers" sehifesinden deyisir.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Teschizatcilar. Category: 0 Mobile, 1 Electricity, 2 Gas, 3 Water, 4 Internet
DECLARE @p TABLE (Code NVARCHAR(30), Name NVARCHAR(60), Category INT, LogoUrl NVARCHAR(300), SortOrder INT, Prefixes NVARCHAR(40), IdentifierLength INT, IdentifierLabel NVARCHAR(40), UnitName NVARCHAR(10), UnitPrice DECIMAL(18,4), MinPayment DECIMAL(18,2), MaxPayment DECIMAL(18,2));
INSERT INTO @p VALUES
(N'nar',       N'Nar',       0, N'/images/providers/nar.jpg',       1, N'70,77',    10, N'Phone number',    NULL,    0,      1.00, 200.00),
(N'azercell',  N'Azercell',  0, N'/images/providers/azercell.png',  2, N'10,50,51', 10, N'Phone number',    NULL,    0,      1.00, 200.00),
(N'bakcell',   N'Bakcell',   0, N'/images/providers/bakcell.png',   3, N'55,99',    10, N'Phone number',    NULL,    0,      1.00, 200.00),
(N'azerishiq', N'Azərişıq',  1, N'/images/providers/azerishiq.png', 4, NULL,        10, N'Subscriber code', N'kWh',  0.0900, 1.00, 2000.00),
(N'azerigas',  N'Azəriqaz',  2, N'/images/providers/azerigas.png',  5, NULL,         9, N'Subscriber code', N'm³',   0.2200, 1.00, 2000.00),
(N'azersu',    N'Azərsu',    3, N'/images/providers/azersu.png',    6, NULL,         8, N'Subscriber code', N'm³',   0.4300, 1.00, 1000.00),
(N'citynet',   N'CityNet',   4, N'/images/providers/citynet.png',   7, NULL,         7, N'Subscriber code', NULL,    0,      1.00, 500.00);

INSERT INTO BillProviders (Code, Name, Category, LogoUrl, SortOrder, IsActive, Prefixes, IdentifierLength, IdentifierLabel, UnitName, UnitPrice, MinPayment, MaxPayment)
SELECT x.Code, x.Name, x.Category, x.LogoUrl, x.SortOrder, 1, x.Prefixes, x.IdentifierLength, x.IdentifierLabel, x.UnitName, x.UnitPrice, x.MinPayment, x.MaxPayment
FROM @p x
WHERE NOT EXISTS (SELECT 1 FROM BillProviders b WHERE b.Code = x.Code);

-- Abunecilər. Mobil: Balance = nomrenin balansi. Kommunal: AverageUsage = ayliq orta isletme (kWh ve ya m3), mövsüme gore deyisir.
-- Internet: MonthlyFee = paketin qiymeti (20, 30 ve ya 50 AZN)
DECLARE @s TABLE (ProviderCode NVARCHAR(30), Identifier NVARCHAR(20), FullName NVARCHAR(100), Balance DECIMAL(18,2), MonthlyFee DECIMAL(18,2), PackageName NVARCHAR(40), AverageUsage DECIMAL(18,2));
INSERT INTO @s VALUES
-- Nar (070, 077)
(N'nar', N'0771420618', N'Rəşad Məmmədov',      12.50, NULL, NULL, NULL),
(N'nar', N'0773856024', N'Günel Əliyeva',        3.20, NULL, NULL, NULL),
(N'nar', N'0775031947', N'Elvin Hüseynov',       0.00, NULL, NULL, NULL),
(N'nar', N'0777290385', N'Aytən Quliyeva',      25.00, NULL, NULL, NULL),
(N'nar', N'0779614832', N'Orxan Nəsirov',        7.80, NULL, NULL, NULL),
(N'nar', N'0700517293', N'Sevinc Hacıyeva',      1.40, NULL, NULL, NULL),
(N'nar', N'0702846150', N'Tahir Bayramov',      18.90, NULL, NULL, NULL),
(N'nar', N'0704379861', N'Leyla Rüstəmova',      0.50, NULL, NULL, NULL),
(N'nar', N'0706125947', N'Kamran İsmayılov',     9.00, NULL, NULL, NULL),
(N'nar', N'0708903716', N'Nərmin Qasımova',     15.25, NULL, NULL, NULL),
-- Azercell (010, 050, 051)
(N'azercell', N'0102847391', N'Fərid Səfərov',       6.40, NULL, NULL, NULL),
(N'azercell', N'0105630284', N'Aygün Mustafayeva',   2.10, NULL, NULL, NULL),
(N'azercell', N'0107419052', N'Vüqar Cəfərov',       0.00, NULL, NULL, NULL),
(N'azercell', N'0502318674', N'Təranə Abbasova',    21.70, NULL, NULL, NULL),
(N'azercell', N'0504765102', N'Elnur Əhmədov',       4.60, NULL, NULL, NULL),
(N'azercell', N'0506093847', N'Səbinə Kərimova',    11.30, NULL, NULL, NULL),
(N'azercell', N'0508421963', N'Rövşən Əsgərov',      0.90, NULL, NULL, NULL),
(N'azercell', N'0511754208', N'Gülnar Vəliyeva',     8.00, NULL, NULL, NULL),
(N'azercell', N'0513962075', N'Emin Zeynalov',      13.45, NULL, NULL, NULL),
(N'azercell', N'0517308416', N'Nigar Şükürova',      5.15, NULL, NULL, NULL),
-- Bakcell (055, 099)
(N'bakcell', N'0551682039', N'Cavid Rzayev',        10.00, NULL, NULL, NULL),
(N'bakcell', N'0553047918', N'Ləman Yusifova',       3.75, NULL, NULL, NULL),
(N'bakcell', N'0555826471', N'Samir Hüseynli',       0.00, NULL, NULL, NULL),
(N'bakcell', N'0557391604', N'Ülviyyə Məmmədli',    19.20, NULL, NULL, NULL),
(N'bakcell', N'0559204753', N'Murad Nuriyev',        6.30, NULL, NULL, NULL),
(N'bakcell', N'0991536280', N'Zəminə Əliyeva',       1.85, NULL, NULL, NULL),
(N'bakcell', N'0993870415', N'İlkin Babayev',       14.60, NULL, NULL, NULL),
(N'bakcell', N'0995248169', N'Röya Mehdiyeva',       0.25, NULL, NULL, NULL),
(N'bakcell', N'0997614032', N'Tofiq Qurbanov',       7.10, NULL, NULL, NULL),
(N'bakcell', N'0999380527', N'Şəbnəm Allahverdiyeva', 22.40, NULL, NULL, NULL),
-- Azerisiq (10 reqemli abunecı kodu, kWh)
(N'azerishiq', N'1020345601', N'Əli Hüseynzadə',      -38.40, NULL, NULL, 210),
(N'azerishiq', N'1020345712', N'Zeynəb Məmmədova',      0.00, NULL, NULL, 165),
(N'azerishiq', N'1020356823', N'Rafiq Abdullayev',    -96.10, NULL, NULL, 420),
(N'azerishiq', N'1020367934', N'Məryəm Əliyeva',       24.50, NULL, NULL, 130),
(N'azerishiq', N'1030478045', N'Fuad Qədirov',        -12.75, NULL, NULL, 285),
(N'azerishiq', N'1030489156', N'Xədicə Rəhimova',       0.00, NULL, NULL, 190),
(N'azerishiq', N'1030490267', N'Nadir Süleymanov',   -154.30, NULL, NULL, 510),
(N'azerishiq', N'1040501378', N'Sənubər İbrahimova',    9.20, NULL, NULL, 150),
(N'azerishiq', N'1040512489', N'Vasif Mirzəyev',      -27.90, NULL, NULL, 240),
(N'azerishiq', N'1040523590', N'Pərvin Həsənova',       0.00, NULL, NULL, 175),
-- Azeriqaz (9 reqemli abunecı kodu, m3)
(N'azerigas', N'400120031', N'Əhməd Qarayev',         -64.20, NULL, NULL, 70),
(N'azerigas', N'400120142', N'Sevda Novruzova',          0.00, NULL, NULL, 60),
(N'azerigas', N'400131253', N'İsa Hüseynov',          -188.50, NULL, NULL, 110),
(N'azerigas', N'400142364', N'Gülşən Əkbərova',         31.00, NULL, NULL, 45),
(N'azerigas', N'400153475', N'Ramil Əsədov',           -22.60, NULL, NULL, 85),
(N'azerigas', N'410164586', N'Mehriban Qasımlı',         0.00, NULL, NULL, 55),
(N'azerigas', N'410175697', N'Cəlal Xəlilov',          -97.30, NULL, NULL, 130),
(N'azerigas', N'410186708', N'Şahnaz Orucova',          12.40, NULL, NULL, 40),
(N'azerigas', N'410197819', N'Elşən Kazımov',          -45.00, NULL, NULL, 95),
(N'azerigas', N'420108920', N'Ziba Bağırova',            0.00, NULL, NULL, 65),
-- Azersu (8 reqemli abunecı kodu, m3)
(N'azersu', N'50120341', N'Qasım Məmmədli',            -18.40, NULL, NULL, 14),
(N'azersu', N'50120452', N'Tahirə Əliyeva',              0.00, NULL, NULL, 10),
(N'azersu', N'50131563', N'Elçin Rəsulov',             -41.70, NULL, NULL, 22),
(N'azersu', N'50142674', N'Gülər Cabbarova',             7.50, NULL, NULL, 9),
(N'azersu', N'50153785', N'Mahir Fətəliyev',            -6.20, NULL, NULL, 16),
(N'azersu', N'51164896', N'Aynur Seyidova',              0.00, NULL, NULL, 12),
(N'azersu', N'51175907', N'Nizami Bəşirov',            -29.10, NULL, NULL, 26),
(N'azersu', N'51186018', N'Lalə Həsənli',               14.00, NULL, NULL, 8),
-- CityNet (7 reqemli abunecı kodu, 3 paket: 20, 30, 50 AZN)
(N'citynet', N'7010101', N'Kənan Əsgərli',             -20.00, 20, N'Start 20 AZN',    NULL),
(N'citynet', N'7010202', N'Samirə Cəfərli',              0.00, 30, N'Comfort 30 AZN',  NULL),
(N'citynet', N'7010303', N'Orxan Vəliyev',             -50.00, 50, N'Premium 50 AZN',  NULL),
(N'citynet', N'7010404', N'Nurlan Haqverdiyev',         35.00, 20, N'Start 20 AZN',    NULL),
(N'citynet', N'7020505', N'Aysel Ramazanova',          -30.00, 30, N'Comfort 30 AZN',  NULL),
(N'citynet', N'7020606', N'Elmar Quliyev',               0.00, 50, N'Premium 50 AZN',  NULL),
(N'citynet', N'7020707', N'Fidan Sadıqova',             12.50, 20, N'Start 20 AZN',    NULL),
(N'citynet', N'7020808', N'Ruslan Ağayev',             -65.00, 30, N'Comfort 30 AZN',  NULL),
(N'citynet', N'7030909', N'Könül Əmirova',               0.00, 50, N'Premium 50 AZN',  NULL),
(N'citynet', N'7031010', N'Teymur Şirinov',            -20.00, 20, N'Start 20 AZN',    NULL),
(N'citynet', N'7031111', N'Ülkər Mahmudova',            10.00, 30, N'Comfort 30 AZN',  NULL),
(N'citynet', N'7031212', N'Rəhim Hüseynli',           -100.00, 50, N'Premium 50 AZN',  NULL);

INSERT INTO BillSubscribers (ProviderId, Identifier, FullName, Balance, MonthlyFee, PackageName, AverageUsage, IsActive)
SELECT p.Id, s.Identifier, s.FullName, s.Balance, s.MonthlyFee, s.PackageName, s.AverageUsage, 1
FROM @s s
JOIN BillProviders p ON p.Code = s.ProviderCode
WHERE NOT EXISTS (SELECT 1 FROM BillSubscribers b WHERE b.ProviderId = p.Id AND b.Identifier = s.Identifier);

COMMIT TRANSACTION;

SELECT p.Name, COUNT(s.Id) AS Subscribers
FROM BillProviders p
LEFT JOIN BillSubscribers s ON s.ProviderId = p.Id
GROUP BY p.Name, p.SortOrder
ORDER BY p.SortOrder;
