-- Top-up mətnləri (az, ru). Təkrar işlədilə bilər: olan tərcümələri dəyişmir.
SET NOCOUNT ON;
DECLARE @t TABLE ([Language] nvarchar(5), [Key] nvarchar(400), [Value] nvarchar(2000));
INSERT INTO @t VALUES (N'az', N'from a card of another bank or from another Caspian card. A Caspian card is charged for real.', N'başqa bankın kartından və ya başqa Caspian kartından. Caspian kartından pul həqiqətən çıxılır.');
INSERT INTO @t VALUES (N'ru', N'from a card of another bank or from another Caspian card. A Caspian card is charged for real.', N'с карты другого банка или другой карты Caspian. С карты Caspian деньги списываются по-настоящему.');
INSERT INTO @t VALUES (N'az', N'A card of another bank is a demo: no real money moves, and the card details are never stored.', N'Başqa bankın kartı demodur: real pul hərəkət etmir, kart məlumatları saxlanmır.');
INSERT INTO @t VALUES (N'ru', N'A card of another bank is a demo: no real money moves, and the card details are never stored.', N'Карта другого банка — демо: реальные деньги не движутся, данные карты не сохраняются.');
INSERT INTO @t VALUES (N'az', N'Choose a different card from the one you''re topping up.', N'Artırdığınız kartdan fərqli kart seçin.');
INSERT INTO @t VALUES (N'ru', N'Choose a different card from the one you''re topping up.', N'Выберите карту, отличную от пополняемой.');
INSERT INTO @t VALUES (N'az', N'The card details are not correct.', N'Kart məlumatları düzgün deyil.');
INSERT INTO @t VALUES (N'ru', N'The card details are not correct.', N'Данные карты неверны.');
INSERT INTO @t VALUES (N'az', N'Money left your card', N'Kartınızdan pul çıxdı');
INSERT INTO @t VALUES (N'ru', N'Money left your card', N'С вашей карты списаны деньги');
INSERT INTO @t VALUES (N'az', N'Top up of card •••• {0}', N'•••• {0} kartının artırılması');
INSERT INTO @t VALUES (N'ru', N'Top up of card •••• {0}', N'Пополнение карты •••• {0}');
INSERT INTO @t VALUES (N'az', N'Top up from card •••• {0}', N'•••• {0} kartından artırma');
INSERT INTO @t VALUES (N'ru', N'Top up from card •••• {0}', N'Пополнение с карты •••• {0}');
INSERT INTO @t VALUES (N'az', N'Top up sent', N'Artırma (göndərilən)');
INSERT INTO @t VALUES (N'ru', N'Top up sent', N'Пополнение (отправлено)');
INSERT INTO Translations ([Language], [Key], [Value], CreatedAt)
SELECT s.[Language], s.[Key], s.[Value], GETUTCDATE() FROM @t s
WHERE NOT EXISTS (SELECT 1 FROM Translations x WHERE x.[Language] = s.[Language] AND x.[Key] = s.[Key]);
PRINT CONCAT('Translations added: ', @@ROWCOUNT);
