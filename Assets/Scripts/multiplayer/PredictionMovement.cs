using PurrNet.Prediction;
using UnityEngine;

public class PredictionMovement : PredictedIdentity<PredictionMovement.Input, PredictionMovement.State>
{
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private float _moveForce = 5;
    protected override void Simulate(Input input, ref State state, float delta)
    {
        Debug.Log("mio dio");
        Vector3 moveDirection = new Vector3(input.direction.x, 0, input.direction.y).normalized * _moveForce;
        Debug.Log(moveDirection);
        _rigidbody.AddForce(moveDirection);
    }

    protected override void GetFinalInput(ref Input input)
    {
        input.direction = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"),
            UnityEngine.Input.GetAxisRaw("Vertical"));
    }

    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public Vector2 direction;

        public void Dispose() { }
    }
}