-- Yaratma və redaktə səhifələrində üç dildə birlikdə saxlama üçün mətnlər (az, ru). js: ilə başlayanlar JavaScript mətnləridir. Təkrar işlədilə bilər.
SET NOCOUNT ON;
DECLARE @t TABLE ([Language] nvarchar(5), [Key] nvarchar(400), [Value] nvarchar(2000));
INSERT INTO @t VALUES (N'az', N'Write the English text, then pick AZ or RU to write the same text in that language. One button saves all three languages.', N'İngilis mətni yazın, sonra eyni mətni həmin dildə yazmaq üçün AZ və ya RU seçin. Bir düymə üç dili də yadda saxlayır.');
INSERT INTO @t VALUES (N'ru', N'Write the English text, then pick AZ or RU to write the same text in that language. One button saves all three languages.', N'Напишите английский текст, затем выберите AZ или RU, чтобы написать тот же текст на этом языке. Одна кнопка сохраняет все три языка.');
INSERT INTO @t VALUES (N'az', N'js:Editing the {0} translation. The English text is shown as a hint. Leave a field empty to show the English text.', N'{0} mətni redaktə olunur. İngilis mətn işarə kimi göstərilir. İngilis mətnin görünməsi üçün sahəni boş buraxın. Yadda saxla düyməsi bütün dilləri saxlayır.');
INSERT INTO @t VALUES (N'ru', N'js:Editing the {0} translation. The English text is shown as a hint. Leave a field empty to show the English text.', N'Редактируется текст: {0}. Английский текст показан как подсказка. Оставьте поле пустым, чтобы показывался английский текст. Кнопка «Сохранить» сохраняет все языки.');
INSERT INTO @t VALUES (N'az', N'js:Your AZ and RU texts are kept. Fix the error and save again.', N'AZ və RU mətnləriniz saxlanılıb. Xətanı düzəldib yenidən yadda saxlayın.');
INSERT INTO @t VALUES (N'ru', N'js:Your AZ and RU texts are kept. Fix the error and save again.', N'Ваши тексты AZ и RU сохранены. Исправьте ошибку и сохраните снова.');
INSERT INTO Translations ([Language], [Key], [Value], CreatedAt)
SELECT s.[Language], s.[Key], s.[Value], GETUTCDATE() FROM @t s
WHERE NOT EXISTS (SELECT 1 FROM Translations x WHERE x.[Language] = s.[Language] AND x.[Key] = s.[Key]);
PRINT CONCAT('Translations added: ', @@ROWCOUNT);
