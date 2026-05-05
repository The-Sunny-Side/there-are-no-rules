using PurrNet.Prediction;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;


public class TrailZone : StatelessPredictedIdentity
{
    [SerializeField] private Transform source;
    [SerializeField] private PredictedRigidbody sourceRigidbody;

    public Transform Source => source;
    public PredictedRigidbody SourceRigidbody => sourceRigidbody;

}