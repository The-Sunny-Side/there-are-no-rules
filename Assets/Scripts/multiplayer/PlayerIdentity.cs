using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerIdentity : MonoBehaviour
{
    [SerializeField] private NewMovementPredicted _movement;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private PredictedRigidbody _predictedRigidbody;
    [SerializeField] private PredictedTransform _predictedTransform;
    [SerializeField] private bool _isBot;
    [SerializeField] private int _botRaceId;

    public bool IsLocalOwner => _movement != null && _movement.isOwner;
    public bool IsBot => _isBot;
    public int BotRaceId => _botRaceId;
    public Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : GetComponent<Rigidbody>();
    public PredictedRigidbody PredictedRigidbody => _predictedRigidbody != null ? _predictedRigidbody : GetComponent<PredictedRigidbody>();
    public PredictedTransform PredictedTransform => _predictedTransform != null ? _predictedTransform : GetComponent<PredictedTransform>();

    private void Awake()
    {
        _movement ??= GetComponent<NewMovementPredicted>();
        _rigidbody ??= GetComponent<Rigidbody>();
        _predictedRigidbody ??= GetComponent<PredictedRigidbody>();
        _predictedTransform ??= GetComponent<PredictedTransform>();
    }

    public bool TryGetOwner(out PlayerID playerId)
    {
        if (_movement != null && _movement.owner.HasValue)
        {
            playerId = _movement.owner.Value;
            return true;
        }

        playerId = default;
        return false;
    }

    public void SetBotIdentity(int botRaceId)
    {
        _isBot = botRaceId > 0;
        _botRaceId = _isBot ? botRaceId : 0;
    }
}
