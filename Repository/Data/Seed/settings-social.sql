/*
  Footer-dakı sosial şəbəkə linkləri (Settings cədvəli). Təkrar işlətsən zərər yoxdur.
  Facebook və LinkedIn: Code Academy-nin səhifələri; Instagram: efendiyev.006.
  X (Twitter): Code Academy-nin rəsmi X hesabı tapılmadı, ona görə akademiyanın saytı qoyulub; hesab olsa admin panelində Settings səhifəsindən dəyiş.
  Açar yoxdursa əlavə edir; varsa YALNIZ ilkin boş ünvanı (şəbəkənin ana səhifəsini) əvəz edir, admin yazdığı ünvana toxunmur.
*/
INSERT INTO Settings ([Key], [Value])
SELECT v.[Key], v.[Value]
FROM (VALUES
  (N'InstagramUrl', N'https://www.instagram.com/efendiyev.006/'),
  (N'FacebookUrl',  N'https://www.facebook.com/code.edu.az/'),
  (N'TwitterUrl',   N'https://code.edu.az/'),
  (N'LinkedInUrl',  N'https://www.linkedin.com/school/code.edu.az/')
) AS v([Key], [Value])
WHERE NOT EXISTS (SELECT 1 FROM Settings s WHERE s.[Key] = v.[Key]);

UPDATE s SET s.[Value] = v.[Value]
FROM Settings s
JOIN (VALUES
  (N'InstagramUrl', N'https://www.instagram.com/', N'https://www.instagram.com/efendiyev.006/'),
  (N'FacebookUrl',  N'https://www.facebook.com/', N'https://www.facebook.com/code.edu.az/'),
  (N'TwitterUrl',   N'https://x.com/',            N'https://code.edu.az/'),
  (N'LinkedInUrl',  N'https://www.linkedin.com/', N'https://www.linkedin.com/school/code.edu.az/')
) AS v([Key], [Old], [Value]) ON s.[Key] = v.[Key] AND s.[Value] = v.[Old];
