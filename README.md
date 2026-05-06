# 🧰 LGS Toolbox

**LGS Toolbox** est un petit package Unity réutilisable regroupant les helpers des projets LGS.

Le but est de garder une base simple à importer dans plusieurs projets : audio, musique, logs, stats, debug console, fullscreen editor, Newgrounds, save state, Google Forms / Sheets, etc.

---

## 📦 Installation via Unity Package Manager

Dans Unity :

```txt
https://github.com/DorianSanjivy/LGS_toolbox.git
```

---

## 🔧 Fonctionnalités principales

* 🔊 **SoundManager**
  → Joue des sons par nom avec volume, pitch et anti-spam intégré

* 🎵 **MusicManager**
  → Gère la musique persistante entre scènes avec transition ou changement instantané

* 💬 **TypewriterSound**
  → Joue un son à chaque caractère affiché avec `TMPWriter`

* 📝 **LogManager**
  → Enregistre les logs Unity dans un fichier `.log`, avec support des changements de scènes

* 📈 **StatsManager**
  → Collecte des stats de session et peut les envoyer vers Google Forms / Sheets

* 📡 **GoogleFormSink**
  → Envoie les données vers un Google Form connecté à un Google Sheet

* 🌐 **GoogleSheetsDataFetcher**
  → Récupère du texte ou du CSV depuis une URL Google Sheets / Apps Script

* 🎖️ **NewgroundsAPI**
  → Wrapper simple pour médailles, scores, login et session Newgrounds

* 🧪 **RuntimeDebugConsoleBridge**
  → Ajoute une console debug runtime avec commandes simples

* 💾 **SaveStateSystem**
  → Système générique pour sauvegarder/restaurer un état

* 🖥️ **EditorFullscreenController**
  → Active/désactive le fullscreen dans l’éditeur Unity

---

## 🔊 SoundManager

Permet de jouer un son à partir de son nom.

### Utilisation

```csharp
using LGSToolbox;

SoundManager.Play("Jump");
SoundManager.Play("Jump", volume: 0.5f);
SoundManager.Play("Jump", pitch: 1.2f);
SoundManager.Play("Jump", volume: 0.5f, pitch: 1.2f);

SoundManager.Stop("Jump");
```

### Setup Unity

Créer un GameObject :

```txt
[LGS] Sound Manager
└── SoundManager
```

Puis configurer la liste `Sounds` dans l’inspecteur :

```txt
Name: Jump
Audios: AudioSource_Jump
```

Le nom utilisé dans le code doit correspondre au nom défini dans l’inspecteur.

---

## 🎵 MusicManager

Gère une musique de fond persistante entre les scènes.

### Utilisation depuis Resources

Le fichier doit être dans :

```txt
Assets/Resources/Music/main_loop.wav
```

Puis :

```csharp
using LGSToolbox;

MusicManager.PlayFromResources("Music/main_loop");
MusicManager.PlayFromResources("Music/shop_loop", transition: true, fadeDuration: 2f);
MusicManager.PlayFromResources("Music/boss_loop", transition: false);

MusicManager.Stop(fade: true);
MusicManager.SetVolume(0.1f);
```

Le `MusicManager` s’auto-crée si aucun objet n’existe dans la scène.

---

## 💬 TypewriterSound

Permet de jouer un son à chaque caractère affiché par `TMPWriter`.

### Dépendance

Nécessite :

```txt
TMPEffects
```

### Setup

Sur ton texte :

```txt
DialogueText
├── TextMeshProUGUI
├── TMPWriter
└── TypewriterSound
```

Dans `TypewriterSound`, configurer :

```txt
Voice Key: Billy
Pitch Range: 0.95 / 1.05
Volume: 1
Ignore Spaces: true
Ignore Punctuation: true
```

Le `Voice Key` doit correspondre à un son enregistré dans `SoundManager`.

---

## 📝 LogManager

Écrit les logs Unity dans un fichier `.log`.

### Fonctionnalités

* Logs persistants sur fichier
* Marqueurs de changement de scène
* Option pour inclure les warnings
* Option pour inclure les stack traces
* Rotation automatique des anciens logs
* Persiste entre les scènes

### Setup

Créer un GameObject :

```txt
[LGS] Log Manager
└── LogManager
```

Récupérer le chemin du log :

```csharp
using LGSToolbox;

string path = LogManager.GetCurrentLogPath();
```

---

## 📈 StatsManager + GoogleFormSink

Système simple pour collecter des stats et les envoyer vers un Google Form.

Le flow recommandé :

```txt
Unity → Google Form → Google Sheet
```

### Setup

Créer un asset :

```txt
Right click > Create > LGS Toolbox > Stats > Google Form Sink
```

Le placer par exemple ici :

```txt
Assets/Config/Stats/GoogleFormSink_Playtest.asset
```

Puis créer dans la première scène :

```txt
[LGS] Stats Manager
└── StatsManager
```

Assigner le `GoogleFormSink` dans le champ `Sink Asset`.

### Exemple d’utilisation

```csharp
using UnityEngine;
using LGSToolbox;

public class StatsTest : MonoBehaviour
{
    private void Start()
    {
        StatsManager.I.SetPlayerName("Yanis");
        StatsManager.I.SetInt("score", 1500);
        StatsManager.I.SetBool("won", true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            StatsManager.I.SendAll();
        }
    }
}
```

### Mapping Google Form

Dans le `GoogleFormSink`, mapper les champs :

```txt
Google Form Entry: entry.123456789
Stat Name: sessionId

Google Form Entry: entry.987654321
Stat Name: version

Google Form Entry: entry.555555555
Stat Name: score
```

Les `Stat Name` peuvent être :

* Des champs de `StatsSession`
* Des custom stats ajoutées avec `StatsManager.I.SetInt(...)`, `SetFloat(...)`, `SetBool(...)`, etc.

---

## 🌐 GoogleSheetsDataFetcher

Permet de récupérer du texte ou du CSV depuis une URL.

```csharp
using UnityEngine;
using LGSToolbox;

public class SheetFetchTest : MonoBehaviour
{
    [SerializeField] private string sheetCsvUrl;

    private void Start()
    {
        StartCoroutine(GoogleSheetsDataFetcher.FetchText(
            sheetCsvUrl,
            OnLoaded,
            OnError
        ));
    }

    private void OnLoaded(string text)
    {
        Debug.Log(text);
    }

    private void OnError(string error)
    {
        Debug.LogWarning(error);
    }
}
```

---

## 🎖️ NewgroundsAPI

Wrapper simple autour de l’API Newgrounds.

### Dépendance

Nécessite le package :

```txt
https://github.com/PsychoGoldfishNG/NewgroundsIO-Unity.git
```

Dans Unity :

```txt
Window > Package Manager > + > Add package from git URL
```

Puis coller l’URL ci-dessus.

### Setup

Créer un GameObject :

```txt
[LGS] Newgrounds API
└── NewgroundsAPI
```

Puis configurer :

```txt
App ID
AES Key
Version
Initialize On Start
```

### Utilisation

```csharp
using LGSToolbox;

NewgroundsAPI.Instance.PostScore(boardId, score);
NewgroundsAPI.Instance.UnlockMedal(medalId);
NewgroundsAPI.OpenLoginPage();
```

---

## 🧪 RuntimeDebugConsoleBridge

Ajoute des commandes simples à la console runtime.

### Dépendance

Nécessite :

```txt
yasirkula / UnityIngameDebugConsole
```

Repo :

```txt
https://github.com/yasirkula/UnityIngameDebugConsole
```

### Commandes incluses

```txt
help              -> Show this help
scene             -> Show the active scene name
reload            -> Reload the active scene
timescale [Float] -> Set Time.timeScale
logpath           -> Show current log file path
show              -> Show the debug console
hide              -> Hide the debug console
```

### Ajouter une commande spécifique au projet

Créer un script dans ton projet, pas dans le package :

```csharp
using UnityEngine;
using IngameDebugConsole;
using LGSToolbox;

public class GameDebugCommands : MonoBehaviour
{
    private void OnEnable()
    {
        DebugLogConsole.AddCommand("hello", "Log hello", Hello);
        RuntimeDebugConsoleBridge.RegisterHelpEntry("hello", "Log hello");
    }

    private void OnDisable()
    {
        DebugLogConsole.RemoveCommand("hello");
        RuntimeDebugConsoleBridge.UnregisterHelpEntry("hello");
    }

    private string Hello()
    {
        return "hello";
    }
}
```

Ensuite, dans la console :

```txt
hello
```

---

## 💾 SaveStateSystem

Système générique pour créer des sauvegardes de debug.

Le package fournit :

```txt
SaveStateSystem
ISaveStateProvider
SaveStateProviderBase<TState>
```
 
Il sauvegarde seulement les providers que ajouté dans le projet.

### Exemple de provider projet

```csharp
using System;
using LGSToolbox;

public class PlayerSaveProvider : SaveStateProviderBase<PlayerSaveProvider.PlayerState>
{
    public override string SaveKey => "player";

    [Serializable]
    public class PlayerState
    {
        public int health;
        public int money;
    }

    protected override PlayerState CaptureState()
    {
        return new PlayerState
        {
            health = 100,
            money = 50
        };
    }

    protected override void RestoreState(PlayerState state)
    {
        if (state == null)
            return;

        // Apply state to your game here
    }
}
```

### Utilisation

```csharp
using LGSToolbox;

SaveStateSystem.SaveNow();
SaveStateSystem.LoadNow();
SaveStateSystem.Clear();
```

---

## 🖥️ EditorFullscreenController

Permet d’ouvrir le Game View Unity en fullscreen dans l’éditeur.

### Setup

Créer un GameObject :

```txt
[LGS] Editor Fullscreen Controller
└── EditorFullscreenController
```

Options :

```txt
Fullscreen On Start
Allow Toggle
Toggle Key
Persist Across Scenes
```

Le fullscreen est réservé à l’éditeur Unity et n’existe pas en build.

---

## ⚙️ Dépendances optionnelles

Certains scripts dépendent de plugins externes.  
Si tu n’utilises pas ces modules, tu peux les retirer ou les désactiver.

| Module | Dépendance | Utilité |
|---|---|---|
| `TypewriterSound` | TMPEffects | Son par caractère avec `TMPWriter` |
| `NewgroundsAPI` | NewgroundsIO Unity | Médailles, scores, login |
| `RuntimeDebugConsoleBridge` | IngameDebugConsole | Console debug runtime |
| `EditorFullscreenController` | UnityEditor | Fullscreen Game View dans l’éditeur |
