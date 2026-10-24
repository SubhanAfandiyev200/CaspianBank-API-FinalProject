-- Əməliyyat təsvirləri, salamlama və kart yazıları üçün tərcümələr (az, ru).
-- Açar şablondur: {0}, {1} yerinə kart tipi, son 4 rəqəm və s. qoyulur (MVC: Helpers/TextTranslation.cs).
-- Təkrar işlədilə bilər: olan tərcümələri DƏYİŞMİR, yalnız çatmayanları əlavə edir.
SET NOCOUNT ON;
DECLARE @t TABLE ([Language] nvarchar(5), [Key] nvarchar(400), [Value] nvarchar(2000));
INSERT INTO @t VALUES (N'az', N'Valid', N'Etibarlıdır');
INSERT INTO @t VALUES (N'ru', N'Valid', N'Действует до');
INSERT INTO @t VALUES (N'az', N'Good morning', N'Sabahınız xeyir');
INSERT INTO @t VALUES (N'ru', N'Good morning', N'Доброе утро');
INSERT INTO @t VALUES (N'az', N'Good afternoon', N'Günortanız xeyir');
INSERT INTO @t VALUES (N'ru', N'Good afternoon', N'Добрый день');
INSERT INTO @t VALUES (N'az', N'Good evening', N'Axşamınız xeyir');
INSERT INTO @t VALUES (N'ru', N'Good evening', N'Добрый вечер');
INSERT INTO @t VALUES (N'az', N'Transfer commission', N'Köçürmə komissiyası');
INSERT INTO @t VALUES (N'ru', N'Transfer commission', N'Комиссия за перевод');
INSERT INTO @t VALUES (N'az', N'Transfer to your {0} card •••• {1}', N'{0} kartınıza köçürmə •••• {1}');
INSERT INTO @t VALUES (N'ru', N'Transfer to your {0} card •••• {1}', N'Перевод на вашу карту {0} •••• {1}');
INSERT INTO @t VALUES (N'az', N'Transfer from your {0} card •••• {1}', N'{0} kartınızdan köçürmə •••• {1}');
INSERT INTO @t VALUES (N'ru', N'Transfer from your {0} card •••• {1}', N'Перевод с вашей карты {0} •••• {1}');
INSERT INTO @t VALUES (N'az', N'Transfer to •••• {0}', N'•••• {0} kartına köçürmə');
INSERT INTO @t VALUES (N'ru', N'Transfer to •••• {0}', N'Перевод на карту •••• {0}');
INSERT INTO @t VALUES (N'az', N'Transfer to {0} •••• {1}', N'{0} kartına köçürmə •••• {1}');
INSERT INTO @t VALUES (N'ru', N'Transfer to {0} •••• {1}', N'Перевод на карту {0} •••• {1}');
INSERT INTO @t VALUES (N'az', N'Transfer from {0}', N'{0} tərəfindən köçürmə');
INSERT INTO @t VALUES (N'ru', N'Transfer from {0}', N'Перевод от {0}');
INSERT INTO @t VALUES (N'az', N'{0} card opening fee', N'{0} kartının açılış haqqı');
INSERT INTO @t VALUES (N'ru', N'{0} card opening fee', N'Плата за открытие карты {0}');
INSERT INTO @t VALUES (N'az', N'Cashback · {0}', N'Keşbek · {0}');
INSERT INTO @t VALUES (N'ru', N'Cashback · {0}', N'Кешбэк · {0}');
INSERT INTO @t VALUES (N'az', N'Term deposit · {0} months', N'Müddətli depozit · {0} ay');
INSERT INTO @t VALUES (N'ru', N'Term deposit · {0} months', N'Срочный вклад · {0} мес.');
INSERT INTO @t VALUES (N'az', N'Deposit returned · {0} months', N'Depozit qaytarıldı · {0} ay');
INSERT INTO @t VALUES (N'ru', N'Deposit returned · {0} months', N'Вклад возвращён · {0} мес.');
INSERT INTO @t VALUES (N'az', N'Deposit interest · {0}% a year', N'Depozit faizi · ildə {0}%');
INSERT INTO @t VALUES (N'ru', N'Deposit interest · {0}% a year', N'Проценты по вкладу · {0}% годовых');
INSERT INTO @t VALUES (N'az', N'Loan {0} paid out', N'{0} saylı kredit ödənildi');
INSERT INTO @t VALUES (N'ru', N'Loan {0} paid out', N'Кредит {0} выплачен');
INSERT INTO @t VALUES (N'az', N'Early payment on loan {0}', N'{0} saylı kredit üzrə vaxtından əvvəl ödəniş');
INSERT INTO @t VALUES (N'ru', N'Early payment on loan {0}', N'Досрочный платёж по кредиту {0}');
INSERT INTO @t VALUES (N'az', N'Loan {0} repaid in full', N'{0} saylı kredit tam ödənildi');
INSERT INTO @t VALUES (N'ru', N'Loan {0} repaid in full', N'Кредит {0} погашен полностью');
INSERT INTO @t VALUES (N'az', N'Payment on loan {0}', N'{0} saylı kredit üzrə ödəniş');
INSERT INTO @t VALUES (N'ru', N'Payment on loan {0}', N'Платёж по кредиту {0}');
INSERT INTO @t VALUES (N'az', N'This ID record can''t be used to open an account. If you think this is a mistake, please contact the bank.', N'Bu vəsiqə qeydi ilə hesab açmaq mümkün deyil. Səhv olduğunu düşünürsünüzsə, banka müraciət edin.');
INSERT INTO @t VALUES (N'ru', N'This ID record can''t be used to open an account. If you think this is a mistake, please contact the bank.', N'По этой записи удостоверения открыть счёт нельзя. Если вы считаете, что это ошибка, обратитесь в банк.');
INSERT INTO @t VALUES (N'az', N'We couldn''t find a citizen with these details. Check the FIN, ID card number, first name and last name and try again.', N'Bu məlumatlarla vətəndaş tapılmadı. FİN, vəsiqə nömrəsi, ad və soyadı yoxlayıb yenidən cəhd edin.');
INSERT INTO @t VALUES (N'ru', N'We couldn''t find a citizen with these details. Check the FIN, ID card number, first name and last name and try again.', N'Гражданин с такими данными не найден. Проверьте ФИН, номер удостоверения, имя и фамилию и повторите попытку.');
INSERT INTO @t VALUES (N'az', N'An account already exists for this person. Log in instead.', N'Bu şəxs üçün hesab artıq mövcuddur. Daxil olun.');
INSERT INTO @t VALUES (N'ru', N'An account already exists for this person. Log in instead.', N'Для этого человека счёт уже существует. Войдите в систему.');
INSERT INTO Translations ([Language], [Key], [Value], CreatedAt)
SELECT s.[Language], s.[Key], s.[Value], GETUTCDATE() FROM @t s
WHERE NOT EXISTS (SELECT 1 FROM Translations x WHERE x.[Language] = s.[Language] AND x.[Key] = s.[Key]);
PRINT CONCAT('Translations added: ', @@ROWCOUNT);
