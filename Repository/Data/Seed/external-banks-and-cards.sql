/*
  Başqa banklar və onların saxta kartları (köçürmə üçün). Təkrar işlətsən zərər yoxdur: banklar BIN-ə, kartlar nömrəyə görə yoxlanılır.
  Bank adları real Azərbaycan bankları, BIN-lər sınaq üçündür. Loqolar API-nin wwwroot/images/banks qovluğundadır.
*/
SET NOCOUNT ON;

-- 1) Banklar: mövcud 6 sətir (Id 1-6, kreditlər onlara bağlıdır) real adlarla yenilənir, çatmayan olarsa əlavə olunur
IF EXISTS (SELECT 1 FROM ExternalBanks WHERE Id = 1) UPDATE ExternalBanks SET Name = N'Kapital Bank', Bin = N'41697388' WHERE Id = 1;
IF EXISTS (SELECT 1 FROM ExternalBanks WHERE Id = 2) UPDATE ExternalBanks SET Name = N'International Bank of Azerbaijan (ABB)', Bin = N'55220992' WHERE Id = 2;
IF EXISTS (SELECT 1 FROM ExternalBanks WHERE Id = 3) UPDATE ExternalBanks SET Name = N'Rabitəbank', Bin = N'40154877' WHERE Id = 3;
IF EXISTS (SELECT 1 FROM ExternalBanks WHERE Id = 4) UPDATE ExternalBanks SET Name = N'PASHA Bank', Bin = N'53780916' WHERE Id = 4;
IF EXISTS (SELECT 1 FROM ExternalBanks WHERE Id = 5) UPDATE ExternalBanks SET Name = N'Bank Respublika', Bin = N'41298654' WHERE Id = 5;
IF EXISTS (SELECT 1 FROM ExternalBanks WHERE Id = 6) UPDATE ExternalBanks SET Name = N'Xalq Bank', Bin = N'45067213' WHERE Id = 6;

-- Loqolar (API wwwroot/images/banks)
UPDATE ExternalBanks SET LogoUrl = N'/images/banks/kapital.png' WHERE Bin = N'41697388';
UPDATE ExternalBanks SET LogoUrl = N'/images/banks/abb.png' WHERE Bin = N'55220992';
UPDATE ExternalBanks SET LogoUrl = N'/images/banks/rabite.png' WHERE Bin = N'40154877';
UPDATE ExternalBanks SET LogoUrl = N'/images/banks/pasha.png' WHERE Bin = N'53780916';
UPDATE ExternalBanks SET LogoUrl = N'/images/banks/respublika.png' WHERE Bin = N'41298654';
UPDATE ExternalBanks SET LogoUrl = N'/images/banks/xalq.png' WHERE Bin = N'45067213';

-- 2) Saxta kartlar (hər kartın ilk 8 rəqəmi bankın BIN-idir, Luhn yoxlaması keçir)
DECLARE @c TABLE (Bin NVARCHAR(8), CardNumber NVARCHAR(19), HolderName NVARCHAR(100));
INSERT INTO @c VALUES
(N'41697388', N'4169738884344825', N'Elnur Məmmədov'),
(N'55220992', N'5522099261501233', N'Aysel Həsənova'),
(N'40154877', N'4015487785192000', N'Rəşad Əliyev'),
(N'53780916', N'5378091683814406', N'Günel Quliyeva'),
(N'41298654', N'4129865474658360', N'Orxan Hüseynov'),
(N'45067213', N'4506721304517123', N'Səbinə İsmayılova'),
(N'41697388', N'4169738810055404', N'Kamran Abbasov'),
(N'55220992', N'5522099205229800', N'Nigar Nəsirova'),
(N'40154877', N'4015487704203193', N'Vüqar Mustafayev'),
(N'53780916', N'5378091678429673', N'Leyla Rzayeva'),
(N'41298654', N'4129865416749871', N'Cavid Bağırov'),
(N'45067213', N'4506721338680061', N'Fidan Süleymanova'),
(N'41697388', N'4169738894295603', N'Samir Kərimov'),
(N'55220992', N'5522099292139334', N'Məryəm Qasımova'),
(N'40154877', N'4015487715267294', N'Tural Əhmədov'),
(N'53780916', N'5378091695809360', N'Aynur Talıbova'),
(N'41298654', N'4129865466131285', N'Emil Orucov'),
(N'45067213', N'4506721341330720', N'Nərmin Cəfərova'),
(N'41697388', N'4169738835095310', N'Ramin Nağıyev'),
(N'55220992', N'5522099223127697', N'Gülnar Əsgərova'),
(N'40154877', N'4015487784853321', N'Anar Səfərov'),
(N'53780916', N'5378091650070578', N'Ülviyyə Hacıyeva'),
(N'41298654', N'4129865438274759', N'Fərid Rəhimov'),
(N'45067213', N'4506721307049439', N'Sevinc Mikayılova'),
(N'41697388', N'4169738829035868', N'Ruslan Qurbanov'),
(N'55220992', N'5522099274874973', N'Lalə Şahbazova'),
(N'40154877', N'4015487750147492', N'Elvin Dadaşov'),
(N'53780916', N'5378091659539482', N'Zərifə Ələsgərova'),
(N'41298654', N'4129865478138831', N'Tofiq Babayev'),
(N'45067213', N'4506721300457167', N'Aytən Vəliyeva'),
(N'41697388', N'4169738834007746', N'Murad Rəsulov'),
(N'55220992', N'5522099278920467', N'Pərvin Ağayeva'),
(N'40154877', N'4015487704063795', N'Kənan Kazımov'),
(N'53780916', N'5378091667351458', N'Arzu Yusifova'),
(N'41298654', N'4129865497993323', N'Həsən Hüseynli'),
(N'45067213', N'4506721345216396', N'Nurlan Zeynalova'),
(N'41697388', N'4169738801184692', N'Mehriban Piriyev'),
(N'55220992', N'5522099254843675', N'Eldar Mirzəyeva'),
(N'40154877', N'4015487776462974', N'Şəbnəm Əfəndiyev'),
(N'53780916', N'5378091600260469', N'Rövşən Sadıqova');

INSERT INTO ExternalCards (ExternalBankId, CardNumber, HolderName, IsActive)
SELECT b.Id, c.CardNumber, c.HolderName, 1
FROM @c c JOIN ExternalBanks b ON b.Bin = c.Bin
WHERE NOT EXISTS (SELECT 1 FROM ExternalCards e WHERE e.CardNumber = c.CardNumber);
