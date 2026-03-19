# Riot Picker

League of Legends ve Valorant icin otomatik sampiyon/ajan secim araci.

**[English README](README.md)**

## Ozellikler

### League of Legends
- **Otomatik Mac Kabul** - Mac bulundugunda otomatik kabul
- **Otomatik Sampiyon Secimi** - Rol bazli oncelik listesi ile otomatik pick ve kilitleme
- **Otomatik Yasaklama** - Oncelik sirasina gore otomatik ban
- **Otomatik Run Sayfasi** - Kilitlenen sampiyonun adini iceren run sayfasini otomatik secer

### Valorant
- **Otomatik Ajan Secimi** - Oncelik listesindeki ilk musait ajani otomatik secer ve kilitler

### Genel
- **Surukleme ile Siralama** - Oncelik listelerinde drag-and-drop
- **Dil Destegi** - Turkce / English
- **Tek .exe** - Kurulum gerektirmez, sifir bagimlilik
- **Dusuk Kaynak Tuketimi** - Adaptif polling ile minimum CPU/RAM kullanimi

## Kurulum

1. [Releases](../../releases) sayfasindan `RiotPicker.exe` dosyasini indirin
2. Calistirin - kurulum gerekmez

## Kullanim

1. LoL veya Valorant istemcisini acin
2. RiotPicker'i calistirin
3. LoL/VAL toggle'larini acin
4. Sampiyon/ajan oncelik listelerinizi olusturun
5. Macin baslamasini bekleyin - gerisini RiotPicker halleder

## Derleme

```bash
# Debug
dotnet run --project RiotPicker

# Release (tek exe)
dotnet publish RiotPicker/RiotPicker.csproj -c Release -o ./publish
```

## Teknoloji

- **C# / .NET 8** + **Avalonia UI 11**
- Self-contained single-file publish (~38MB)
- LCU API (LoL) + Valorant Local API

## Lisans

MIT
