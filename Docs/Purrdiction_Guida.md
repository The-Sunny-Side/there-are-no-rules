# Guida Pratica a Purrdiction (PurrNet Prediction)

Questa guida spiega **in parole semplici** come usare Purrdiction in Unity:
- quali classi usare
- quali metodi override-are
- come fare setup
- come funzionano `IPredictedData`, input, state e lifecycle

Versione progetto: **Unity 6000.2.10f1**

---

## 1) Mental model: cosa fa Purrdiction

Purrdiction simula il gioco a tick su client e server per avere controllo fluido e correggere errori con rollback/replay.

In pratica:
1. Il client owner raccoglie input
2. Simula subito localmente (reattivo)
3. Il server verifica e manda stato/autorevolezza
4. Se il client aveva predetto male, fa rollback e replay

Per questo separi sempre:
- **Input**: cosa il giocatore vuole fare
- **State**: risultato della simulazione
- **View**: come lo mostri a schermo

---

## 2) Quale classe base usare

### `PredictedIdentity<INPUT, STATE>`
Usala quando l'oggetto ha input del player (es. movimento, sparo, invio config).

### `PredictedIdentity<STATE>`
Usala quando non serve input diretto del player, ma vuoi stato sincronizzato/predetto.

### `DeterministicIdentity<...>`
Versione deterministic (sfloat) quando vuoi massima coerenza deterministica.

---

## 3) Setup scena/prefab (checklist)

1. In scena serve un `PredictionManager`.
2. Nel `PredictionManager` configura `PredictedPrefabs`.
3. Spawna player/oggetti attraverso `PredictedPlayerSpawner` o `PredictedHierarchy.Create(...)`.
4. Sul prefab player aggiungi componenti predicted necessari (`PredictedTransform`, `PredictedRigidbody`, script custom).
5. Evita di mischiare la stessa responsabilità tra `NetworkIdentity` legacy e `PredictedIdentity` sulla stessa logica.

---

## 4) `IPredictedData`: cos'è e perché serve

`IPredictedData` è l'interfaccia dei dati che Purrdiction serializza/storicizza.

- `INPUT : IPredictedData`
- `STATE : IPredictedData<STATE>`

### Regole pratiche
- Usa `struct` (non class).
- Metti dentro solo dati necessari.
- Implementa `Dispose()` (anche vuoto se non hai risorse da pulire).
- Per `STATE`, PurrNet genera automaticamente metodi math (`Add/Negate/Scale`) per campi compatibili.

Esempio minimale:

```csharp
public struct MoveInput : IPredictedData
{
    public float turn;
    public bool jump;

    public void Dispose() { }
}

public struct MoveState : IPredictedData<MoveState>
{
    public float yaw;

    public void Dispose() { }
}
```

---

## 5) Lifecycle importante (ordine logico)

### `LateAwake()`
È il punto iniziale equivalente al "sono pronto" nel mondo predicted.
Usalo per setup locale leggero (cache reference, camera owner, subscribe eventi).

### `GetFinalInput(ref INPUT input)`
Input "base" del frame/tick (es. assi movimento tenuti premuti).

### `UpdateInput(ref INPUT input)`
Input incrementale/eventi one-shot nello stesso frame (es. jump tap, click).

### `Simulate(INPUT input, ref STATE state, float delta)`
Cuore della simulazione: aggiorna lo stato e/o fisica predicted.

### `LateSimulate(...)` (se usato)
Logica post-simulate del tick.

### `UpdateView(STATE viewState, STATE? verified)`
Solo visual: applica stato interpolato a mesh/animazioni/UI.

### `Interpolate(from, to, t)`
Come interpolare stato per la view.
Per dati discreti (es. config string/json) di solito `return to;`.

### `OnViewOwnerChanged(oldOwner, newOwner)`
Cambio owner, utile per effetti visuali, camera, HUD.

---

## 6) Differenza chiave: `GetFinalInput` vs `UpdateInput`

Entrambi sono validi.

- `GetFinalInput`: ottimo per input continuo (movimento).
- `UpdateInput`: comodo per accumulare one-shot/eventi durante frame.

Pattern tipico:
- assi in `GetFinalInput`
- pulsanti edge-trigger in `UpdateInput`

---

## 6-bis) Parametri Inspector importanti

Questi sono i 3 parametri che creano piu confusione all'inizio.

### `Extrapolate Input` (su `PredictedIdentity<INPUT, STATE>`)

Decide cosa fare per i player remoti quando manca un input nuovo in quel tick.

- `ON`: prova a riusare input vecchio (piu fluido, ma puo creare correzioni/jitter quando arriva la verifica server).
- `OFF`: non riusa input vecchio (meno errori di predizione, ma piu micro-stop sui remoti se la rete e sporca).

Quando e `ON`, conviene implementare `ModifyExtrapolatedInput(ref INPUT input)` per spegnere input non continui (esempio: `jump = false`).

### `Repeat Input Factor` (su `PredictedIdentity<INPUT, STATE>`)

Funziona solo se `Extrapolate Input` e `ON`.
Controlla per quanti tick massimi e lecito riusare un input vecchio.

- alto (es. `0.8`): piu continuo, ma puo "trascinare" il movimento remoto e poi correggere.
- basso (es. `0.1`): meno trascinamento e meno turn fantasma, ma piu rischio di micro-stop.

Regola rapida (approssimativa):
- tick riusati ~ `ceil(RepeatInputFactor * tickRate / 6)`
- con tickRate `42`: `0.8` -> ~6 tick, `0.1` -> ~1 tick.

### `Unparent Graphics` (su `PredictedTransform`)

Quando attivo, il nodo grafico (`_graphics`, per esempio `Visuals`) viene staccato dal root predicted.

Perche serve:
- durante reconcile/pooling il root puo essere abilitato/disabilitato o corretto spesso;
- tenendo la grafica separata, eviti sparizioni/flicker dei mesh render.

Tradeoff:
- i `Visuals` diventano in world space (non ragionare piu come semplice figlio locale del root).
- logica gameplay, collider, rigidbody, hitbox devono restare sul root predicted, non sui `Visuals`.

Quando usarlo:
- se vedi grafica che sparisce o "scatta" durante correzioni;
- se con `Extrapolate Input` basso/off hai problemi visuali.

---

## 7) Esempio completo: movimento predicted

```csharp
using PurrNet.Prediction;
using UnityEngine;

public class SimpleMove : PredictedIdentity<SimpleMove.InputData, SimpleMove.StateData>
{
    [SerializeField] private PredictedRigidbody rb;
    [SerializeField] private float moveForce = 10f;

    protected override void GetFinalInput(ref InputData input)
    {
        input.horizontal = Input.GetAxisRaw("Horizontal");
        input.vertical = Input.GetAxisRaw("Vertical");
    }

    protected override void UpdateInput(ref InputData input)
    {
        input.jump |= Input.GetKeyDown(KeyCode.Space);
    }

    protected override void Simulate(InputData input, ref StateData state, float delta)
    {
        Vector3 dir = new Vector3(input.horizontal, 0f, input.vertical).normalized;
        rb.AddForce(dir * moveForce, ForceMode.Force);

        if (input.jump)
            rb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
    }

    public struct InputData : IPredictedData
    {
        public float horizontal;
        public float vertical;
        public bool jump;

        public void Dispose() { }
    }

    public struct StateData : IPredictedData<StateData>
    {
        public void Dispose() { }
    }
}
```

---

## 8) Esempio completo: dato discreto (config veicolo)

Questo è il caso tipo "invio una configurazione e la applico a tutti".

```csharp
using PurrNet.Prediction;
using UnityEngine;

public class VehicleNetConfig : PredictedIdentity<VehicleNetConfig.ConfigInput, VehicleNetConfig.ConfigState>
{
    [SerializeField] private VehicleLoader loader;

    private int _nextRevision = 1;
    private int _lastAppliedRevision;
    private bool _hasPending;
    private string _pendingJson;

    protected override void LateAwake()
    {
        QueueLocalIfAny();
    }

    protected override void UpdateInput(ref ConfigInput input)
    {
        QueueLocalIfAny();

        if (!_hasPending)
            return;

        input.submit = true;
        input.revision = _nextRevision++;
        input.json = _pendingJson;

        _hasPending = false;
        _pendingJson = null;
    }

    protected override void Simulate(ConfigInput input, ref ConfigState state, float delta)
    {
        if (!input.submit) return;
        if (input.revision <= state.revision) return;
        if (string.IsNullOrEmpty(input.json)) return;

        state.revision = input.revision;
        state.json = input.json;
    }

    protected override ConfigState Interpolate(ConfigState from, ConfigState to, float t) => to;

    protected override void UpdateView(ConfigState viewState, ConfigState? verified)
    {
        if (loader == null) return;
        if (viewState.revision <= _lastAppliedRevision) return;
        if (string.IsNullOrEmpty(viewState.json)) return;

        _lastAppliedRevision = viewState.revision;
        loader.ApplyConfigJson(viewState.json);
    }

    private void QueueLocalIfAny()
    {
        if (!isOwner) return;
        if (VehicleManager.Instance == null) return;

        var json = VehicleManager.Instance.GetVehicleJson();
        if (string.IsNullOrEmpty(json)) return;

        _pendingJson = json;
        _hasPending = true;
    }

    public struct ConfigInput : IPredictedData
    {
        public bool submit;
        public int revision;
        public string json;
        public void Dispose() { json = null; }
    }

    public struct ConfigState : IPredictedData<ConfigState>
    {
        public int revision;
        public string json;
        public void Dispose() { json = null; }
    }
}
```

---

## 9) Cosa NON fare (errori comuni)

1. Non mettere logica gameplay dentro `UpdateView`.
2. Non leggere input in `Simulate` direttamente da `Input.GetKey`.
3. Non mescolare due fonti di verità (RPC legacy + predicted state) sulla stessa cosa.
4. Non usare dati enormi in input/state se puoi inviare ID compatti.
5. Non dimenticare check owner per input locali.

---

## 10) Trigger e collisioni predicted

Con `PredictedRigidbody`, i trigger non vanno gestiti con metodi custom tipo:

```csharp
void OnTriggerEnter(ref State state, Collider col) { }
```

Quel metodo non e un callback Unity valido e PurrDiction non lo chiama.

Il pattern pratico e iscriversi agli eventi del `PredictedRigidbody`:

```csharp
protected override void LateAwake()
{
    _rigidbody = GetComponent<PredictedRigidbody>();
    _rigidbody.onTriggerEnter += HandlePredictedTriggerEnter;
    _rigidbody.onTriggerExit += HandlePredictedTriggerExit;
}

protected override void Destroyed()
{
    if (_rigidbody == null) return;

    _rigidbody.onTriggerEnter -= HandlePredictedTriggerEnter;
    _rigidbody.onTriggerExit -= HandlePredictedTriggerExit;
}

private void HandlePredictedTriggerEnter(GameObject other)
{
    ref var state = ref currentState;
    // aggiorna solo dati di simulazione
}

private void HandlePredictedTriggerExit(GameObject other)
{
    ref var state = ref currentState;
    // aggiorna solo dati di simulazione
}
```

Regole pratiche:
- Non usare `isOwner` per decidere se cambiare `currentState`: lo state deve restare coerente tra owner, server e replay.
- Usa `isOwner` solo per input locale, camera, UI e view locali.
- Nei callback trigger aggiorna solo dati simulati; VFX/UI vanno letti dallo state o dalla view.
- Se puoi entrare in piu zone uguali contemporaneamente, preferisci un contatore nello `State` invece di un singolo `bool`.
- Gli eventi fisici predicted risolvono l'altro oggetto tramite `PredictedIdentity`; se ti serve distinguere un child trigger specifico, dagli un marker/componento predetto dedicato.

---

## 11) Debug rapido

Se qualcosa non torna:
1. Verifica che prefab sia spawnato da predicted hierarchy/spawner.
2. Verifica `isOwner` lato client owner.
3. Logga `input.revision` e `state.revision` in `Simulate`.
4. Logga quando `UpdateView` applica stato.
5. Controlla che `PredictionManager` sia in scena e configurato.

---

## 12) Mini cheat-sheet override

Per `PredictedIdentity<INPUT, STATE>` i più usati:

- `LateAwake()`
- `GetFinalInput(ref INPUT input)`
- `UpdateInput(ref INPUT input)`
- `Simulate(INPUT input, ref STATE state, float delta)`
- `Interpolate(STATE from, STATE to, float t)`
- `UpdateView(STATE viewState, STATE? verified)`
- `ModifyExtrapolatedInput(ref INPUT input)` (opzionale)
- `SanitizeInput(ref INPUT input)` (opzionale)

---

## 13) Consiglio architetturale per questo progetto

Nel tuo progetto conviene mantenere questa regola:
- **Movimento/azioni/config runtime player**: predicted (`PredictedIdentity`)
- **Sistemi legacy solo dove non hai ancora migrato**

Così eviti duplicazioni, desync e callback che "non partono" perché sono lifecycle diversi.

---

Fine guida.
