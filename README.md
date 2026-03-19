# Riot Picker

Auto champion/agent selection tool for League of Legends and Valorant.

League of Legends ve Valorant icin otomatik sampiyon/ajan secim araci.

---

## Features / Ozellikler

### League of Legends
| English | Turkce |
|---------|--------|
| **Auto Accept** - Automatically accepts when a match is found | **Otomatik Mac Kabul** - Mac bulundugunda otomatik kabul |
| **Auto Champion Pick** - Picks and locks champions based on role-specific priority lists | **Otomatik Sampiyon Secimi** - Rol bazli oncelik listesi ile otomatik pick ve kilitleme |
| **Auto Ban** - Bans champions in priority order | **Otomatik Yasaklama** - Oncelik sirasina gore otomatik ban |
| **Auto Rune Page** - Selects the rune page matching the locked champion's name | **Otomatik Run Sayfasi** - Kilitlenen sampiyonun adini iceren run sayfasini otomatik secer |

### Valorant
| English | Turkce |
|---------|--------|
| **Auto Agent Select** - Selects and locks the first available agent from your priority list | **Otomatik Ajan Secimi** - Oncelik listesindeki ilk musait ajani otomatik secer ve kilitler |

### General / Genel
- **Drag & Drop / Surukle-Birak** - Reorder priority lists by dragging / Oncelik listelerinde surukleme ile siralama
- **Language / Dil** - English / Turkce
- **Single .exe / Tek .exe** - No installation, zero dependencies / Kurulum gerektirmez, sifir bagimlilik
- **Low Resource Usage / Dusuk Kaynak Tuketimi** - Adaptive polling, minimum CPU/RAM

---

## Installation / Kurulum

1. Download `RiotPicker.exe` from [Releases](../../releases) / [Releases](../../releases) sayfasindan `RiotPicker.exe` indirin
2. Run it - no setup needed / Calistirin - kurulum gerekmez

## Usage / Kullanim

1. Open LoL or Valorant client / LoL veya Valorant istemcisini acin
2. Run RiotPicker / RiotPicker'i calistirin
3. Enable LoL/VAL toggles / LoL/VAL toggle'larini acin
4. Set up your priority lists / Oncelik listelerinizi olusturun
5. Wait for a match - RiotPicker handles the rest / Maci bekleyin - gerisini RiotPicker halleder

## Build / Derleme

```bash
# Debug
dotnet run --project RiotPicker

# Release (single exe / tek exe)
dotnet publish RiotPicker/RiotPicker.csproj -c Release -o ./publish
```

## Tech Stack / Teknoloji

- **C# / .NET 8** + **Avalonia UI 11**
- Self-contained single-file publish (~38MB)
- LCU API (LoL) + Valorant Local API

## License / Lisans

MIT
