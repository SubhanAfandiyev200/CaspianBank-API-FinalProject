-- Admin redaktə səhifələrindəki dil seçimi (EN | AZ | RU) üçün mətnlər (az, ru). js: ilə başlayanlar JavaScript mətnləridir. Təkrar işlədilə bilər.
SET NOCOUNT ON;
DECLARE @t TABLE ([Language] nvarchar(5), [Key] nvarchar(400), [Value] nvarchar(2000));
INSERT INTO @t VALUES (N'az', N'Language of the text', N'Mətnin dili');
INSERT INTO @t VALUES (N'ru', N'Language of the text', N'Язык текста');
INSERT INTO @t VALUES (N'az', N'Pick a language to add or update the text in that language.', N'Mətni həmin dildə əlavə etmək və ya yeniləmək üçün dil seçin.');
INSERT INTO @t VALUES (N'ru', N'Pick a language to add or update the text in that language.', N'Выберите язык, чтобы добавить или обновить текст на этом языке.');
INSERT INTO @t VALUES (N'az', N'js:Loading...', N'Yüklənir...');
INSERT INTO @t VALUES (N'ru', N'js:Loading...', N'Загрузка...');
INSERT INTO @t VALUES (N'az', N'js:Saving...', N'Yadda saxlanır...');
INSERT INTO @t VALUES (N'ru', N'js:Saving...', N'Сохранение...');
INSERT INTO @t VALUES (N'az', N'js:Editing the {0} translation. The English text is shown as a hint. Leave a field empty to show the English text.', N'{0} tərcüməsi redaktə olunur. İngilis mətn işarə kimi göstərilir. İngilis mətnin görünməsi üçün sahəni boş buraxın.');
INSERT INTO @t VALUES (N'ru', N'js:Editing the {0} translation. The English text is shown as a hint. Leave a field empty to show the English text.', N'Редактируется перевод: {0}. Английский текст показан как подсказка. Оставьте поле пустым, чтобы показывался английский текст.');
INSERT INTO @t VALUES (N'az', N'js:The translations can''t be loaded right now. Please try again in a moment.', N'Tərcümələri hazırda yükləmək mümkün deyil. Bir azdan yenidən cəhd edin.');
INSERT INTO @t VALUES (N'ru', N'js:The translations can''t be loaded right now. Please try again in a moment.', N'Сейчас не удаётся загрузить переводы. Повторите через минуту.');
INSERT INTO @t VALUES (N'az', N'js:The translation was saved.', N'Tərcümə yadda saxlandı.');
INSERT INTO @t VALUES (N'ru', N'js:The translation was saved.', N'Перевод сохранён.');
INSERT INTO @t VALUES (N'az', N'js:Something went wrong. Please try again.', N'Xəta baş verdi. Yenidən cəhd edin.');
INSERT INTO @t VALUES (N'ru', N'js:Something went wrong. Please try again.', N'Что-то пошло не так. Повторите попытку.');
INSERT INTO Translations ([Language], [Key], [Value], CreatedAt)
SELECT s.[Language], s.[Key], s.[Value], GETUTCDATE() FROM @t s
WHERE NOT EXISTS (SELECT 1 FROM Translations x WHERE x.[Language] = s.[Language] AND x.[Key] = s.[Key]);
PRINT CONCAT('Translations added: ', @@ROWCOUNT);
