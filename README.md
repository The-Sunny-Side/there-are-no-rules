# There Are No Rules

**ThereAreNoRules** è il progetto avanzato dal team **THE SUNNY SIDE** partecipante alla community **Unical Game Forge**, nata all’Università della Calabria per sperimentare e condividere idee di videogiochi fuori dagli schemi. Il progetto è vivo e aperto: qui teniamo traccia della visione creativa e degli asset Unity già disponibili per i playtest.

## Visione e concept

- **Titolo provvisorio:** _THERE ARE NO RULES_.
- **Pitch:** party game multiplayer competitivo per mobile in cui squadre di piloti precipitano su piste folli a bordo di veicoli impossibili, colpiscono gli avversari e sopravvivono a ostacoli sempre diversi.
- **Atmosfera:** caos arcade, humor slapstick e ritmo “wacky races”, con richiami a Mario Kart, Crash Team Racing, Looney Tunes, Zelda: Wind Waker e Fall Guys.

## Meccaniche principali

- **Controlli semplici:** il giocatore non accelera; sterza inclinando il telefono (giroscopio) e usa due pulsanti su schermo per attivare abilità, attacchi o furto di estensioni.
- **Collisioni fisiche:** scontri corpo a corpo leggibili, dalla “sedia da ufficio rotante” al bob d’assalto, con feedback visivi e vocali (“DEVASTANTE!!!”).
- **Veicoli modulari:** ogni mezzo nasce da layer combinabili:
  - **Base:** definisce la fisica di sliding (snowboard, cingolati, bob, ecc.).
  - **Corpo:** determina difesa e stabilità.
  - **Estensioni:** armi e gadget (arpione per grabbare, pugno a molla, lame laterali).
- **Progressione e crafting:** sblocca componenti, assembla configurazioni uniche, bilancia peso vs velocità e resistenza.
- **Danni dinamici:** il veicolo si deteriora visivamente; la corsa termina quando la struttura collassa.

## Ambientazioni e stile

- Tracciati stagionali (montagne innevate, deserti sabbiosi, foreste fiorite, paesaggi cupi e nebbiosi) più mappe extraterrestri e scenari surreali.
- Ogni bioma introduce ostacoli ambientali, mob e power-up dedicati.
- Direzione artistica cartoon/toon shading con onomatopee a schermo (“BANG!”, “SPLASH!”, “CRASH!”).
- Colonna sonora country swing/bluegrass, FX slapstick e cronista che commenta colpi e KO.

## Power-up e abilità

- Abilità collegate alle parti del veicolo: la base sblocca movimenti speciali, le estensioni garantiscono attacchi esclusivi e possono essere rubate.
- Booster di velocità sul tracciato, trigger dinamici e trappole contestuali.
- Possibilità di usare arpioni per tirare gli avversari a sé o spingerli fuori pista.

## Modalità di gioco

- Multiplayer competitivo locale o online, partite rapide da 3–5 minuti.
- Obiettivo: rimanere integri fino al traguardo o dominare la classifica infliggendo più danni possibili.

## Stato attuale della build Unity

- **Versione Unity:** `6000.2.10f1`.
- **Scene disponibili:** `MainScene`, `BaseLevel`, `LatticeHitScene`, `ScriptingMovementeScene`.
- **Script chiave:**
  - `SkiMovement.cs`: prototipo di fisica su pendii, rotazioni con A/D e salto W.
  - `SpawnPoint`: istanzia più giocatori con offset configurabile.
  - `LatticeHitTrigger`: applica deformazioni temporanee a oggetti con tag `Deformable` tramite il pacchetto `Lattice`.
  - `AudioManager` + `MainSceneManager`: gestiscono musica persistente e SFX one-shot.
  - `FinishPoint`/`StartPoint`: controllano spawn e arrivi nelle scene di test.
- **Active GameObject di riferimento:** `player 1` (Tag `Player`, Layer `Default`).

## Struttura della repo

```
Assets/
├── Scenes/                  # BaseLevel, MainScene, LatticeHitScene, ScriptingMovementScene
├── Scripts/                 # Movimento, audio, deformazioni, punti di interesse
├── Prefabs/                 # Veicoli, trigger, elementi scenario
├── Materials/, Textures/    # Materiali stylized e decal
├── Models/, Audios/         # Asset 3D e clip sonori
└── Settings/, Samples/      # Config generali e asset di esempio
```

## Setup rapido

1. Clona la repo o copiala nella directory gestita da Unity Hub.
2. In Unity Hub scegli **Add → Add project from disk** e seleziona `there-are-no-rules`.
3. Apri con Unity `6000.2.10f1`.
4. Carica la scena desiderata e verifica che il pacchetto `Lattice` sia presente se usi gli script di deformazione.

## Controlli temporanei su PC

Sebbene il target finale sia mobile, gli input attuali per i test in editor sono:

- `A`/`D`: ruota il veicolo.
- `W`: salto con impulso verticale + leggero boost in avanti (solo se `isGrounded`).
  La fisica è gestita tramite `Rigidbody`, quindi modificare mesh e colliders del terreno influisce subito sul feeling.

## Audio e feedback

- Registra i clip nello scriptable `Sound` dell’`AudioManager` e richiama:

```csharp
AudioManager.Instance.PlayBackground("background_menu", 0.5f);
AudioManager.Instance.PlayOneShot("notification_ok", 1.0f);
```

- Aggiungi cronista e onomatopee tramite Timeline/VFX per enfatizzare i colpi più spettacolari.

## Roadmap suggerita

1. Prototipare il sistema di veicoli modulari con editor tools per combinare layer.
2. Integrare power-up rubabili e abilità legate alle estensioni equipaggiate.
3. Testare multiplayer locale (split screen) e successivamente netcode per partite online.
4. Curare FX audio/visivi ad hoc per garantire leggibilità del caos in partita.

## Community

Per idee, contributi o playtest passa dal server Discord della **Unical Game Forge** o apri una issue/PR. Non ci sono regole… basta portare una nuova follia da provare!
