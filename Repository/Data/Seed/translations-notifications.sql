-- Bildiriş mətnləri (az, ru). Açar şablondur: {0}, {1} yerinə real dəyərlər (kart nömrəsinin son 4 rəqəmi, məbləğ, kredit kodu və s.) qoyulur.
-- MVC bu şablonları gələn real mətnə uyğunlaşdırır (Services/Translator.cs). Təkrar işlədilə bilər: olan tərcümələri dəyişmir.
SET NOCOUNT ON;
DECLARE @t TABLE ([Language] nvarchar(5), [Key] nvarchar(400), [Value] nvarchar(2000));

-- Kartlar
INSERT INTO @t VALUES (N'az', N'Your card •••• {0} was blocked. You can unblock it with a code sent to your email.', N'•••• {0} kartınız bloklandı. Emailinizə gələn kodla blokdan çıxara bilərsiniz.');
INSERT INTO @t VALUES (N'ru', N'Your card •••• {0} was blocked. You can unblock it with a code sent to your email.', N'Ваша карта •••• {0} заблокирована. Разблокировать её можно кодом, отправленным на ваш email.');
INSERT INTO @t VALUES (N'az', N'Your card •••• {0} is active again.', N'•••• {0} kartınız yenidən aktivdir.');
INSERT INTO @t VALUES (N'ru', N'Your card •••• {0} is active again.', N'Ваша карта •••• {0} снова активна.');
INSERT INTO @t VALUES (N'az', N'{0} AZN was taken from your card •••• {1} to top up another card.', N'•••• {1} kartınızdan başqa kartı artırmaq üçün {0} AZN çıxıldı.');
INSERT INTO @t VALUES (N'ru', N'{0} AZN was taken from your card •••• {1} to top up another card.', N'С вашей карты •••• {1} списано {0} AZN для пополнения другой карты.');

-- Köçürmə
INSERT INTO @t VALUES (N'az', N'You received {0} AZN from {1} on your card •••• {2}.', N'•••• {2} kartınıza {1} tərəfindən {0} AZN köçürüldü.');
INSERT INTO @t VALUES (N'ru', N'You received {0} AZN from {1} on your card •••• {2}.', N'На вашу карту •••• {2} поступило {0} AZN от {1}.');
INSERT INTO @t VALUES (N'az', N'{0} was sent to {1}.', N'{0} {1} ünvanına göndərildi.');
INSERT INTO @t VALUES (N'ru', N'{0} was sent to {1}.', N'{0} отправлено получателю {1}.');
INSERT INTO @t VALUES (N'az', N'We couldn''t send {0} to {1}: {2} We will try again tomorrow.', N'{0} məbləğini {1} ünvanına göndərə bilmədik: {2} Sabah yenidən cəhd edəcəyik.');
INSERT INTO @t VALUES (N'ru', N'We couldn''t send {0} to {1}: {2} We will try again tomorrow.', N'Не удалось отправить {0} получателю {1}: {2} Мы повторим попытку завтра.');

-- Aylıq ödənişlər
INSERT INTO @t VALUES (N'az', N'Loan {0} is closed, so its monthly payment was stopped.', N'{0} saylı kredit bağlandığı üçün onun aylıq ödənişi dayandırıldı.');
INSERT INTO @t VALUES (N'ru', N'Loan {0} is closed, so its monthly payment was stopped.', N'Кредит {0} закрыт, поэтому его ежемесячный платёж остановлен.');
INSERT INTO @t VALUES (N'az', N'{0} AZN was paid to {1} ({2}) from card {3}.', N'{3} kartından {1} ({2}) üçün {0} AZN ödənildi.');
INSERT INTO @t VALUES (N'ru', N'{0} AZN was paid to {1} ({2}) from card {3}.', N'С карты {3} оплачено {0} AZN: {1} ({2}).');
INSERT INTO @t VALUES (N'az', N'{0} ({1}): {2} We will try again tomorrow.', N'{0} ({1}): {2} Sabah yenidən cəhd edəcəyik.');
INSERT INTO @t VALUES (N'ru', N'{0} ({1}): {2} We will try again tomorrow.', N'{0} ({1}): {2} Мы повторим попытку завтра.');

-- Depozitlər
INSERT INTO @t VALUES (N'az', N'{0} is locked for {1} months at {2}% a year. You will get {3} on {4}.', N'{0} {1} ay müddətinə ildə {2}% ilə bağlandı. {4} tarixində {3} alacaqsınız.');
INSERT INTO @t VALUES (N'ru', N'{0} is locked for {1} months at {2}% a year. You will get {3} on {4}.', N'{0} размещено на {1} мес. под {2}% годовых. {4} вы получите {3}.');
INSERT INTO @t VALUES (N'az', N'{0} ({1} + {2} interest) was returned to your card •••• {3}.', N'{0} ({1} + {2} faiz) •••• {3} kartınıza qaytarıldı.');
INSERT INTO @t VALUES (N'ru', N'{0} ({1} + {2} interest) was returned to your card •••• {3}.', N'{0} ({1} + {2} процентов) возвращено на вашу карту •••• {3}.');

-- Kreditlər
INSERT INTO @t VALUES (N'az', N'We can approve {0} AZN instead of {1} AZN for loan {2}. Open the loan to accept or decline.', N'{2} saylı kredit üçün {1} AZN əvəzinə {0} AZN təsdiqləyə bilərik. Qəbul etmək və ya imtina etmək üçün krediti açın.');
INSERT INTO @t VALUES (N'ru', N'We can approve {0} AZN instead of {1} AZN for loan {2}. Open the loan to accept or decline.', N'Мы можем одобрить {0} AZN вместо {1} AZN по кредиту {2}. Откройте кредит, чтобы принять или отклонить.');
INSERT INTO @t VALUES (N'az', N'{0} AZN for loan {1} was sent to your card.', N'{1} saylı kredit üzrə {0} AZN kartınıza köçürüldü.');
INSERT INTO @t VALUES (N'ru', N'{0} AZN for loan {1} was sent to your card.', N'{0} AZN по кредиту {1} переведено на вашу карту.');
INSERT INTO @t VALUES (N'az', N'We can''t approve your loan application right now. You can apply again later.', N'Kredit müraciətinizi hazırda təsdiqləyə bilmirik. Sonra yenidən müraciət edə bilərsiniz.');
INSERT INTO @t VALUES (N'ru', N'We can''t approve your loan application right now. You can apply again later.', N'Мы не можем одобрить вашу заявку на кредит сейчас. Вы сможете подать её позже.');
INSERT INTO @t VALUES (N'az', N'A payment of {0} AZN was made on your loan {1} from another account.', N'{1} saylı kreditinizə başqa hesabdan {0} AZN ödəniş edildi.');
INSERT INTO @t VALUES (N'ru', N'A payment of {0} AZN was made on your loan {1} from another account.', N'По вашему кредиту {1} с другого счёта внесён платёж {0} AZN.');
INSERT INTO @t VALUES (N'az', N'Your loan {0} is fully paid. Thank you.', N'{0} saylı krediti tam ödədiniz. Təşəkkür edirik.');
INSERT INTO @t VALUES (N'ru', N'Your loan {0} is fully paid. Thank you.', N'Ваш кредит {0} полностью погашен. Спасибо.');
INSERT INTO @t VALUES (N'az', N'Your offer for loan {0} expired. You can apply again.', N'{0} saylı kredit üzrə təklifin vaxtı bitdi. Yenidən müraciət edə bilərsiniz.');
INSERT INTO @t VALUES (N'ru', N'Your offer for loan {0} expired. You can apply again.', N'Срок предложения по кредиту {0} истёк. Вы можете подать заявку снова.');
INSERT INTO @t VALUES (N'az', N'Your payment of {0} AZN for loan {1} is due on {2}.', N'{1} saylı kredit üzrə {0} AZN ödənişinin tarixi {2}-dir.');
INSERT INTO @t VALUES (N'ru', N'Your payment of {0} AZN for loan {1} is due on {2}.', N'Платёж {0} AZN по кредиту {1} нужно внести до {2}.');
INSERT INTO @t VALUES (N'az', N'Your payment for loan {0} was due on {1}. {2} AZN is due now and a penalty grows every day until you pay.', N'{0} saylı kredit üzrə ödəniş {1} tarixində idi. İndi {2} AZN ödənilməlidir və ödəyənə qədər hər gün cərimə artır.');
INSERT INTO @t VALUES (N'ru', N'Your payment for loan {0} was due on {1}. {2} AZN is due now and a penalty grows every day until you pay.', N'Платёж по кредиту {0} нужно было внести {1}. Сейчас к оплате {2} AZN, пеня растёт каждый день до оплаты.');
INSERT INTO @t VALUES (N'az', N'Loan {0} has been overdue for {1} days and was passed to court. Please pay what is due as soon as possible.', N'{0} saylı kredit {1} gündür gecikir və məhkəməyə verildi. Borcu tez bir zamanda ödəyin.');
INSERT INTO @t VALUES (N'ru', N'Loan {0} has been overdue for {1} days and was passed to court. Please pay what is due as soon as possible.', N'Кредит {0} просрочен на {1} дн. и передан в суд. Пожалуйста, как можно скорее погасите задолженность.');

-- Dəstək və şikayətlər
INSERT INTO @t VALUES (N'az', N'Re: {0}', N'Cavab: {0}');
INSERT INTO @t VALUES (N'ru', N'Re: {0}', N'Re: {0}');
INSERT INTO @t VALUES (N'az', N'We reviewed your report about {0} and found no problem.', N'{0} barədə şikayətinizə baxdıq və problem tapmadıq.');
INSERT INTO @t VALUES (N'ru', N'We reviewed your report about {0} and found no problem.', N'Мы рассмотрели ваше сообщение об операции {0} и проблем не обнаружили.');
INSERT INTO @t VALUES (N'az', N'We reviewed your report about {0} and took action.', N'{0} barədə şikayətinizə baxdıq və tədbir gördük.');
INSERT INTO @t VALUES (N'ru', N'We reviewed your report about {0} and took action.', N'Мы рассмотрели ваше сообщение об операции {0} и приняли меры.');
INSERT INTO @t VALUES (N'az', N'We reviewed your report about {0}.', N'{0} barədə şikayətinizə baxdıq.');
INSERT INTO @t VALUES (N'ru', N'We reviewed your report about {0}.', N'Мы рассмотрели ваше сообщение об операции {0}.');

INSERT INTO Translations ([Language], [Key], [Value], CreatedAt)
SELECT s.[Language], s.[Key], s.[Value], GETUTCDATE() FROM @t s
WHERE NOT EXISTS (SELECT 1 FROM Translations x WHERE x.[Language] = s.[Language] AND x.[Key] = s.[Key]);
PRINT CONCAT('Translations added: ', @@ROWCOUNT);
