# VS Code Source Control Test Trigger

Bu dosya VS Code içinde Source Control görünürlüğünü tetiklemek için eklendi.

Eğer VS Code Source Control panelinde görünmüyorsa, lütfen aşağıdaki adımları normal macOS Terminal'inde çalıştırın ve çıktıları buraya yapıştırın:

```bash
cd /Users/dilarasara/katana
# Git durumunu göster
git status --porcelain=2 --branch
# Lokal branch adı
git branch --show-current
# Değişiklikleri ekle ve commit yap (isteğe bağlı, sadece test için)
# git add VSCode_test_trigger.md
# git commit -m "Test: add VSCode_test_trigger.md"
```

Ayrıca VS Code'u eklentisiz başlatıp kontrol etmek için:

```bash
open -a "Visual Studio Code" --args --disable-extensions
```

Eğer Source Control hâlâ görünmüyorsa, `View → Output` içindeki `Git` logunu ve `Help → Toggle Developer Tools → Console` içindeki hata mesajlarını buraya yapıştırın.

---

Not: Bu dosya otomatik olarak oluşturuldu; isterseniz test commit'ini yapabilir veya dosyayı silebilirsiniz.