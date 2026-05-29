using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;

// 定义用于序列化为 JSON 的数据类
[System.Serializable]
public class HazardData
{
    public float x;
    public float y;
    public float z;
    public string obstacle_type;
    public string client_source;
}

public class DigitalCane : MonoBehaviour
{
    [Header("探测设置")]
    [Tooltip("正前方射线长度（探墙）")]
    public float forwardRayLength = 2.0f;
    [Tooltip("多方向扫描射线长度")]
    public float omniRayLength = 1.6f;
    [Tooltip("侧向扫描射线长度")]
    public float sideRayLength = 1.3f;
    [Tooltip("后向扫描射线长度")]
    public float rearRayLength = 1.0f;
    [Tooltip("水平扫描起始高度")]
    public float horizontalRayOriginOffset = 0.55f;
    [Tooltip("斜下方射线长度（探坑）")]
    public float downwardRayLength = 1.0f;
    [Tooltip("探坑射线的起始高度偏移（相对于角色中心）")]
    public float downwardRayOriginOffset = 0.5f;

    [Header("网络设置")]
    [Tooltip("后端 POST 接口地址")]
    public string backendUrl = "http://127.0.0.1:8000/api/record_hazard";
    [Tooltip("当前 Agent 的来源或分组标识")]
    public string agentGroup = "unity_agent";
    [Tooltip("报警冷却时间（秒）")]
    public float cooldownTime = 2.0f;
    public int hazardTriggerCount { get; private set; }

    [Header("实验开关")]
    [Tooltip("是否启用前方标签检测")]
    public bool enableForwardHazardScan = true;
    [Tooltip("是否启用多方向扫描（前左/前右/左右/可选后向）")]
    public bool enableOmniHazardScan = false;
    [Tooltip("多方向扫描是否包含后方")]
    public bool includeRearArcScan = false;
    [Tooltip("是否启用向下悬空检测（Drop-off）")]
    public bool enableDropOffScan = true;
    [Tooltip("路面材质提示是否计入危险暴露")]
    public bool countSurfaceTagsAsCriticalExposure = false;

    [Header("空间音频配置 (Spatial Audio)")]
    [Tooltip("台阶提示音")]
    public AudioClip audioStairs;
    [Tooltip("跌落/悬空提示音")]
    public AudioClip audioDropOff;
    [Tooltip("青石板路面提示音")]
    public AudioClip audioRoadStone;
    [Tooltip("水泥路面提示音")]
    public AudioClip audioRoadCement;
    [Tooltip("默认障碍物提示音")]
    public AudioClip audioDefault;

    [Header("音频衰减设置")]
    [Tooltip("最小听到距离")]
    public float minAudioDistance = 1.0f;
    [Tooltip("最大听到距离")]
    public float maxAudioDistance = 20.0f;

    private float lastTriggerTime = -999f; // 确保第一次可立即触发
    
    // 标签与音效的映射字典
    private Dictionary<string, AudioClip> tagAudioMap;
    private Dictionary<string, int> hazardCountsByType;
    private Dictionary<string, int> criticalHazardCountsByType;
    private HashSet<string> warningOnlyHazardTypes;
    private HashSet<string> criticalHazardTypes;
    public int criticalHazardTriggerCount { get; private set; }
    public bool HasCriticalHazardExposure => criticalHazardTriggerCount > 0;

    void Start()
    {
        hazardTriggerCount = 0;
        criticalHazardTriggerCount = 0;
        hazardCountsByType = new Dictionary<string, int>();
        criticalHazardCountsByType = new Dictionary<string, int>();
        // 初始化标签与音效的映射
        tagAudioMap = new Dictionary<string, AudioClip>
        {
            { "Stairs", audioStairs },
            { "Road_Stone", audioRoadStone },
            { "Road_Cement", audioRoadCement },
            { "Drop-off", audioDropOff },
            { "DynamicObstacle", audioDefault },
            { "SideObstacle", audioDefault },
            { "RearObstacle", audioDefault },
            { "NarrowPath", audioDefault }
        };

        warningOnlyHazardTypes = new HashSet<string>
        {
            "Road_Stone",
            "Road_Cement"
        };

        criticalHazardTypes = new HashSet<string>
        {
            "Stairs",
            "Drop-off",
            "DynamicObstacle",
            "SideObstacle",
            "RearObstacle",
            "NarrowPath"
        };
    }

    void Update()
    {
        bool hazardDetectedThisFrame = false;

        if (enableForwardHazardScan)
        {
            Vector3 forwardOrigin = GetHorizontalRayOrigin();
            if (TryScanAndTrigger(forwardOrigin, transform.forward, forwardRayLength, Color.red))
            {
                hazardDetectedThisFrame = true;
            }
        }

        if (enableOmniHazardScan && !hazardDetectedThisFrame)
        {
            hazardDetectedThisFrame = RunOmniDirectionalScan();
        }

        if (enableDropOffScan && !hazardDetectedThisFrame)
        {
            Vector3 downwardOrigin = transform.position + transform.forward * 0.5f; 
            Vector3 downwardDirection = Vector3.down;
            Debug.DrawRay(downwardOrigin, downwardDirection * downwardRayLength, Color.blue); // 蓝色：探坑

            if (!Physics.Raycast(downwardOrigin, downwardDirection, downwardRayLength))
            {
                Vector3 dropOffPoint = downwardOrigin + downwardDirection * downwardRayLength;
                TryTriggerHazard("Drop-off", dropOffPoint);
            }
        }
    }

    // 检查是否为目标标签
    bool IsTargetTag(string tag)
    {
        return tag == "Stairs" || tag == "Road_Stone" || tag == "Road_Cement";
    }

    public void ReportHazard(string type, Vector3 position)
    {
        if (string.IsNullOrEmpty(type))
        {
            type = "unknown";
        }

        TryTriggerHazard(type, position);
    }

    // 尝试触发危险反馈（带冷却检查）
    void TryTriggerHazard(string type, Vector3 position)
    {
        string normalizedType = NormalizeHazardType(type);
        if (Time.time - lastTriggerTime >= cooldownTime)
        {
            lastTriggerTime = Time.time;
            hazardTriggerCount++;
            if (!hazardCountsByType.ContainsKey(normalizedType))
            {
                hazardCountsByType[normalizedType] = 0;
            }

            hazardCountsByType[normalizedType]++;
            if (IsCriticalHazardType(normalizedType))
            {
                criticalHazardTriggerCount++;
                if (!criticalHazardCountsByType.ContainsKey(normalizedType))
                {
                    criticalHazardCountsByType[normalizedType] = 0;
                }

                criticalHazardCountsByType[normalizedType]++;
            }
            
            // 1. 播放 3D 空间音效
            PlaySpatialAudio(normalizedType, position);

            // 2. 向后端发送数据
            StartCoroutine(PostHazardData(normalizedType, position));
        }
    }

    string NormalizeHazardType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return "unknown";
        }

        return type.Trim();
    }

    bool IsCriticalHazardType(string type)
    {
        string normalizedType = NormalizeHazardType(type);
        if (criticalHazardTypes.Contains(normalizedType))
        {
            return true;
        }

        if (!countSurfaceTagsAsCriticalExposure && warningOnlyHazardTypes.Contains(normalizedType))
        {
            return false;
        }

        if (countSurfaceTagsAsCriticalExposure && warningOnlyHazardTypes.Contains(normalizedType))
        {
            return true;
        }

        return !warningOnlyHazardTypes.Contains(normalizedType);
    }

    public int GetHazardCountForType(string type)
    {
        string normalizedType = NormalizeHazardType(type);
        if (!hazardCountsByType.ContainsKey(normalizedType))
        {
            return 0;
        }

        return hazardCountsByType[normalizedType];
    }

    public Dictionary<string, int> GetHazardCountSnapshot()
    {
        return new Dictionary<string, int>(hazardCountsByType);
    }

    public Dictionary<string, int> GetCriticalHazardCountSnapshot()
    {
        return new Dictionary<string, int>(criticalHazardCountsByType);
    }

    Vector3 GetHorizontalRayOrigin()
    {
        return transform.position + Vector3.up * horizontalRayOriginOffset;
    }

    bool RunOmniDirectionalScan()
    {
        Vector3 origin = GetHorizontalRayOrigin();
        Vector3 forwardLeft = (transform.forward - transform.right).normalized;
        Vector3 forwardRight = (transform.forward + transform.right).normalized;

        if (TryScanAndTrigger(origin, forwardLeft, omniRayLength, new Color(1f, 0.5f, 0f)))
        {
            return true;
        }

        if (TryScanAndTrigger(origin, forwardRight, omniRayLength, new Color(1f, 0.5f, 0f)))
        {
            return true;
        }

        if (TryScanAndTrigger(origin, -transform.right, sideRayLength, Color.yellow))
        {
            return true;
        }

        if (TryScanAndTrigger(origin, transform.right, sideRayLength, Color.yellow))
        {
            return true;
        }

        if (includeRearArcScan)
        {
            if (TryScanAndTrigger(origin, -transform.forward, rearRayLength, Color.magenta))
            {
                return true;
            }
        }

        return false;
    }

    bool TryScanAndTrigger(Vector3 origin, Vector3 direction, float length, Color debugColor)
    {
        Debug.DrawRay(origin, direction * length, debugColor);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, length))
        {
            return false;
        }

        string hitTag = hit.collider.tag;
        if (!IsTargetTag(hitTag))
        {
            return false;
        }

        string hazardType = ClassifyDirectionalHazard(hitTag, direction);
        TryTriggerHazard(hazardType, hit.point);
        return true;
    }

    string ClassifyDirectionalHazard(string baseTag, Vector3 direction)
    {
        string normalizedTag = NormalizeHazardType(baseTag);
        if (normalizedTag == "Stairs" || normalizedTag == "Drop-off")
        {
            return normalizedTag;
        }

        float dotForward = Vector3.Dot(direction.normalized, transform.forward);
        if (dotForward < -0.3f)
        {
            return "RearObstacle";
        }

        if (Mathf.Abs(Vector3.Dot(direction.normalized, transform.right)) > 0.7f)
        {
            return "SideObstacle";
        }

        return normalizedTag;
    }

    // 播放 3D 空间音效
    void PlaySpatialAudio(string type, Vector3 position)
    {
        AudioClip clipToPlay = audioDefault;

        // 根据类型查找对应的 AudioClip
        if (tagAudioMap.ContainsKey(type) && tagAudioMap[type] != null)
        {
            clipToPlay = tagAudioMap[type];
        }

        if (clipToPlay == null) return;

        // 在指定位置创建一个临时的 GameObject 来播放声音
        GameObject audioObj = new GameObject("TempAudio_" + type);
        audioObj.transform.position = position;

        // 添加并配置 AudioSource
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clipToPlay;
        source.spatialBlend = 1.0f; // 开启 3D 声音 (1.0 = 完全 3D)
        source.rolloffMode = AudioRolloffMode.Logarithmic; // 对数衰减
        source.minDistance = minAudioDistance;
        source.maxDistance = maxAudioDistance;
        source.Play();

        // 播放完毕后销毁对象
        Destroy(audioObj, clipToPlay.length);
        
        Debug.Log($"[DigitalCane] 在 {position} 播放音效: {type}");
    }

    IEnumerator PostHazardData(string obstacleType, Vector3 position)
    {
        // 构建数据对象
        HazardData data = new HazardData();
        data.x = position.x; // 使用发生碰撞的具体位置，而非角色位置，更精准
        data.y = position.y;
        data.z = position.z;
        data.obstacle_type = obstacleType;
        data.client_source = agentGroup;

        // 转换为 JSON 字符串
        string json = JsonUtility.ToJson(data);

        // 创建 POST 请求
        using (UnityWebRequest request = new UnityWebRequest(backendUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[DigitalCane] 正在向后端发送 {obstacleType} 警报...");

            // 发送并等待
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[DigitalCane] 通信失败: {request.error}");
            }
            else
            {
                Debug.Log($"[DigitalCane] 后端接收成功: {request.downloadHandler.text}");
            }
        }
    }
}
