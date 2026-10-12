/*
    Saxta dovlet reyestri: vetendaÅŸlar, is yerleri, xarici banklar, xarici kreditler ve Caspian Bank-in oz qaydalari.
    SSMS-de CaspianBankDataBase secilib iceri Execute edilir. Skript TEKRAR islede biler: artiq olan setirlere (FIN, VOEN, BIN) toxunmur,
    yalniz olmayanlari elave edir. Ilk once API bir defe isledilmelidir (migration cedvelleri yaradir).
    Qeyd: bunlar yalniz reyestrdir. Qeydiyyatdan kecmis 6 musteri (login, kartlar) API-nin SeedCustomers hissesi ile yaranir.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Caspian Bank (tek setir)
IF NOT EXISTS (SELECT 1 FROM OwnBanks)
INSERT INTO OwnBanks (Name, LogoUrl, Bin, MinLoanAmount, MaxLoanAmount, MinTermMonths, MaxTermMonths, AnnualInterestRate, MinAge, FixedPenalty, DailyPenaltyPercent, CourtAfterDays, MaxActiveLoans, MaxPaymentToIncomePercent, InterbankFeePercent, InterbankMinFee, MaxInterbankAmount)
VALUES (N'Caspian Bank', NULL, N'11223456', 500, 50000, 6, 48, 10, 18, 10, 0.5, 90, 3, 45, 1, NULL, 10000);

-- Is yerleri (VOEN ile yoxlanilir)
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400123451') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Xəzər Logistika MMC', N'1400123451', N'Nəqliyyat', N'Bakı, Suraxanı r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400234562') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Odlar IT Solutions MMC', N'1400234562', N'İnformasiya texnologiyaları', N'Bakı, Nəsimi r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400345673') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Lider Market ASC', N'1400345673', N'Pərakəndə satış', N'Bakı, Yasamal r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400456784') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Qafqaz Tikinti MMC', N'1400456784', N'Tikinti', N'Bakı, Binəqədi r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400567895') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Şəfa Klinikası MMC', N'1400567895', N'Səhiyyə', N'Bakı, Səbail r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400678906') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Şəhər Poliklinikası №5', N'1400678906', N'Səhiyyə (dövlət)', N'Şirvan');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400789017') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Nizami rayon 12 saylı məktəb', N'1400789017', N'Təhsil (dövlət)', N'Bakı, Yasamal r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400890128') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Aran Aqro MMC', N'1400890128', N'Kənd təsərrüfatı', N'Bərdə');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1400901239') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Mavi Dəniz Turizm MMC', N'1400901239', N'Turizm', N'Qəbələ');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1401012340') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Sumqayıt Kimya Zavodu ASC', N'1401012340', N'Sənaye', N'Sumqayıt');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1401123451') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Avtoservis Sürət', N'1401123451', N'Xidmət', N'Bakı, Xətai r.');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1401234562') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Orxan Hüquq Məsləhət', N'1401234562', N'Hüquq', N'Gəncə');
IF NOT EXISTS (SELECT 1 FROM Workplaces WHERE TaxId = N'1401345673') INSERT INTO Workplaces (Name, TaxId, Sector, Address) VALUES (N'Bakı Texno Qrup MMC', N'1401345673', N'Mühəndislik', N'Bakı, Nərimanov r.');

-- Xarici banklar (BIN ile yoxlanilir)
IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = N'41697388') INSERT INTO ExternalBanks (Name, LogoUrl, Bin) VALUES (N'Kapital Bank', NULL, N'41697388');
IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = N'52361402') INSERT INTO ExternalBanks (Name, LogoUrl, Bin) VALUES (N'Azəri Kredit Bank', NULL, N'52361402');
IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = N'40154877') INSERT INTO ExternalBanks (Name, LogoUrl, Bin) VALUES (N'Bakı Milli Bank', NULL, N'40154877');
IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = N'53780916') INSERT INTO ExternalBanks (Name, LogoUrl, Bin) VALUES (N'Şəfəq Bank', NULL, N'53780916');
IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = N'41298654') INSERT INTO ExternalBanks (Name, LogoUrl, Bin) VALUES (N'Qızıl Körpü Bank', NULL, N'41298654');
IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = N'45067213') INSERT INTO ExternalBanks (Name, LogoUrl, Bin) VALUES (N'Dəniz Bank', NULL, N'45067213');

-- VetendaÅŸlar (FIN ile yoxlanilir): her vetendaÅŸin is yeri ve xarici kreditleri ile birlikde
DECLARE @c INT;

-- 5KM2R9A Aysel Məmmədova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'5KM2R9A')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'5KM2R9A', N'AZE10482931', N'Aysel', N'Məmmədova', N'Elçin', N'Sevinc', '1992-03-14', N'Bakı', N'Bakı, Nərimanov r., Əhmədli küç. 14/8', NULL, 0, NULL, N'/citizens/5KM2R9A.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400234562'), N'Mühasib', 2400, '2019-02-01', NULL, NULL);
END;

-- 7XQ4D1B Elçin Hüseynov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'7XQ4D1B')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'7XQ4D1B', N'AZE10573402', N'Elçin', N'Hüseynov', N'Rəşid', N'Zəminə', '1988-07-02', N'Sumqayıt', N'Sumqayıt, 9-cu mkr., 21/45', NULL, 0, NULL, N'/citizens/7XQ4D1B.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400123451'), N'Anbar müdiri', 1900, '2016-05-10', NULL, NULL);
END;

-- 3HT8V6C Nigar Əliyeva
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'3HT8V6C')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'3HT8V6C', N'AZE10691157', N'Nigar', N'Əliyeva', N'Vüqar', N'Gülnarə', '1995-11-23', N'Gəncə', N'Bakı, Yasamal r., H. Əliyev küç. 7/31', NULL, 0, NULL, N'/citizens/3HT8V6C.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400345673'), N'Kassir rəhbəri', 1100, '2021-03-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'53780916'), 0, NULL, 1500, 1500, 135, '2023-05-01', '2024-04-01', 1, 0, 0, 0, 0, NULL);
END;

-- 9PL2N5D Rauf Quliyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'9PL2N5D')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'9PL2N5D', N'AZE10735620', N'Rauf', N'Quliyev', N'Tahir', N'Aynur', '1990-01-30', N'Lənkəran', N'Bakı, Binəqədi r., 8-ci mkr. 3/12', NULL, 0, NULL, N'/citizens/9PL2N5D.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400456784'), N'Fəhlə', 750, '2024-03-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'40154877'), 0, NULL, 5000, 1900, 225, '2025-03-01', '2027-02-01', 0, 4, 28, 700, 0, NULL);
END;

-- 1BW7Z4E Leyla Cəfərova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'1BW7Z4E')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'1BW7Z4E', N'AZE10828446', N'Leyla', N'Cəfərova', N'Nadir', N'Şəfiqə', '1999-05-09', N'Şəki', N'Bakı, Xətai r., Z. Əliyeva küç. 22/5', NULL, 0, NULL, N'/citizens/1BW7Z4E.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400567895'), N'Tibb bacısı', 1000, '2022-08-15', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'52361402'), 0, NULL, 1200, 1200, 105, '2023-04-01', '2024-03-01', 1, 0, 0, 0, 0, NULL);
END;

-- 6FG3J8H Tural Rzayev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'6FG3J8H')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'6FG3J8H', N'AZE10914073', N'Tural', N'Rzayev', N'Akif', N'Mehriban', '1985-12-18', N'Mingəçevir', N'Mingəçevir, H. Əliyev pr. 44/9', NULL, 0, NULL, N'/citizens/6FG3J8H.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401123451'), N'Usta', 1500, '2014-04-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41697388'), 0, NULL, 2000, 2000, 180, '2021-06-01', '2022-05-01', 1, 0, 0, 0, 0, NULL);
END;

-- 4CD9S2K Günel Həsənova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'4CD9S2K')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'4CD9S2K', N'AZE11027589', N'Günel', N'Həsənova', N'Fuad', N'Rəna', '1993-02-27', N'Bakı', N'Bakı, Səbail r., Rəsul Rza küç. 5/17', NULL, 0, NULL, N'/citizens/4CD9S2K.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400234562'), N'Layihə meneceri', 3200, '2020-01-15', NULL, NULL);
END;

-- 8RY5T1L Orxan Nəbiyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'8RY5T1L')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'8RY5T1L', N'AZE11136204', N'Orxan', N'Nəbiyev', N'Ramiz', N'Samirə', '1991-08-11', N'Quba', N'Bakı, Suraxanı r., Qaraçuxur 10/3', NULL, 0, NULL, N'/citizens/8RY5T1L.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400456784'), N'Prorab', 1700, '2017-06-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41697388'), 0, NULL, 6000, 6000, 270, '2021-03-01', '2023-03-01', 1, 0, 0, 0, 0, NULL);
END;

-- 2NA6Q7M Səbinə Vəliyeva
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'2NA6Q7M')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'2NA6Q7M', N'AZE11249871', N'Səbinə', N'Vəliyeva', N'Azər', N'Lalə', '1997-04-04', N'Şirvan', N'Şirvan, H. Əliyev pr. 12/6', NULL, 0, NULL, N'/citizens/2NA6Q7M.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400678906'), N'Həkim köməkçisi', 1300, '2020-09-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'52361402'), 4, NULL, 3000, 1300, 130, '2025-09-01', '2027-09-01', 0, 0, 0, 0, 0, NULL);
END;

-- AJ4X8W3 Kamran İsmayılov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'AJ4X8W3')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'AJ4X8W3', N'AZE11358026', N'Kamran', N'İsmayılov', N'Qasım', N'Südabə', '1983-09-19', N'Gəncə', N'Gəncə, Kəpəz r., Atatürk pr. 80/14', NULL, 0, NULL, N'/citizens/AJ4X8W3.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400890128'), N'Aqronom', 1600, '2012-03-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'40154877'), 5, NULL, 12000, 5400, 420, '2024-05-01', '2027-05-01', 0, 1, 6, 0, 0, NULL);
END;

-- B7E2U5P Ülviyyə Hacıyeva
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'B7E2U5P')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'B7E2U5P', N'AZE11463715', N'Ülviyyə', N'Hacıyeva', N'Faiq', N'Təranə', '1994-06-21', N'Bakı', N'Bakı, Yasamal r., Şərifzadə küç. 9/22', NULL, 0, NULL, N'/citizens/B7E2U5P.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400789017'), N'Müəllim', 900, '2018-09-15', NULL, NULL);
END;

-- C3S9K6V Vüqar Səfərov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'C3S9K6V')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'C3S9K6V', N'AZE11572948', N'Vüqar', N'Səfərov', N'Kamal', N'Gülşən', '1989-10-05', N'Zaqatala', N'Bakı, Nəsimi r., 28 May küç. 31/10', NULL, 0, NULL, N'/citizens/C3S9K6V.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400345673'), N'Mağaza müdiri', 1800, '2015-02-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'53780916'), 0, NULL, 4500, 1500, 195, '2025-12-01', '2027-11-01', 0, 0, 0, 0, 0, NULL);
END;

-- D8M1H4Y Zəhra Qasımova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'D8M1H4Y')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'D8M1H4Y', N'AZE11681350', N'Zəhra', N'Qasımova', N'Eldar', N'Ülkər', '2000-01-17', N'Naxçıvan', N'Bakı, Nərimanov r., Təbriz küç. 15/4', NULL, 0, NULL, N'/citizens/D8M1H4Y.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400901239'), N'Rezervasiya əməkdaşı', 1050, '2023-02-01', NULL, NULL);
END;

-- E5V7L2Z Rəşad Əhmədov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'E5V7L2Z')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'E5V7L2Z', N'AZE11794602', N'Rəşad', N'Əhmədov', N'Mübariz', N'Nərgiz', '1986-03-08', N'Bakı', N'Bakı, Xəzər r., Şüvəlan 17', NULL, 0, NULL, N'/citizens/E5V7L2Z.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400123451'), N'Sürücü-ekspeditor', 1400, '2013-07-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41298654'), 1, NULL, 8000, 3900, 340, '2025-02-01', '2027-01-01', 0, 2, 12, 680, 0, NULL);
END;

-- F2T4B9N Fidan Bağırova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'F2T4B9N')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'F2T4B9N', N'AZE11803179', N'Fidan', N'Bağırova', N'Nizami', N'Sevil', '1996-09-30', N'Şamaxı', N'Bakı, Binəqədi r., Biləcəri 5/2', NULL, 0, NULL, N'/citizens/F2T4B9N.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400234562'), N'Dizayner', 1900, '2021-11-01', NULL, NULL);
END;

-- G6Q8C3R Samir Abbasov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'G6Q8C3R')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'G6Q8C3R', N'AZE11916544', N'Samir', N'Abbasov', N'Etibar', N'Rafiqə', '1992-12-25', N'Bərdə', N'Bərdə, Cavad xan küç. 3', NULL, 0, NULL, N'/citizens/G6Q8C3R.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400890128'), N'Traktorçu', 1000, '2019-04-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41697388'), 0, NULL, 2500, 800, 115, '2026-02-01', '2027-12-01', 0, 0, 0, 0, 0, NULL);
END;

-- H9Z1D5S Məryəm Nəsirova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'H9Z1D5S')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'H9Z1D5S', N'AZE12025887', N'Məryəm', N'Nəsirova', N'Hikmət', N'Arzu', '1990-07-14', N'Bakı', N'Bakı, Səbail r., Neftçilər pr. 60/33', NULL, 0, NULL, N'/citizens/H9Z1D5S.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400567895'), N'Həkim', 2800, '2016-10-01', NULL, NULL);
END;

-- J4P3F7T Cavid Mustafayev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'J4P3F7T')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'J4P3F7T', N'AZE12134093', N'Cavid', N'Mustafayev', N'Zaur', N'Elnarə', '1987-05-22', N'Sumqayıt', N'Sumqayıt, Sülh küç. 18/60', NULL, 0, NULL, N'/citizens/J4P3F7T.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401012340'), N'Operator', 1500, '2011-01-10', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'40154877'), 2, NULL, 15000, 6000, 280, '2024-01-01', '2029-01-01', 0, 0, 0, 0, 0, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'45067213'), 0, NULL, 2000, 2000, 100, '2022-01-01', '2023-12-01', 1, 0, 0, 0, 0, NULL);
END;

-- K1U6G8X Nurlan Rüstəmov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'K1U6G8X')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'K1U6G8X', N'AZE12247618', N'Nurlan', N'Rüstəmov', N'Tofiq', N'Fəridə', '1993-11-03', N'Lənkəran', N'Bakı, Xətai r., H. Aslanov küç. 14/7', NULL, 0, NULL, N'/citizens/K1U6G8X.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401123451'), N'Usta', 1300, '2020-06-01', NULL, NULL);
END;

-- L7W2A4Q Aygün Kərimova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'L7W2A4Q')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'L7W2A4Q', N'AZE12359720', N'Aygün', N'Kərimova', N'Nüsrət', N'Kəmalə', '1998-02-16', N'Qəbələ', N'Qəbələ, Nizami küç. 27', NULL, 0, NULL, N'/citizens/L7W2A4Q.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400901239'), N'Qonaq xidmətləri əməkdaşı', 900, '2022-05-01', NULL, NULL);
END;

-- M3Y5E9B Elvin Məhərrəmov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'M3Y5E9B')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'M3Y5E9B', N'AZE12468135', N'Elvin', N'Məhərrəmov', N'Cəlal', N'Ramilə', '1984-08-28', N'Bakı', N'Bakı, Nəsimi r., Ü. Hacıbəyov küç. 9/12', NULL, 0, NULL, N'/citizens/M3Y5E9B.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401345673'), N'Mühəndis', 2600, '2014-10-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41697388'), 2, NULL, 20000, 7400, 330, '2024-09-01', '2029-09-01', 0, 0, 0, 0, 0, NULL);
END;

-- N8X1R6C Sevinc Talıbova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'N8X1R6C')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'N8X1R6C', N'AZE12571469', N'Sevinc', N'Talıbova', N'Anar', N'Zöhrə', '1996-01-12', N'Gəncə', N'Gəncə, Kəpəz r., Cavad xan küç. 33/5', NULL, 0, NULL, N'/citizens/N8X1R6C.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401234562'), N'Hüquqşünas', 1500, '2021-09-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'52361402'), 3, NULL, 1500, 1500, 130, '2023-09-01', '2024-08-01', 1, 0, 0, 0, 0, NULL);
END;

-- P2H7K3D Ramin Quliyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'P2H7K3D')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'P2H7K3D', N'AZE12683902', N'Ramin', N'Quliyev', N'Yusif', N'Zemfira', '1988-04-06', N'Bakı', N'Bakı, Suraxanı r., Hövsan 8/1', NULL, 0, NULL, N'/citizens/P2H7K3D.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400123451'), N'Yükləyici', 1200, '2018-01-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41697388'), 0, NULL, 7000, 2800, 310, '2025-03-01', '2027-02-01', 0, 4, 35, 640, 0, NULL);
END;

-- Q6D4M8F Gülnar Əsgərova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'Q6D4M8F')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'Q6D4M8F', N'AZE12796254', N'Gülnar', N'Əsgərova', N'Maqsud', N'Aliyə', '1991-06-19', N'Mingəçevir', N'Mingəçevir, Nizami küç. 10/2', NULL, 0, NULL, N'/citizens/Q6D4M8F.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400345673'), N'Satıcı', 800, '2022-02-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41298654'), 0, NULL, 4000, 1500, 175, '2025-06-01', '2027-05-01', 0, 3, 21, 380, 0, NULL);
END;

-- R9J2V5G Tahir Nağıyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'R9J2V5G')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'R9J2V5G', N'AZE12804371', N'Tahir', N'Nağıyev', N'Ədalət', N'Mətanət', '1985-02-09', N'Şəki', N'Şəki, M. F. Axundov küç. 6', NULL, 0, NULL, N'/citizens/R9J2V5G.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400890128'), N'Fermer köməkçisi', 1100, '2015-05-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'53780916'), 6, NULL, 9000, 3000, 400, '2024-12-01', '2026-11-30', 0, 5, 48, 1500, 0, NULL);
END;

-- S4L8N1H Ilahə Mikayılova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'S4L8N1H')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'S4L8N1H', N'AZE12917865', N'Ilahə', N'Mikayılova', N'Sabir', N'Gültəkin', '1994-10-27', N'Bakı', N'Bakı, Yasamal r., Şəhriyar küç. 4/9', NULL, 0, NULL, N'/citizens/S4L8N1H.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400234562'), N'Tester', 1700, '2022-03-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'45067213'), 0, NULL, 3500, 900, 155, '2026-03-01', '2028-02-01', 0, 1, 5, 0, 0, NULL);
END;

-- T1C3Z7J Anar Şahbazov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'T1C3Z7J')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'T1C3Z7J', N'AZE13026498', N'Anar', N'Şahbazov', N'Teymur', N'Şövkət', '1982-12-01', N'Bakı', N'Bakı, Sabunçu r., Bakıxanov 21/6', NULL, 1, N'Adam öldürmə, hökm qüvvədədir', N'/citizens/T1C3Z7J.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400456784'), N'Fəhlə', 900, '2015-01-01', '2019-06-30', N'Həbs olunub');
END;

-- U5B9P2K Vüsal Dadaşov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'U5B9P2K')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'U5B9P2K', N'AZE13139520', N'Vüsal', N'Dadaşov', N'Rövşən', N'Mina', '1990-03-15', N'Gəncə', N'Gəncə, Nizami r., Heydər Əliyev pr. 5/3', NULL, 1, N'Bank soyğunçuluğu', N'/citizens/U5B9P2K.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400345673'), N'Təhlükəsizlik işçisi', 800, '2014-01-01', '2018-12-31', N'Müqaviləyə xitam');
END;

-- V8F6W4L Kənan Orucov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'V8F6W4L')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'V8F6W4L', N'AZE13248073', N'Kənan', N'Orucov', N'Samir', N'Aytən', '1987-09-08', N'Bakı', N'Bakı, Nəsimi r., Rəşid Behbudov 12/9', NULL, 1, N'Saxta sənədlərlə kredit dələduzluğu', N'/citizens/V8F6W4L.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401345673'), N'Satış meneceri', 2000, '2016-01-01', '2022-04-30', N'İşdən çıxarılıb');
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41298654'), 6, NULL, 10000, 2000, 450, '2023-06-01', '2026-05-31', 0, 6, 90, 4200, 1, NULL);
END;

-- W3A1S9M Ağaəli Mirzəyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'W3A1S9M')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'W3A1S9M', N'AZE13351786', N'Ağaəli', N'Mirzəyev', N'Hüseyn', N'Xədicə', '1955-05-05', N'Quba', N'Quba, Heydər Əliyev küç. 40', '2025-11-03', 0, NULL, N'/citizens/W3A1S9M.jpg');
    SET @c = SCOPE_IDENTITY();
END;

-- X7G5Y2N Zeynəb Hüseynli
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'X7G5Y2N')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'X7G5Y2N', N'AZE13467209', N'Zeynəb', N'Hüseynli', N'Ədil', N'Firəngiz', '1949-08-08', N'Şəki', N'Şəki, Gənclik küç. 8/2', '2026-02-17', 0, NULL, N'/citizens/X7G5Y2N.jpg');
    SET @c = SCOPE_IDENTITY();
END;

-- Y2K8T6P Murad Əliyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'Y2K8T6P')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'Y2K8T6P', N'AZE13578312', N'Murad', N'Əliyev', N'Ceyhun', N'Nərmin', '2010-06-12', N'Bakı', N'Bakı, Nərimanov r., Atatürk pr. 3/5', NULL, 0, NULL, N'/citizens/Y2K8T6P.jpg');
    SET @c = SCOPE_IDENTITY();
END;

-- Z6R4Q1R Qəşəm Ağayev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'Z6R4Q1R')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'Z6R4Q1R', N'AZE13684957', N'Qəşəm', N'Ağayev', N'Mahir', N'Gövhər', '1958-04-10', N'Gəncə', N'Gəncə, Kəpəz r., Təbriz küç. 11/1', NULL, 0, NULL, N'/citizens/Z6R4Q1R.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401012340'), N'Texnik', 700, '1985-09-01', '2018-04-30', N'Təqaüd');
END;

-- 1D9H3X5 Pərvanə Ələsgərova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'1D9H3X5')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'1D9H3X5', N'AZE13792640', N'Pərvanə', N'Ələsgərova', N'Elman', N'Sədaqət', '2001-07-24', N'Bakı', N'Bakı, Nizami r., Q. Qarayev pr. 29/14', NULL, 0, NULL, N'/citizens/1D9H3X5.jpg');
    SET @c = SCOPE_IDENTITY();
END;

-- 2E7M6C8 İsmayıl Babayev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'2E7M6C8')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'2E7M6C8', N'AZE13805163', N'İsmayıl', N'Babayev', N'Rafael', N'Nailə', '1989-01-26', N'Bakı', N'Bakı, Xətai r., Əhmədli 22/3', NULL, 0, NULL, N'/citizens/2E7M6C8.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400345673'), N'Satıcı', 700, '2012-03-01', '2020-08-31', N'İxtisar');
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401345673'), N'Satış meneceri', 2100, '2020-09-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'41697388'), 0, NULL, 3000, 3000, 135, '2022-05-01', '2024-04-01', 1, 0, 0, 0, 0, NULL);
END;

-- 3J5V2B4 Xədicə Orucova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'3J5V2B4')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'3J5V2B4', N'AZE13914728', N'Xədicə', N'Orucova', N'Zakir', N'Pakizə', '1992-09-02', N'Lənkəran', N'Lənkəran, Seyid Əzim küç. 15/2', NULL, 0, NULL, N'/citizens/3J5V2B4.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400123451'), N'Katib', 950, '2020-05-01', NULL, NULL);
    INSERT INTO ExternalLoans (CustomerId, ExternalBankId, Purpose, PurposeNote, BorrowedAmount, PaidAmount, MonthlyPayment, StartDate, EndDate, IsClosed, DelayCount, MaxDelayDays, OverdueAmount, WentToCourt, Note)
    VALUES (@c, (SELECT Id FROM ExternalBanks WHERE Bin = N'45067213'), 0, NULL, 5000, 1500, 230, '2024-08-01', '2026-07-01', 0, 7, 120, 2300, 1, NULL);
END;

-- 4WN7P2D Lalə Qurbanova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'4WN7P2D')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'4WN7P2D', N'AZE14023651', N'Lalə', N'Qurbanova', N'Rövşən', N'Gülbahar', '1994-04-18', N'Bakı', N'Bakı, Nərimanov r., Ə. Rəcəbli küç. 12/7', NULL, 0, NULL, N'/citizens/4WN7P2D.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400234562'), N'Data analitik', 2700, '2019-06-01', NULL, NULL);
END;

-- 6TB3K8M Emil Kazımov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'6TB3K8M')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'6TB3K8M', N'AZE14136972', N'Emil', N'Kazımov', N'Fəxrəddin', N'Şəhla', '1991-09-09', N'Gəncə', N'Bakı, Yasamal r., İ. Qayıbov küç. 25/14', NULL, 0, NULL, N'/citizens/6TB3K8M.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1401345673'), N'Mühəndis-konstruktor', 2900, '2017-02-01', NULL, NULL);
END;

-- 9HV5C2R Nərmin Rəhimova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'9HV5C2R')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'9HV5C2R', N'AZE14248035', N'Nərmin', N'Rəhimova', N'Telman', N'Zümrüd', '1996-12-03', N'Sumqayıt', N'Sumqayıt, 11-ci mkr., 7/33', NULL, 0, NULL, N'/citizens/9HV5C2R.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400567895'), N'Həkim', 2200, '2021-10-01', NULL, NULL);
END;

-- 2QF8J6X Ruslan Hacıyev
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'2QF8J6X')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'2QF8J6X', N'AZE14359148', N'Ruslan', N'Hacıyev', N'Elmar', N'Pərvin', '1987-06-27', N'Şirvan', N'Bakı, Səbail r., Azadlıq pr. 40/21', NULL, 0, NULL, N'/citizens/2QF8J6X.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400345673'), N'Region müdiri', 2600, '2014-08-01', NULL, NULL);
END;

-- 7MD4Y9G Aynur Süleymanova
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'7MD4Y9G')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'7MD4Y9G', N'AZE14461259', N'Aynur', N'Süleymanova', N'Vaqif', N'Şəfa', '1993-08-14', N'Bakı', N'Bakı, Xətai r., Nobel pr. 18/6', NULL, 0, NULL, N'/citizens/7MD4Y9G.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400234562'), N'Layihə əlaqələndiricisi', 2000, '2020-04-01', NULL, NULL);
END;

-- 5ZC2T7L Tofiq Rəsulov
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Fin = N'5ZC2T7L')
BEGIN
    INSERT INTO Customers (Fin, IdCardNumber, Name, Surname, FatherName, MotherName, BirthDate, BirthPlace, ResidenceAddress, DeathDate, IsBlacklisted, BlacklistReason, PhotoUrl)
    VALUES (N'5ZC2T7L', N'AZE14572360', N'Tofiq', N'Rəsulov', N'Mahmud', N'Səadət', '1990-02-21', N'Lənkəran', N'Bakı, Binəqədi r., 9-cu mkr. 5/18', NULL, 0, NULL, N'/citizens/5ZC2T7L.jpg');
    SET @c = SCOPE_IDENTITY();
    INSERT INTO Employments (CustomerId, WorkplaceId, Position, MonthlySalary, StartDate, EndDate, LeaveReason)
    VALUES (@c, (SELECT Id FROM Workplaces WHERE TaxId = N'1400123451'), N'Logistika meneceri', 2400, '2016-09-01', NULL, NULL);
END;

COMMIT TRANSACTION;

-- Yoxlama: gozlenen netice 42 / 13 / 6 / 39 / 21 / 1
SELECT (SELECT COUNT(*) FROM Customers) AS Customers, (SELECT COUNT(*) FROM Workplaces) AS Workplaces, (SELECT COUNT(*) FROM ExternalBanks) AS ExternalBanks, (SELECT COUNT(*) FROM Employments) AS Employments, (SELECT COUNT(*) FROM ExternalLoans) AS ExternalLoans, (SELECT COUNT(*) FROM OwnBanks) AS OwnBanks;
