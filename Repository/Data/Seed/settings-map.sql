/*
  Contact us səhifəsindəki xəritənin mərkəzi (Settings cədvəli): enlik və uzunluq, nöqtə ilə.
  Hazırda Nizami küçəsi, Bakı. Admin Settings səhifəsindən dəyişir (Google Maps-də yerə sağ klik edib koordinatı kopyalayın).
  Təkrar işlətsən zərər yoxdur: artıq olan açara toxunmur.
*/
INSERT INTO Settings ([Key], [Value])
SELECT v.[Key], v.[Value]
FROM (VALUES
  (N'MapLatitude',  N'40.3730'),
  (N'MapLongitude', N'49.8370')
) AS v([Key], [Value])
WHERE NOT EXISTS (SELECT 1 FROM Settings s WHERE s.[Key] = v.[Key]);
