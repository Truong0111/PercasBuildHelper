using UnityEngine;

namespace PercasHelper.Runtime
{
    [CreateAssetMenu(fileName = "PercasHelperRuntimeSettings", menuName = "Percas/Runtime Settings")]
    public class PercasRuntimeSettingsSO : ScriptableObject
    {
        public bool ShowLogs = true;
        public int FrameRate = 60;
    }
}