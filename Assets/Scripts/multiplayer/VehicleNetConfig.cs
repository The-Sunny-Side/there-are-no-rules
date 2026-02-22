using PurrNet.Prediction;
using UnityEngine;       

// Questo script fa dire al gioco:
// "Il mio veicolo ha questa configurazione" e la fa vedere a tutti.
public class VehicleNetConfig : PredictedIdentity<VehicleNetConfig.ConfigInput, VehicleNetConfig.ConfigState>
{
    // loader che monta davvero i pezzi del veicolo.
    [SerializeField] private VehicleLoader loader;

    // Numero della "versione" della config (1, 2, 3...).
    private int _nextRevision = 1;

    // Ultima versione già applicata in grafica.
    private int _lastAppliedRevision;

    // True = abbiamo una config pronta da spedire.
    private bool _hasPendingConfig;

    // Qui teniamo il testo JSON pronto da mandare.
    private string _pendingJson;

    // Ultimo JSON che abbiamo già mandato (per non rimandarlo uguale).
    private string _lastSubmittedJson;

    // Viene chiamato quando l'oggetto predicted è pronto.
    protected override void LateAwake()
    {
        // Proviamo subito a prendere la config locale del player.
        TryQueueLatestLocalConfig();
    }

    // Qui prepariamo l'input da inviare in rete.
    // Questo metodo viene chiamato spesso (frame).
    protected override void UpdateInput(ref ConfigInput input)
    {
        // Controlla se c'è una config nuova sul disco/manager.
        TryQueueLatestLocalConfig();

        // Se non c'è niente da inviare, usciamo.
        if (!_hasPendingConfig)
            return;

        // Diciamo: "Sì, voglio inviare una config".
        input.submit = true;

        // Mettiamo il numero di versione.
        input.revision = _nextRevision++;

        // Mettiamo il testo JSON da inviare.
        input.json = _pendingJson;

        // Segniamo che questo JSON è già stato messo in invio.
        _lastSubmittedJson = _pendingJson;

        // Puliamo la coda locale.
        _pendingJson = null;
        _hasPendingConfig = false;
    }

    // Questa è la "simulazione": se arriva input valido, aggiorna lo stato.
    protected override void Simulate(ConfigInput input, ref ConfigState state, float delta)
    {
        // Se nessuno ha chiesto di inviare, non fare niente.
        if (!input.submit)
            return;

        // Se la versione è vecchia o uguale, ignorala.
        if (input.revision <= state.revision)
            return;

        // Se il JSON è vuoto, ignoralo.
        if (string.IsNullOrEmpty(input.json))
            return;

        // Salva la nuova versione...
        state.revision = input.revision;

        // ...e salva il nuovo JSON.
        state.json = input.json;
    }

    // Per dati "a scatti" (testo/config), non vogliamo interpolare.
    // Prendiamo direttamente il valore nuovo.
    protected override ConfigState Interpolate(ConfigState from, ConfigState to, float t)
    {
        return to;
    }

    // Qui applichiamo lo stato alla parte visiva.
    protected override void UpdateView(ConfigState viewState, ConfigState? verified)
    {
        // Se manca il loader, non possiamo montare nulla.
        if (loader == null)
            return;

        // Se è una versione già vista, non rifare il lavoro.
        if (viewState.revision <= _lastAppliedRevision)
            return;

        // Se il JSON è vuoto, non applicare.
        if (string.IsNullOrEmpty(viewState.json))
            return;

        // Segna che questa versione è applicata.
        _lastAppliedRevision = viewState.revision;

        // Monta davvero i pezzi del veicolo.
        loader.ApplyConfigJson(viewState.json);
    }

    // Guarda la config locale del player e, se nuova, la mette in coda.
    private void TryQueueLatestLocalConfig()
    {
        // Solo il proprietario del player decide la propria config.
        if (!isOwner)
            return;

        // Se il manager non esiste ancora, aspetta.
        if (VehicleManager.Instance == null)
            return;

        // Leggi il JSON del veicolo locale.
        string json = VehicleManager.Instance.GetVehicleJson();

        // Se è vuoto, niente da fare.
        if (string.IsNullOrEmpty(json))
            return;

        // Se è uguale all'ultimo già inviato o già in coda, evita duplicati.
        if (json == _lastSubmittedJson || json == _pendingJson)
            return;

        // Mettilo in coda per l'invio.
        _pendingJson = json;
        _hasPendingConfig = true;
    }

    // Questo è lo stato condiviso tra tutti.
    public struct ConfigState : IPredictedData<ConfigState>
    {
        // Numero versione della config.
        public int revision;

        // Testo JSON della config.
        public string json;

        // Pulizia memoria quando serve.
        public void Dispose()
        {
            json = null;
        }
    }

    // Questo è il "messaggino" che parte dal controller.
    public struct ConfigInput : IPredictedData
    {
        // True = invia config in questo input.
        public bool submit;

        // Versione della config inviata.
        public int revision;

        // Testo JSON inviato.
        public string json;

        // Pulizia memoria quando serve.
        public void Dispose()
        {
            json = null;
        }
    }
}
