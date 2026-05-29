using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

[RequireComponent(typeof(NavMeshAgent))]
public class RouteFollower : MonoBehaviour
{
    public string routeApiBaseUrl = "http://127.0.0.1:8000";
    public string userType = "blind";
    public string strategy = "safest";
    public float waypointReachDistance = 0.8f;
    public float requestTimeoutSeconds = 10f;
    public Transform fallbackTarget;
    [Header("仿真扰动")]
    public bool enableNavigationNoise = false;
    public float positionErrorRadius = 0.25f;
    public bool enablePositionErrorDrift = false;
    public float positionErrorDriftInterval = 1.2f;
    public float positionErrorDriftBlend = 0.35f;
    public bool enableLateralWalkingNoise = false;
    public float lateralWalkingNoiseAmplitude = 0.15f;
    public float destinationRefreshInterval = 0.25f;

    private NavMeshAgent navAgent;
    private Coroutine routeCoroutine;
    private readonly List<Transform> activeWaypoints = new List<Transform>();
    private Vector3 fallbackDestination;
    private bool hasFallbackDestination;
    private Vector3 routeNoiseOffset;
    private Vector3 routeNoiseTargetOffset;
    private float nextNoiseDriftTime;
    public bool HasBegunNavigation { get; private set; }

    [Serializable]
    private class RouteResponse
    {
        public bool success;
        public RouteResult[] routes;
        public string error;
    }

    [Serializable]
    private class RouteResult
    {
        public RouteStep[] steps;
    }

    [Serializable]
    private class RouteStep
    {
        public string to_name;
    }

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    public void ConfigureFallback(Vector3 destination)
    {
        fallbackDestination = destination;
        hasFallbackDestination = true;
    }

    public void StartRoute(string startNodeId, string endNodeId)
    {
        if (routeCoroutine != null)
        {
            StopCoroutine(routeCoroutine);
        }

        HasBegunNavigation = false;
        routeNoiseOffset = enableNavigationNoise ? BuildPlanarNoiseOffset() : Vector3.zero;
        routeNoiseTargetOffset = routeNoiseOffset;
        nextNoiseDriftTime = Time.time + Mathf.Max(0.1f, positionErrorDriftInterval);
        routeCoroutine = StartCoroutine(FetchAndFollowRoute(startNodeId, endNodeId));
    }

    private IEnumerator FetchAndFollowRoute(string startNodeId, string endNodeId)
    {
        activeWaypoints.Clear();

        if (string.IsNullOrWhiteSpace(startNodeId) || string.IsNullOrWhiteSpace(endNodeId))
        {
            MoveToFallback();
            routeCoroutine = null;
            yield break;
        }

        string url = string.Format(
            "{0}/api/route?start={1}&end={2}&user_type={3}&strategy={4}",
            routeApiBaseUrl.TrimEnd('/'),
            UnityWebRequest.EscapeURL(startNodeId),
            UnityWebRequest.EscapeURL(endNodeId),
            UnityWebRequest.EscapeURL(userType),
            UnityWebRequest.EscapeURL(strategy)
        );

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("[RouteFollower] route request failed: " + request.error);
                MoveToFallback();
                routeCoroutine = null;
                yield break;
            }

            RouteResponse response = JsonUtility.FromJson<RouteResponse>(request.downloadHandler.text);
            if (response == null || !response.success || response.routes == null || response.routes.Length == 0 || response.routes[0].steps == null || response.routes[0].steps.Length == 0)
            {
                Debug.LogWarning("[RouteFollower] route response missing steps.");
                MoveToFallback();
                routeCoroutine = null;
                yield break;
            }

            RouteStep[] steps = response.routes[0].steps;
            for (int i = 0; i < steps.Length; i++)
            {
                string targetName = steps[i].to_name;
                if (string.IsNullOrWhiteSpace(targetName))
                {
                    continue;
                }

                Transform waypoint = FindSceneTransform(targetName);
                if (waypoint != null)
                {
                    activeWaypoints.Add(waypoint);
                }
                else
                {
                    Debug.LogWarning("[RouteFollower] missing route marker: " + targetName);
                }
            }
        }

        if (activeWaypoints.Count == 0)
        {
            MoveToFallback();
            routeCoroutine = null;
            yield break;
        }

        for (int i = 0; i < activeWaypoints.Count; i++)
        {
            Transform waypoint = activeWaypoints[i];
            if (waypoint == null)
            {
                continue;
            }

            HasBegunNavigation = true;
            float nextRefreshTime = -1f;
            while (navAgent.pathPending || navAgent.remainingDistance > waypointReachDistance)
            {
                if (Time.time >= nextRefreshTime)
                {
                    navAgent.SetDestination(BuildSimulatedDestination(waypoint.position));
                    nextRefreshTime = Time.time + Mathf.Max(0.05f, destinationRefreshInterval);
                }

                yield return null;
            }
        }

        MoveToFallback();
        routeCoroutine = null;
    }

    private void MoveToFallback()
    {
        if (navAgent == null)
        {
            return;
        }

        if (fallbackTarget != null)
        {
            HasBegunNavigation = true;
            navAgent.SetDestination(BuildSimulatedDestination(fallbackTarget.position));
            return;
        }

        if (hasFallbackDestination)
        {
            HasBegunNavigation = true;
            navAgent.SetDestination(BuildSimulatedDestination(fallbackDestination));
        }
    }

    private Vector3 BuildSimulatedDestination(Vector3 baseDestination)
    {
        Vector3 simulatedDestination = baseDestination;
        if (enableNavigationNoise)
        {
            UpdateNoiseOffset();
            simulatedDestination += routeNoiseOffset;
        }

        if (enableLateralWalkingNoise)
        {
            Vector3 lateralDirection = navAgent != null && navAgent.desiredVelocity.sqrMagnitude > 0.001f
                ? Vector3.Cross(Vector3.up, navAgent.desiredVelocity.normalized)
                : transform.right;
            float lateralPhase = Mathf.Sin(Time.time * 2.35f + GetInstanceID() * 0.173f);
            simulatedDestination += lateralDirection.normalized * lateralPhase * lateralWalkingNoiseAmplitude;
        }

        return simulatedDestination;
    }

    private void UpdateNoiseOffset()
    {
        if (!enableNavigationNoise)
        {
            routeNoiseOffset = Vector3.zero;
            routeNoiseTargetOffset = Vector3.zero;
            return;
        }

        if (!enablePositionErrorDrift)
        {
            return;
        }

        if (Time.time >= nextNoiseDriftTime)
        {
            routeNoiseTargetOffset = BuildPlanarNoiseOffset();
            nextNoiseDriftTime = Time.time + Mathf.Max(0.1f, positionErrorDriftInterval);
        }

        float blend = Mathf.Clamp01(positionErrorDriftBlend);
        routeNoiseOffset = Vector3.Lerp(routeNoiseOffset, routeNoiseTargetOffset, blend);
    }

    private Vector3 BuildPlanarNoiseOffset()
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, positionErrorRadius);
        return new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    private Transform FindSceneTransform(string objectName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform item = allTransforms[i];
            if (item == null || item.hideFlags != HideFlags.None || !item.gameObject.scene.IsValid())
            {
                continue;
            }

            if (string.Equals(item.name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }
}
