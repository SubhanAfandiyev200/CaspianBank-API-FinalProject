-- Top-up üçün hazır kartlar (təkrar işlədilə bilər, olanı yenidən yazmır).
-- 1) Başqa bank kartı (demo): 4111 1111 1111 1111. Ondan top-up edəndə heç yerdən pul çıxmır, çünki xarici kartın balansı bizdə yoxdur. Ana səhifədəki "Use a demo card" düyməsi bunu doldurur.
-- 2) Caspian Gold kartı: 9630 1020 7777 7772, bitmə tarixi 10/30, CVV istənilən 3 rəqəm, balans 100 000 AZN. Sahibi Rauf Quliyev.
--    Başqa karta top-up edəndə pul bu kartdan çıxır, balans çatmasa "That card doesn't have enough balance" xətası çıxır.
-- Qeyd: sqlcmd ilə işlədəndə -I düyməsi lazımdır (Cards cədvəlində filtrli indeks var).
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM ExternalBanks WHERE Bin = '41111111')
    INSERT INTO ExternalBanks (Name, Bin) VALUES (N'Demo Bank', '41111111');

IF NOT EXISTS (SELECT 1 FROM ExternalCards WHERE CardNumber = '4111111111111111')
    INSERT INTO ExternalCards (ExternalBankId, CardNumber, HolderName, IsActive)
    SELECT Id, '4111111111111111', N'Demo Cardholder', 1 FROM ExternalBanks WHERE Bin = '41111111';

DECLARE @user nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE Email LIKE '%+rauf@%');
IF @user IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Cards WHERE CardNumber = '9630102077777772')
    INSERT INTO Cards (UserId, Tier, CardDesignId, CardNumber, ExpiryDate, Balance, IsBlocked)
    VALUES (@user, 3, 3, '9630102077777772', '2030-10-31', 100000.00, 0);

PRINT 'Top-up cards are ready.';
