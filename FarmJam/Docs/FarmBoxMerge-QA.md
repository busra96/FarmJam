# FarmBoxMerge test ve düzenleme özeti

Tarih: 8 Ekim 2026. Unity 6000.3.8f1, Editor Play Mode, Android hedefi.

## Düzeltilenler

- Kart bırakma artık ilk uygun alanı değil, işaretçinin yakınındaki alanı seçiyor. Dolu veya yanlış boyutlu hedef, kartı geri gönderiyor ve kısa açıklama gösteriyor.
- Merge animasyonu sırasında kaynak ve hedef rezerve ediliyor; aynı karta eşzamanlı ikinci merge engelleniyor. Hedef yok olursa rezervasyon temizleniyor.
- Ayarlar paneli açıkken oyun girdisi kapalı; kapatınca yalnızca panelin kilidi kaldırılıyor. Win/fail kilidi korunuyor.
- Deste tükenmiş ve geçerli hamle kalmamışsa, kart tahtası dolu olmasa da fail sayacı çalışıyor. Uygulama askıya alındığında sonuç sayacı ilerlemiyor.
- Aktif kutular kayıt sırasıyla dolduruluyor; aynı renkli gruplarda yerleşme sırası kararlı.
- Geç veya tekrarlanan reklam callback'leri ve dispose sonrasındaki işlemler korunuyor. Reklam oynarken tekrar yükleme yapılmıyor.
- Analytics servisleri izin verilmeden başlatılmıyor; kapanmış oturuma geç async dönüş engelleniyor. Üç mevcut level eventi korunuyor.
- Güvenli alanın yatay düzenlemesi, üst/alt inset'leri artık sıfırlamıyor. Settings butonu üst ve sağ güvenli alana uyuyor.
- Mevcut kart başlığına merge formülü ve kısa etkileşim açıklamaları eklendi; yeni Canvas oluşturulmadı. İki görsel kurulum aracının eski sahne yolu düzeltildi.

## Doğrulananlar

- 35/35 bölüm: yazılı çözüm akışı, gerçek kart merge ve sürükle/bırak API'leri, en fazla 12 kart, kart yenileme, item sıçrama, kutu temizlenme, win ve sonuç sonrası input kilidi.
- 35 bölümün slot planı ve bağımsız 12 kartlık deste simülasyonu.
- 16 kutu şekli: bağlantılı hücreler, tekil hücreler, en fazla iki kutu genişliği.
- Ayrı 3 saniye win / 5 saniye fail süreleri; sonuç geldiğinde ayar panelinin kapanması.
- Retry başlangıç kart sırası ve birer ücretsiz Add/Trash hakkının sıfırlanması.
- Ayarların izole test anahtarlarında kaydedilip okunması, ses/müzik/haptic durumu ve ses seviyesi sınırları. Oyuncunun gerçek ayar ve ilerleme kayıtları değiştirilmedi.
- Reklam callback simülasyonu: iptal, erken kapatma, tamamlama, kapanma/ödül sıralaması, dispose ve tek seferlik sonuç.
- Mevcut ana menü Canvas'ındaki Play butonuna gerçek UI tıklamasıyla basıldı; kayıtlı 7. bölüm açıldı, item ve kartlar geldi. İlerleme kaydı değiştirilmedi.

| Game View | Dikey FOV | UI, ilk item ve görünür kutu sınırları |
| --- | --- | --- |
| 1080 × 1920 | 50° | Geçti |
| 828 × 1792 | 59.17° | Geçti |
| 1080 × 2520 | 62.94° | Geçti |
| 1768 × 2208 | 50° | Geçti |
| 1536 × 2048 | 50° | Geçti |
| 1600 × 2560 | 50° | Geçti |

Çentik, yan inset ve alt hareket çubuğu ayrıca simüle edildi. Uzun telefon ve katlanır/tablet ekran görüntüleri görsel olarak incelendi. Ekran düzeltmesi sonrası presentation kontrolleri ayrı tekrarlandı; rapor önceki başarılı 35 bölüm turunu açıkça koruyor. Son regresyon konsol kontrolünde hata veya uyarı dönmedi.

## Tekrar çalıştırma

Sahneyi kaydedip Play Mode'dan çıkın. `Tools > FarmBoxMerge > Run Gameplay Regression` (F8) tam kontrolü başlatır. `Repeat Presentation Checks` başarılı bölüm turundan sonra ekran ve ana menü geçiş kontrollerini tekrarlar. Son raporda ana menü otomatik geçiş kontrolü de geçti.

Ham rapor ve görüntüler: `Temp/FarmBoxMergeTests/regression.txt` ve aynı klasördeki PNG dosyaları. Testten sonra başlangıç sahnesi ve Game View seçimi geri yüklenir. Test sırasında script/sahne düzenlemeyin veya Play Mode'u kesmeyin.

## İlk kurulum tutorial kontrolü

`Tools > FarmBoxMerge > Tutorial > Run Tutorial Checks` ilk bölümü izole edilmiş kayıt anahtarıyla çalıştırır. Mevcut level kaydı, ses/titreşim ayarları ve gerçek tutorial tamamlanma bayrağı değiştirilmez. Gerçek kart sürükleme/merge/kutu yerleştirme API'leriyle şu kontroller geçti:

- İki adet aynı renk `1` kartının her iki yönde merge edilmesi ve `2` kartının oluşması.
- Merge tamamlanmadan kutu yerleştirme ve tutorial boyunca çöp kullanımının engellenmesi; yanlış çöp bırakmanın hak tüketmeden kartı geri getirmesi.
- Add Card, Refresh ve üst Retry eylemlerinin geçici olarak kapatılması.
- Ayarlar açıkken el animasyonunun görünmemesi ve kapanınca rehberin devam etmesi.
- Tutorial yarıda retry olursa merge adımından yeniden başlaması.
- Yalnızca başarılı kutu yerleştirmeden sonra tamamlanma kaydı; yeni kayıt servisi oluşturulduğunda kaydın okunması.
- Tamamlanan tutorial'ın retry'da tekrar açılmaması; ilerlemiş oyuncuların ilk bölüme zorlanmaması.

Rapor `Temp/FarmBoxMergeTests/tutorial.txt`, iki adımın görüntüleri `tutorial-merge.png` ve `tutorial-place.png`. `Preview First-Time Tutorial` aynı izole kayıtlarla oynanabilir önizleme açar; Play Mode'dan çıkınca asıl sahneye dönülür. Tutorial yazıları, hareket süreleri ve el görselleri mevcut Canvas üzerindeki `FarmBoxMergeTutorialController` bileşeninden değiştirilebilir.

## Cihazda doğrulanması gerekenler

Bu kontrol her oyuncu hamlesinin veya her cihazın garantisi değildir; yazılı çözümlerin ve temel hata/kenar durumlarının Editor regresyonudur. Fiziksel Android cihazlarda dokunmatik, GPU/particle görünümü, bellek/ısınma, FPS, uygulamanın arka plana alınması ve donanımsal titreşim ayrıca test edilmelidir. Bu turda APK/AAB alınmadı.

Android LevelPlay App Key, Rewarded ve Interstitial Ad Unit ID alanları halen boş. Gerçek reklam testi için yayıncı hesabındaki doğru değerler girilmeli; Editor callback kontrolleri canlı reklam ağı doğrulaması değildir. Yayın öncesi izin/gizlilik akışı ve Analytics dashboard teslimi de ayrıca doğrulanmalı.
