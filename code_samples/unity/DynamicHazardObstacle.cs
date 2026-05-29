using System.Collections.Generic;
using UnityEngine;

public class DynamicHazardObstacle : MonoBehaviour
{
    [Header("障碍类型")]
    public string hazardType = "DynamicObstacle";

    [Header("移动设置")]
    public Vector3 localStartOffset = Vector3.zero;
    public Vector3 localEndOffset = new Vector3(2f, 0f, 0f);
    public float moveSpeed = 1.2f;
    public float arriveThreshold = 0.05f;
    public bool orientAlongMovement = true;

    [Header("危险上报")]
    public float detectionRadius = 1.0f;
    public float criticalDistance = 0.45f;
    public float exposureHoldTime = 0.2f;
    public float perAgentReportCooldown = 0.75f;
    public LayerMask detectionLayers = ~0;

    private Vector3 anchorPosition;
    private bool movingToEnd = true;
    private readonly Dictionary<int, float> lastReportTimes = new Dictionary<int, float>();
    private readonly Dictionary<int, float> exposureStartTimes = new Dictionary<int, float>();

    private void Awake()
    {
        anchorPosition = transform.position;
    }

    public void ApplyRuntimeTuning(float tunedDetectionRadius, float tunedCriticalDistance, float tunedExposureHoldTime, float tunedReportCooldown)
    {
        detectionRadius = Mathf.Max(tunedCriticalDistance, tunedDetectionRadius);
        criticalDistance = Mathf.Max(0.05f, tunedCriticalDistance);
        exposureHoldTime = Mathf.Max(0f, tunedExposureHoldTime);
        perAgentReportCooldown = Mathf.Max(0f, tunedReportCooldown);
    }

    private void Update()
    {
        MoveObstacle();
        ReportNearbyAgents();
    }

    private void MoveObstacle()
    {
        Vector3 startPoint = anchorPosition + localStartOffset;
        Vector3 endPoint = anchorPosition + localEndOffset;
        Vector3 targetPoint = movingToEnd ? endPoint : startPoint;

        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

        Vector3 moveDelta = transform.position - previousPosition;
        if (orientAlongMovement && moveDelta.sqrMagnitude > 0.0001f)
        {
            transform.forward = moveDelta.normalized;
        }

        if (Vector3.Distance(transform.position, targetPoint) <= arriveThreshold)
        {
            movingToEnd = !movingToEnd;
        }
    }

    private void ReportNearbyAgents()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            detectionLayers,
            QueryTriggerInteraction.Collide
        );

        HashSet<int> reportedAgentsThisFrame = new HashSet<int>();
        HashSet<int> agentsInsideCriticalZone = new HashSet<int>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            DigitalCane cane = hit.GetComponentInParent<DigitalCane>();
            if (cane == null)
            {
                continue;
            }

            int agentId = cane.gameObject.GetInstanceID();
            if (reportedAgentsThisFrame.Contains(agentId))
            {
                continue;
            }

            Vector3 reportPosition = transform.position;
            Collider obstacleCollider = GetComponent<Collider>();
            if (obstacleCollider != null)
            {
                reportPosition = obstacleCollider.ClosestPoint(cane.transform.position);
            }

            float closestDistance = Vector3.Distance(reportPosition, cane.transform.position);
            if (closestDistance > criticalDistance)
            {
                exposureStartTimes.Remove(agentId);
                continue;
            }

            agentsInsideCriticalZone.Add(agentId);
            if (!exposureStartTimes.TryGetValue(agentId, out float exposureStartTime))
            {
                exposureStartTimes[agentId] = Time.time;
                continue;
            }

            if (Time.time - exposureStartTime < exposureHoldTime)
            {
                continue;
            }

            if (lastReportTimes.TryGetValue(agentId, out float lastTime) && Time.time - lastTime < perAgentReportCooldown)
            {
                continue;
            }

            reportedAgentsThisFrame.Add(agentId);
            lastReportTimes[agentId] = Time.time;

            cane.ReportHazard(hazardType, reportPosition);
        }

        if (exposureStartTimes.Count == 0)
        {
            return;
        }

        List<int> staleAgentIds = new List<int>();
        foreach (KeyValuePair<int, float> entry in exposureStartTimes)
        {
            if (!agentsInsideCriticalZone.Contains(entry.Key))
            {
                staleAgentIds.Add(entry.Key);
            }
        }

        for (int i = 0; i < staleAgentIds.Count; i++)
        {
            exposureStartTimes.Remove(staleAgentIds[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 runtimeAnchor = Application.isPlaying ? anchorPosition : transform.position;
        Vector3 startPoint = runtimeAnchor + localStartOffset;
        Vector3 endPoint = runtimeAnchor + localEndOffset;

        Gizmos.color = new Color(1f, 0.65f, 0f, 0.75f);
        Gizmos.DrawLine(startPoint, endPoint);
        Gizmos.DrawWireSphere(startPoint, 0.12f);
        Gizmos.DrawWireSphere(endPoint, 0.12f);

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
