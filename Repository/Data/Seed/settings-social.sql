/*
  Footer-dakı sosial şəbəkə linkləri (Settings cədvəli). Təkrar işlətsən zərər yoxdur: artıq olan açara toxunmur.
  Hələlik şəbəkələrin ana səhifəsidir; real hesab ünvanlarını admin panelində Settings səhifəsindən dəyiş.
*/
INSERT INTO Settings ([Key], [Value])
SELECT v.[Key], v.[Value]
FROM (VALUES
  (N'InstagramUrl', N'https://www.instagram.com/'),
  (N'FacebookUrl',  N'https://www.facebook.com/'),
  (N'TwitterUrl',   N'https://x.com/'),
  (N'LinkedInUrl',  N'https://www.linkedin.com/')
) AS v([Key], [Value])
WHERE NOT EXISTS (SELECT 1 FROM Settings s WHERE s.[Key] = v.[Key]);
